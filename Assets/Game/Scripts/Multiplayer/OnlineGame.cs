using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Match;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Multiplayer
{
    // Session lifecycle, roster and match authority. Never created by an offline match.
    public sealed class OnlineGame : MonoBehaviour
    {
        public static OnlineGame Instance { get; private set; }
        public static bool Selecting => Instance != null && Instance.selecting;
        public OnlineConfiguration Configuration { get; private set; }
        public OnlineRoom Room { get; private set; } = new OnlineRoom();
        public NetworkManager Network { get; private set; }
        public string Status { get; private set; } = "Crie uma sala ou entre com o código de convite.";
        public string Code => session?.Code ?? "";
        public bool Busy { get; private set; }
        public bool Connected => Network != null && Network.IsConnectedClient;
        public bool Host => Network != null && Network.IsHost;
        public bool Visible { get; set; } = true;
        public bool SelectingCharacter => selecting;
        public bool LocalTest { get; private set; }
        public event Action Changed;
        ISession session;
        CancellationTokenSource attempt;
        Task servicesInitialization, authentication, serviceCleanup;
        public bool IsClosingSession => leaving || serviceCleanup != null && !serviceCleanup.IsCompleted;
        readonly List<OnlineSeat> roster = new List<OnlineSeat>();
        bool leaving, selecting, closingSelection;
        AsyncOperation selectionLoad;
        double loadingDeadline;
        uint revision, receivedRevision;
        readonly Dictionary<ulong, double> lastCommand = new Dictionary<ulong, double>();
        readonly Dictionary<ulong, string> lastCommandKind = new Dictionary<ulong, string>();
        readonly HashSet<ulong> loaded = new HashSet<ulong>();
        readonly List<Canvas> hiddenCanvases = new List<Canvas>();
        readonly List<UnityEngine.EventSystems.EventSystem> hiddenEvents = new List<UnityEngine.EventSystems.EventSystem>();
        const string CommandChannel = "Bellator.Room.Command.v1", StateChannel = "Bellator.Room.State.v1";
        [Serializable] sealed class Command { public string kind, name; public int value; }
        [Serializable] sealed class State { public uint revision; public OnlineRoom room; }

        public static OnlineGame Open()
        {
            if (Instance != null) { Instance.Visible = true; return Instance; }
            var config = Resources.Load<OnlineConfiguration>("BellatorOnline");
            if (config == null) { Debug.LogError("Bellator: execute Bellator > Multiplayer > Preparar assets online."); return null; }
            var root = new GameObject("BellatorOnlineSession");
            var game = root.AddComponent<OnlineGame>();
            game.Configuration = config;
            game.CreateNetwork();
            root.AddComponent<OnlineInterface>();
            return game;
        }
        void Awake() { Instance = this; DontDestroyOnLoad(gameObject); Time.timeScale = 1; }
        void CreateNetwork()
        {
            Network = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<UnityTransport>();
            transport.ConnectTimeoutMS = 1000; transport.MaxConnectAttempts = 10; transport.DisconnectTimeoutMS = 10000;
            Network.NetworkConfig = new NetworkConfig {
                NetworkTransport = transport, EnableSceneManagement = true,
                ConnectionApproval = true, TickRate = 30, ProtocolVersion = OnlineConfiguration.Protocol,
                ForceSamePrefabs = true, ClientConnectionBufferTimeout = 20
            };
            Network.AddNetworkPrefab(Configuration.playerPrefab);
            Network.ConnectionApprovalCallback += Approve;
            Network.OnServerStarted += ServerStarted;
            Network.OnClientStarted += RegisterMessages;
            Network.OnClientConnectedCallback += Joined;
            Network.OnClientDisconnectCallback += Disconnected;
            Network.OnTransportFailure += TransportFailed;
        }
        void RegisterMessages()
        {
            Network.SceneManager.VerifySceneBeforeLoading = (index,name,mode) => name != "GladiatorSelection";
            Network.CustomMessagingManager.RegisterNamedMessageHandler(CommandChannel, CommandReceived);
            Network.CustomMessagingManager.RegisterNamedMessageHandler(StateChannel, StateReceived);
        }
        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool allowed = roster.Count < 2 && Room.phase == OnlinePhase.Lobby;
            response.Approved = allowed; response.CreatePlayerObject = false; response.Pending = false;
            response.Reason = allowed ? "" : Room.phase == OnlinePhase.Lobby ? "Sala cheia (2/2)." : "A partida já começou. Aguarde uma nova sala.";
        }
        void ServerStarted() { RegisterMessages(); Network.SceneManager.OnSceneEvent += SceneEvent; }
        void Joined(ulong id)
        {
            if (Network.IsServer)
            {
                if (!roster.Any(s => s.client == id)) roster.Add(new OnlineSeat { client = id, name = id == Network.LocalClientId ? PlayerSession.Username : "Participante", gladiator = Configuration.catalog.defaultClass });
                Publish();
            }
            if (id == Network.LocalClientId)
            {
                if (!Network.IsServer) Network.SceneManager.OnSceneEvent += SceneEvent;
                SendCommand("hello", 0, PlayerSession.Username);
                Status = "Conectado. Escolha seu gladiador e confirme PRONTO."; Changed?.Invoke();
            }
        }
        public async Task Connect(bool create, string code)
        {
            if (Busy || Connected || IsClosingSession) return;
            Busy = true; LocalTest = false; Status = "Conectando aos serviços Unity..."; Changed?.Invoke();
            attempt = new CancellationTokenSource(TimeSpan.FromSeconds(35));
            var token = attempt.Token;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    if(servicesInitialization==null || servicesInitialization.IsCompleted) servicesInitialization=UnityServices.InitializeAsync();
                    await OnlineAsync.WaitAsync(servicesInitialization,token);
                }
                token.ThrowIfCancellationRequested();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    if(authentication==null || authentication.IsCompleted) authentication=AuthenticationService.Instance.SignInAnonymouslyAsync();
                    await OnlineAsync.WaitAsync(authentication,token);
                }
                token.ThrowIfCancellationRequested();
                Status = create ? "Criando sessão e conexão Relay..." : "Procurando sala e conectando..."; Changed?.Invoke();
                session = create
                    ? await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = 2, IsPrivate = true, Name = "Bellator 1v1" }.WithRelayNetwork(), token)
                    : await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), new JoinSessionOptions(), token);
                token.ThrowIfCancellationRequested();
                Status = "Sala conectada. Autenticação Unity anônima; conta Bellator preservada.";
            }
            catch (OperationCanceledException) { Status = "Conexão cancelada ou tempo de espera esgotado."; await CleanupSession(); }
            catch (Exception ex) { Status = Explain(ex); Debug.LogWarning("Bellator online: " + ex.GetType().Name + ": " + ex.Message); await CleanupSession(); }
            finally { Busy = false; attempt.Dispose(); attempt = null; Changed?.Invoke(); }
        }
        static string Explain(Exception ex)
        {
            if(ex.ToString().Contains("PlayerPrefs") || ex.ToString().Contains("Could not store preference")) return "O sistema bloqueou a gravação das preferências exigidas pela autenticação Unity. Execute a build em um ambiente com permissão de gravação no perfil do Windows.";
            string reason = ex is SessionException se ? se.Error.ToString() : ex.Message;
            string key = reason.ToLowerInvariant();
            if (key.Contains("full")) return "Sala cheia. O limite é de dois jogadores.";
            if (key.Contains("notfound") || key.Contains("not found")) return "Sala inexistente ou encerrada. Confira o código.";
            if (key.Contains("invalid") || key.Contains("code")) return "Código inválido. Copie o código completo da sala.";
            if (key.Contains("project") || key.Contains("configuration") || key.Contains("environment")) return "Vincule este projeto aos Unity Gaming Services e habilite Authentication, Lobby e Relay (consulte o relatório).";
            if (key.Contains("unauthor") || key.Contains("forbidden")) return "Serviços Unity indisponíveis para este projeto. Verifique o Dashboard e a autenticação anônima.";
            return "Não foi possível conectar. Verifique a internet e a configuração dos serviços Unity. Detalhes: " + reason;
        }
        public void Cancel() { attempt?.Cancel(); Status = "Cancelando conexão..."; Changed?.Invoke(); }
        // Development builds only; bypasses UGS solely for repeatable two-process tests.
        public bool StartLocalTest(bool host, ushort port = 7777)
        {
            if (!Debug.isDebugBuild || Busy || Connected) return false;
            LocalTest = true;
            Network.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port, "127.0.0.1");
            return host ? Network.StartHost() : Network.StartClient();
        }
        public OnlineSeat Mine => Room.seats.FirstOrDefault(s => s.client == Network.LocalClientId);
        public void Select(int index) => SendCommand("select", index);
        public void Ready() => SendCommand("ready", Mine != null && !Mine.ready ? 1 : 0);
        public void StartMatch() => SendCommand("start");
        public void ReturnToRoom() => SendCommand("lobby");
        public void SendCommand(string kind, int value = 0, string name = "")
        {
            if (!Connected) return;
            var command = new Command { kind = kind, value = value, name = name };
            if (Network.IsServer) ApplyCommand(Network.LocalClientId, command);
            else Send(CommandChannel, NetworkManager.ServerClientId, JsonUtility.ToJson(command));
        }
        void CommandReceived(ulong sender, FastBufferReader reader)
        {
            if (!Network.IsServer || !Network.ConnectedClients.ContainsKey(sender) || reader.Length > 512) return;
            try { reader.ReadValueSafe(out string json); ApplyCommand(sender, JsonUtility.FromJson<Command>(json)); } catch (Exception ex) { Debug.LogWarning("Comando de sala rejeitado: " + ex.GetType().Name); }
        }
        void ApplyCommand(ulong sender, Command command)
        {
            var seat = roster.FirstOrDefault(s => s.client == sender);
            if (seat == null || command == null) return;
            if (lastCommand.TryGetValue(sender, out double last) && Time.realtimeSinceStartupAsDouble - last < .04 && lastCommandKind.TryGetValue(sender,out string kind) && kind == command.kind) return;
            lastCommand[sender] = Time.realtimeSinceStartupAsDouble; lastCommandKind[sender]=command.kind;
            switch (command.kind)
            {
                case "hello": seat.name = string.IsNullOrWhiteSpace(command.name) ? "Convidado" : new string(command.name.Where(c => !char.IsControl(c)).Take(24).ToArray()); break;
                case "select":
                    if (Room.phase != OnlinePhase.Lobby || command.value < 0 || command.value >= Configuration.catalog.classes.Length) return;
                    seat.gladiator = command.value; seat.ready = false; break;
                case "ready": if (Room.phase != OnlinePhase.Lobby) return; seat.ready = command.value == 1; break;
                case "start":
                    if (sender != NetworkManager.ServerClientId || Room.phase != OnlinePhase.Lobby || roster.Count != 2 || roster.Any(s => !s.ready)) return;
                    Room.phase = OnlinePhase.Loading; Room.result = ""; Room.round++; loaded.Clear(); loadingDeadline = Time.realtimeSinceStartupAsDouble + 60;
                    Publish();
                    if (Network.SceneManager.LoadScene(OnlineConfiguration.Arena, LoadSceneMode.Additive) != SceneEventProgressStatus.Started) { Room.phase = OnlinePhase.Lobby; Room.result = "Falha ao carregar a arena. Verifique as cenas da build."; }
                    break;
                case "lobby":
                    if (sender != NetworkManager.ServerClientId || Room.phase != OnlinePhase.Result) return;
                    foreach (var actor in FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None)) if (actor.IsSpawned) actor.NetworkObject.Despawn();
                    ClearProjectiles();
                    Room.phase = OnlinePhase.Loading; loadingDeadline=Time.realtimeSinceStartupAsDouble+60; Publish();
                    var scene = SceneManager.GetSceneByPath(OnlineConfiguration.Arena);
                    if (scene.IsValid()) Network.SceneManager.UnloadScene(scene);
                    else CompleteLobby();
                    return;
                default: return;
            }
            Publish();
        }
        void Publish()
        {
            if(selecting && Room.phase!=OnlinePhase.Lobby) _=CloseSelection();
            Room.seats = roster.ToArray();
            var json = JsonUtility.ToJson(new State { revision = ++revision, room = Room });
            foreach (ulong client in Network.ConnectedClientsIds) if (client != Network.LocalClientId) Send(StateChannel, client, json);
            Changed?.Invoke();
        }
        void StateReceived(ulong sender, FastBufferReader reader)
        {
            if (Network.IsServer || sender != NetworkManager.ServerClientId || reader.Length > 4096) return;
            reader.ReadValueSafe(out string json);
            var state = JsonUtility.FromJson<State>(json);
            if (state == null || state.revision <= receivedRevision) return;
            receivedRevision = state.revision; Room = state.room;
            if(selecting && Room.phase!=OnlinePhase.Lobby) _=CloseSelection();
            Changed?.Invoke();
        }
        void Send(string channel, ulong client, string json)
        {
            using var writer = new FastBufferWriter(System.Text.Encoding.UTF8.GetByteCount(json) * 2 + 16, Allocator.Temp);
            writer.WriteValueSafe(json);
            Network.CustomMessagingManager.SendNamedMessage(channel, client, writer, NetworkDelivery.ReliableSequenced);
        }
        void SceneEvent(SceneEvent ev)
        {
            if (!Network.IsServer) return;
            if (ev.SceneEventType == SceneEventType.LoadEventCompleted && ev.SceneName == "OnlineArena" && Room.phase == OnlinePhase.Loading)
            {
                if (ev.ClientsThatTimedOut != null && ev.ClientsThatTimedOut.Count > 0) { _ = Leave("A arena não carregou para todos os participantes."); return; }
                BeginArena();
            }
            if (ev.SceneEventType == SceneEventType.UnloadEventCompleted && ev.SceneName == "OnlineArena") CompleteLobby();
        }
        void BeginArena()
        {
            if (roster.Count != 2) { _ = Leave("Um participante saiu durante o carregamento."); return; }
            var arena = SceneManager.GetSceneByPath(OnlineConfiguration.Arena);
            for (int i = 0; i < roster.Count; i++)
            {
                var point = GameObject.Find("OnlineSpawn" + i);
                if (point == null) { _ = Leave("Ponto de nascimento online ausente."); return; }
                var root = Instantiate(Configuration.playerPrefab, point.transform.position, point.transform.rotation);
                SceneManager.MoveGameObjectToScene(root, arena);
                var actor = root.GetComponent<NetworkGladiator>();
                actor.InitialClass = roster[i].gladiator;
                root.GetComponent<NetworkObject>().SpawnAsPlayerObject(roster[i].client, true);
            }
            Room.startsAt = Network.ServerTime.Time + 4;
            Room.phase = OnlinePhase.Countdown; Publish();
        }
        void CompleteLobby() { foreach (var seat in roster) seat.ready = false; Room.phase = OnlinePhase.Lobby; Room.result = ""; Publish(); }
        void Update()
        {
            if (Network == null || !Network.IsServer || !Network.IsListening || leaving) return;
            if (Room.phase == OnlinePhase.Loading && Time.realtimeSinceStartupAsDouble > loadingDeadline) { _ = Leave("Tempo de carregamento esgotado."); return; }
            if (Room.phase == OnlinePhase.Countdown && Network.ServerTime.Time >= Room.startsAt) { Room.phase = OnlinePhase.Fighting; Publish(); }
            if (Room.phase == OnlinePhase.Fighting)
            {
                var actors = FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None);
                if (actors.Length != 2) return;
                var fallen = actors.Where(a => !a.GetComponent<CharacterHealth>().IsAlive).ToArray();
                if (fallen.Length > 0)
                {
                    var winner = actors.FirstOrDefault(a => a.GetComponent<CharacterHealth>().IsAlive);
                    Room.phase = OnlinePhase.Result; Room.winner=winner != null ? winner.OwnerClientId : ulong.MaxValue;
                    Room.result = winner == null ? "EMPATE" : (roster.First(s => s.client == winner.OwnerClientId).name + " venceu!");
                    ClearProjectiles(); Publish();
                }
            }
        }
        void Disconnected(ulong id)
        {
            if (leaving || Busy) return;
            if (Network.IsServer)
            {
                roster.RemoveAll(s => s.client == id); lastCommand.Remove(id); lastCommandKind.Remove(id);
                if (Room.phase != OnlinePhase.Lobby) { _ = Leave("O adversário desconectou. Partida encerrada sem ranking."); return; }
                foreach (var seat in roster) seat.ready = false;
                Status = "O outro participante saiu da sala."; Publish();
            }
            else if (id == Network.LocalClientId) _ = Leave(DisconnectMessage(Network.DisconnectReason));
        }
        static string DisconnectMessage(string reason)
        {
            if(!string.IsNullOrEmpty(reason) && (reason.StartsWith("Sala ") || reason.StartsWith("A partida "))) return reason;
            if(!string.IsNullOrEmpty(reason) && (reason.Contains("NetworkConfig") || reason.Contains("ProtocolVersion"))) return "As versões do jogo são diferentes. Use a mesma build nos dois computadores.";
            return "Conexão com o host perdida. A partida foi encerrada; não há migração de host.";
        }
        void TransportFailed() { if (!leaving) _ = Leave("Falha de transporte. A conexão foi encerrada."); }
        public async Task ChooseCharacter()
        {
            if (!Connected || Room.phase != OnlinePhase.Lobby || selecting) return;
            SendCommand("ready",0);
            selecting = true; Visible = false;
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if(c.enabled) { hiddenCanvases.Add(c); c.enabled=false; }
            foreach(var e in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None)) if(e.enabled) { hiddenEvents.Add(e); e.enabled=false; }
            Changed?.Invoke();
            var op = selectionLoad = SceneManager.LoadSceneAsync(PlayerClassController.SelectionScene, LoadSceneMode.Additive);
            while (op != null && !op.isDone) await Task.Yield();
        }
        public void ConfirmSelection(int index) { Select(index); _ = CloseSelection(); }
        public async Task CloseSelection()
        {
            if(closingSelection) { while(closingSelection) await Task.Yield(); return; }
            if(!selecting) return;
            closingSelection=true;
            try
            {
                while(selectionLoad!=null && !selectionLoad.isDone) await Task.Yield();
                selecting=false; selectionLoad=null;
                var scene=SceneManager.GetSceneByPath(PlayerClassController.SelectionScene);
                if(scene.IsValid())
                {
                    var op=SceneManager.UnloadSceneAsync(scene);
                    while(op!=null && !op.isDone) await Task.Yield();
                }
                foreach(var c in hiddenCanvases) if(c!=null) c.enabled=true;
                hiddenCanvases.Clear();
                foreach(var e in hiddenEvents) if(e!=null) e.enabled=true;
                hiddenEvents.Clear();
                Visible=true; Changed?.Invoke();
            }
            finally { closingSelection=false; }
        }
        public async Task Leave(string message = "Você saiu da sala.")
        {
            if (leaving) return;
            if (Busy) { Cancel(); return; }
            leaving = true; Status = message; Room.phase=OnlinePhase.Loading;
            if(Connected && Network.IsServer) Publish(); else Changed?.Invoke();
            await CloseSelection();
            await CleanupSession();
            var scene = SceneManager.GetSceneByPath(OnlineConfiguration.Arena);
            if (scene.IsValid())
            {
                var op = SceneManager.UnloadSceneAsync(scene);
                while (op != null && !op.isDone) await Task.Yield();
            }
            ClearProjectiles(); Room = new OnlineRoom(); roster.Clear(); lastCommand.Clear(); lastCommandKind.Clear(); receivedRevision = 0; revision = 0;
            Visible = true; leaving = false; Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; Changed?.Invoke();
        }
        async Task CleanupSession()
        {
            var previous=session; session=null;
            try
            {
                if(previous!=null)
                {
                    var closing=previous.LeaveAsync(); serviceCleanup=closing;
                    _=ObserveServiceCleanup(closing);
                    using(var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(8)))
                        await OnlineAsync.WaitAsync(closing,deadline.Token);
                }
            }
            catch(OperationCanceledException)
            {
                Status+="\nA saída local será concluída; o serviço Unity ainda está encerrando a sessão. Você pode voltar ao menu.";
            }
            catch(Exception ex) { Debug.LogWarning("Encerramento Unity: "+ex.GetType().Name); }
            finally
            {
                if(Network!=null && Network.IsListening)
                {
                    if(Network.SceneManager!=null) Network.SceneManager.OnSceneEvent-=SceneEvent;
                    Network.Shutdown();
                }
            }
        }
        async Task ObserveServiceCleanup(Task closing)
        {
            try { await closing; }
            catch(Exception ex) { Debug.LogWarning("Limpeza da sessão Unity: "+ex.GetType().Name); }
            finally
            {
                if(serviceCleanup==closing) serviceCleanup=null;
                if(this!=null) Changed?.Invoke();
            }
        }
        static void ClearProjectiles() { foreach (var p in FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) Destroy(p.gameObject); }
        void OnApplicationQuit() { attempt?.Cancel(); if (Network != null && Network.IsListening) Network.Shutdown(); }
        void OnDestroy() { if (Instance == this) Instance = null; attempt?.Cancel(); }
    }
}



