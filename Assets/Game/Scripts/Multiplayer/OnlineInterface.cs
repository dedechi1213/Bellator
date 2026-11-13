using System.Linq;
using Bellator.Character;
using Bellator.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Multiplayer
{
    public sealed class OnlineInterface : MonoBehaviour
    {
        OnlineGame game;
        Canvas canvas;
        GameObject entry, lobby, combat, result;
        TMP_InputField code;
        TextMeshProUGUI status, invitation, participants, outcome, countdown, score;
        Button create, join, cancel, ready, start, choose, rematch;
        readonly System.Collections.Generic.Dictionary<Canvas,bool> menuCanvases = new System.Collections.Generic.Dictionary<Canvas,bool>();
        void Start()
        {
            game = GetComponent<OnlineGame>();
            canvas = new GameObject("OnlineCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform,false); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var stage=BellatorUI.Stage(canvas.transform,"OnlineStage");
            entry=Screen(stage,"OnlineEntry");
            BellatorUI.Title(entry.transform,"MULTIPLAYER ONLINE",.68f,.78f);
            BellatorUI.Label(entry.transform,"Description","Duelo 1v1 • Convide um amigo pelo código da sala",.20f,.58f,.80f,.66f,30);
            create=B(entry,"Create","CRIAR SALA",.25f,.45f,.47f,.53f,()=>_ = game.Connect(true,""));
            code=Input(entry.transform);
            join=B(entry,"Join","ENTRAR NA SALA",.53f,.34f,.76f,.42f,()=>_ = game.Connect(false,code.text));
            B(entry,"Close","VOLTAR",.02f,.03f,.18f,.11f,()=> { if(!game.Busy) game.Visible=false; });
            cancel=B(entry,"Cancel","CANCELAR CONEXÃO",.34f,.22f,.66f,.30f,game.Cancel);
            lobby=Screen(stage,"OnlineLobby");
            BellatorUI.Title(lobby.transform,"SALA DE ESPERA",.68f,.78f);
            invitation=BellatorUI.Label(lobby.transform,"Invitation","",.18f,.57f,.72f,.66f,32);
            B(lobby,"Copy","COPIAR CÓDIGO",.72f,.57f,.94f,.65f,()=>GUIUtility.systemCopyBuffer=game.Code);
            participants=BellatorUI.Label(lobby.transform,"Participants","",.20f,.32f,.80f,.56f,30,false);
            choose=B(lobby,"Choose","ESCOLHER GLADIADOR",.12f,.21f,.39f,.29f,()=>_ = game.ChooseCharacter());
            ready=B(lobby,"Ready","PRONTO",.41f,.21f,.61f,.29f,game.Ready);
            start=B(lobby,"Start","INICIAR PARTIDA",.63f,.21f,.90f,.29f,game.StartMatch);
            B(lobby,"Leave","SAIR DA SALA",.02f,.03f,.23f,.11f,()=>_ = game.Leave());
            BellatorUI.Label(lobby.transform,"RankingNotice","Multiplayer de teste sem ranking. Convidados não registram pontuação.",.20f,.12f,.80f,.18f,22,false);
            combat=BellatorUI.Rect(stage,"OnlineCombatHUD",0,0,1,1);
            var bar=BellatorUI.Frame(combat.transform,"CombatStatus",.10f,.86f,.90f,.99f);
            score=BellatorUI.Label(bar.transform,"Health","",.02f,.03f,.98f,.97f,27,false);
            countdown=BellatorUI.Label(combat.transform,"Countdown","",.20f,.45f,.80f,.65f,72);
            B(combat,"ExitMatch","SAIR (ESC para cursor)",.73f,.02f,.98f,.08f,()=>_ = game.Leave("Você encerrou sua participação na partida."));
            result=Screen(stage,"OnlineResult");
            BellatorUI.Title(result.transform,"FIM DO DUELO",.68f,.78f);
            outcome=BellatorUI.Label(result.transform,"Outcome","",.15f,.43f,.85f,.64f,48);
            rematch=B(result,"ReturnRoom","VOLTAR À SALA",.25f,.28f,.52f,.37f,game.ReturnToRoom);
            B(result,"ReturnMenu","SAIR DA SALA",.55f,.28f,.80f,.37f,()=>_ = game.Leave());
            status=BellatorUI.Label(stage,"ConnectionStatus","",.20f,.015f,.72f,.11f,24,false);
            game.Changed+=Refresh;
            Refresh();
        }
        GameObject Screen(Transform parent,string name)
        {
            var root=BellatorUI.Rect(parent,name,0,0,1,1); var bg=root.AddComponent<Image>(); bg.color=BellatorUI.Ink;
            BellatorUI.Artwork(root,"GameModes"); BellatorUI.Logo(root.transform,.80f,.99f); return root;
        }
        static Button B(GameObject root,string name,string title,float x,float y,float r,float t,System.Action action) => BellatorUI.Button(root.transform,name,title,x,y,r,t,action);
        TMP_InputField Input(Transform parent)
        {
            var root=BellatorUI.Rect(parent,"InviteCodeInput",.53f,.45f,.76f,.53f);
            var background=root.AddComponent<Image>(); background.color=new Color(.18f,.15f,.12f);
            var viewport=BellatorUI.Rect(root.transform,"Viewport",.04f,.05f,.96f,.95f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text=BellatorUI.Label(viewport.transform,"Text","",0,0,1,1,32,false);
            var placeholder=BellatorUI.Label(viewport.transform,"Placeholder","CÓDIGO DA SALA",0,0,1,1,26,false); placeholder.color=new Color(.8f,.75f,.65f,.65f);
            var input=root.AddComponent<TMP_InputField>(); input.textViewport=viewport.GetComponent<RectTransform>(); input.textComponent=text; input.placeholder=placeholder; input.characterLimit=12;
            input.contentType=TMP_InputField.ContentType.Alphanumeric; input.lineType=TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(submitted=> { if(!game.Busy) _ = game.Connect(false,input.text); }); return input;
        }
        void Refresh()
        {
            if(canvas==null) return;
            bool connected=game.Connected;
            entry.SetActive(!connected);
            lobby.SetActive(connected && game.Room.phase==OnlinePhase.Lobby);
            combat.SetActive(connected && (game.Room.phase==OnlinePhase.Loading || game.Room.phase==OnlinePhase.Countdown || game.Room.phase==OnlinePhase.Fighting));
            result.SetActive(connected && game.Room.phase==OnlinePhase.Result);
            create.interactable=join.interactable=code.interactable=!game.Busy && !game.IsClosingSession; cancel.gameObject.SetActive(game.Busy);
            status.text=game.Status;
            invitation.text="CÓDIGO: "+(game.LocalTest?"TESTE LOCAL":game.Code)+"    •    "+game.Room.seats.Length+"/2";
            participants.text=string.Join("\n\n",game.Room.seats.Select((s,i)=>(s.client==game.Network.LocalClientId?"VOCÊ — ":"")+s.name+"  |  "+game.Configuration.catalog.classes[s.gladiator].displayName+"  |  "+(s.ready?"PRONTO":"ESCOLHENDO")));
            if(game.Room.seats.Length<2) participants.text+="\n\nAguardando o segundo participante...";
            ready.interactable=choose.interactable=connected && game.Mine!=null && !game.Busy && !game.IsClosingSession;
            ready.GetComponentInChildren<TextMeshProUGUI>().text=game.Mine!=null && game.Mine.ready?"CANCELAR PRONTO":"PRONTO";
            start.gameObject.SetActive(game.Host);
            start.interactable=!game.Busy && !game.IsClosingSession && game.Room.seats.Length==2 && game.Room.seats.All(s=>s.ready);
            outcome.text=(game.Room.winner==ulong.MaxValue?"EMPATE":game.Room.winner==game.Network.LocalClientId?"VITÓRIA":"DERROTA")+"\n"+game.Room.result+(game.Host?"":"\nAguarde o host voltar à sala."); rematch.gameObject.SetActive(game.Host);
        }
        void Update()
        {
            if(canvas==null) return;
            canvas.enabled=game.Visible && !game.SelectingCharacter;
            // The offline menu stays loaded behind the additive arena. Hide its canvas so
            // it cannot cover the world camera during the transparent online combat HUD.
            foreach(var source in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(source!=canvas && source.gameObject.scene.name=="MainMenu" && !menuCanvases.ContainsKey(source)) menuCanvases.Add(source,source.enabled);
            foreach(var pair in menuCanvases) if(pair.Key!=null) pair.Key.enabled=pair.Value && !game.Visible && !game.SelectingCharacter;
            bool fighting=game.Connected && game.Room.phase==OnlinePhase.Fighting;
            foreach(var actor in FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None))
            {
                if(!actor.IsOwner) continue;
                actor.GetComponent<ThirdPersonInput>().enabled=fighting;
                actor.GetComponent<Bellator.Combat.CombatInput>().enabled=fighting;
            }
            if(!fighting && game.Visible && !game.SelectingCharacter) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
            if(!game.Connected) return;
            countdown.text=game.Room.phase==OnlinePhase.Loading?"CARREGANDO ARENA...":game.Room.phase==OnlinePhase.Countdown?Mathf.Max(0,Mathf.CeilToInt((float)(game.Room.startsAt-game.Network.ServerTime.Time))).ToString():"";
            var actors=FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None);
            score.text=string.Join("      |      ",actors.OrderBy(a=>a.OwnerClientId).Select(a=> {
                var seat=game.Room.seats.FirstOrDefault(s=>s.client==a.OwnerClientId);
                var hp=a.GetComponent<CharacterHealth>();
                return (seat?.name??"Gladiador")+" • VIDA "+Mathf.CeilToInt(hp.Current)+"/"+hp.Maximum+" • STAMINA "+Mathf.CeilToInt(a.DisplayStamina)+(a.IsOwner?" • Q: "+a.DisplayCooldown.ToString("0.0")+"s":"");
            }));
        }
        void OnDestroy() { if(game!=null) game.Changed-=Refresh; foreach(var pair in menuCanvases) if(pair.Key!=null) pair.Key.enabled=pair.Value; }
    }
}

