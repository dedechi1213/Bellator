#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.IO;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Match;
using UnityEngine;

namespace Bellator.Multiplayer
{
    // Opt-in, development-only two-process acceptance probe. Never runs in a normal game.
    public sealed class OnlineSmokeProbe : MonoBehaviour
    {
        OnlineGame game;
        bool host, relay, expectDisconnect, captured;
        int expectedClass;
        int stage;
        float start, fightStart, nextAttack, nextSpecial, nextDodge;
        Vector3 spawn;
        bool moved, damaged, opponentDamaged, rolled, ability, animations, replicaRejected;
        public static bool Running { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-bellatorTest");
            bool relay=Array.IndexOf(args,"-bellatorRelay")>=0;
            if(relay) index=Array.IndexOf(args,"-bellatorRelay");
            if(index<0 || index+1>=args.Length || Running) return;
            Running=true; var probe=new GameObject("OnlineSmokeProbe").AddComponent<OnlineSmokeProbe>();
            DontDestroyOnLoad(probe.gameObject); probe.host=args[index+1]=="host"; probe.relay=relay;
            probe.expectedClass=probe.host?2:0;
            int classArg=Array.IndexOf(args,"-bellatorClass"); if(classArg>=0 && classArg+1<args.Length) probe.expectedClass=int.Parse(args[classArg+1]);
            probe.expectDisconnect=Array.IndexOf(args,"-bellatorDisconnectExpected")>=0;
            if(Array.IndexOf(args,"-bellatorImpair")>=0) { UnityTransport.s_DriverConstructor=new OnlineTestDriver(); Debug.Log("ONLINE_TEST SIMULATION 75ms send delay, 20ms jitter, 3% send loss per peer"); }
        }
        IEnumerator Start()
        {
            yield return null; Application.runInBackground=true;
            PlayerSession.EnterGuest(); game=OnlineGame.Open();
            if(relay)
            {
                var init=UnityServices.InitializeAsync();
                while(!init.IsCompleted) yield return null;
                if(init.IsFaulted) { Fail("UGS init: "+init.Exception); yield break; }
                AuthenticationService.Instance.SwitchProfile(host?"bellator_probe_host":"bellator_probe_client");
                string file=Path.GetFullPath("Temp/OnlineRelay.code");
                if(!host) { float deadline=Time.unscaledTime+50; while(!File.Exists(file) && Time.unscaledTime<deadline) yield return null; }
                var connection=game.Connect(host,host?"":File.Exists(file)?File.ReadAllText(file):"");
                while(!connection.IsCompleted) yield return null;
                if(!game.Connected) { Fail("Relay: "+game.Status); yield break; }
                if(host) File.WriteAllText(file,game.Code);
                Debug.Log("ONLINE_TEST RELAY_CONNECTED");
            }
            else if(!game.StartLocalTest(host)) { Fail("NetworkManager não iniciou"); yield break; }
            start=Time.unscaledTime; Debug.Log("ONLINE_TEST START "+(host?"HOST":"CLIENT"));
        }
        void Update()
        {
            if(game==null) return;
            if(Time.unscaledTime-start>100) { Fail("Timeout stage="+stage+" phase="+game.Room.phase+" status="+game.Status); return; }
            if(expectDisconnect && stage>=2 && !game.Connected && game.Room.seats.Length==0) { Debug.Log("ONLINE_TEST HOST_DISCONNECT_PASS "+game.Status); Running=false; Application.Quit(0); enabled=false; return; }
            if(stage==0 && game.Connected && game.Room.seats.Length==2)
            { stage=10; _=game.ChooseCharacter(); start=Time.unscaledTime; Debug.Log("ONLINE_TEST CONNECTED_TWO"); }
            if(stage==10 && Time.unscaledTime-start>.5f)
            {
                var selection=FindAnyObjectByType<Bellator.Classes.GladiatorSelectionController>();
                if(selection!=null) { selection.Select(expectedClass); selection.Confirm(); stage=1; start=Time.unscaledTime; Debug.Log("ONLINE_TEST SELECTION_SCREEN_OK"); }
            }
            if(stage==1 && !game.SelectingCharacter && Time.unscaledTime-start>.5f) { game.Ready(); stage=2; }
            if(stage==2 && host && game.Room.seats.Length==2 && game.Room.seats.All(s=>s.ready) && Time.unscaledTime-start>1)
            { game.StartMatch(); stage=3; }
            if(stage==2 && !host && game.Room.phase!=OnlinePhase.Lobby) stage=3;
            var mine=FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None).FirstOrDefault(a=>a.IsOwner);
            if(mine!=null && stage==3 && game.Room.phase==OnlinePhase.Countdown)
            { spawn=mine.transform.position; stage=4; Debug.Log("ONLINE_TEST REAL_CLASS "+mine.ClassIndex.Value); if(mine.ClassIndex.Value!=expectedClass) Fail("Escolha individual divergente"); }
            if(stage==4 && mine!=null && game.Room.phase==OnlinePhase.Countdown && Vector3.Distance(spawn,mine.transform.position)>.2f) Fail("Movimento durante contagem");
            if(stage==4 && mine!=null && game.Room.phase==OnlinePhase.Countdown && game.Network.ServerTime.Time>game.Room.startsAt-2)
            {
                var orbit=Camera.main!=null?Camera.main.GetComponent<ThirdPersonCamera>():null;
                float expected=game.Configuration.catalog.classes[mine.ClassIndex.Value].colliderHeight*.74f;
                if(orbit==null || Mathf.Abs(orbit.Pivot.y-mine.transform.position.y-expected)>.2f) { Fail("Camera alterada pelo gladiador remoto"); return; }
            }
            if(stage==4 && game.Room.phase==OnlinePhase.Fighting)
                        {
                stage=5; fightStart=Time.unscaledTime;
                if(FindObjectsByType<Canvas>(FindObjectsSortMode.None).Any(c=>c.gameObject.scene.name=="MainMenu" && c.enabled)) { Fail("Menu cobrindo a arena"); return; }
                Debug.Log("ONLINE_TEST COUNTDOWN_OK MENU_HIDDEN");
            }
            if(stage==5 && mine!=null)
            {
                if(!captured && Time.unscaledTime-fightStart>1.5f && Environment.GetCommandLineArgs().Contains("-bellatorCapture")) { captured=true; ScreenCapture.CaptureScreenshot("Logs/Online-"+(host?"host":"client")+".png"); }
                moved|=Vector3.Distance(spawn,mine.transform.position)>1;
                var hp=mine.GetComponent<CharacterHealth>(); damaged|=hp.Current<hp.Maximum;
                foreach(var actor in FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None)) if(actor!=mine) opponentDamaged|=actor.GetComponent<CharacterHealth>().Current<actor.GetComponent<CharacterHealth>().Maximum;
                rolled|=mine.GetComponent<ThirdPersonMotor>().DodgeSequence>0 || mine.CurrentVisualState=="DodgeRoll" || mine.CurrentVisualState=="Quickstep";
                ability|=mine.DisplayCooldown>0;
                var animator=mine.GetComponent<PlayerAnimationBridge>().Animator;
                animations|=animator!=null && animator.GetFloat("Speed")>.01f;
                if(!host && !replicaRejected && Time.unscaledTime-fightStart>1)
                { float before=hp.Current; hp.Damage(999); replicaRejected=Mathf.Approximately(hp.Current,before); Debug.Log("ONLINE_TEST REPLICA_DAMAGE_REJECTED "+replicaRejected); }
            }
            if(stage==5 && game.Room.phase==OnlinePhase.Result)
            {
                Debug.Log("ONLINE_TEST RESULT "+game.Room.result+" moved="+moved+" ownDamage="+damaged+" opponentDamage="+opponentDamaged+" rollHost="+rolled+" ability="+ability+" animation="+animations);
                if(!moved || !damaged || !opponentDamaged || !ability || !animations || !rolled || (!host && !replicaRejected)) { Fail("Cobertura de combate incompleta"); return; }
                stage=6; start=Time.unscaledTime;
            }
            if(stage==6 && host && Time.unscaledTime-start>1) { game.ReturnToRoom(); stage=7; }
            if(stage==6 && !host && game.Room.phase!=OnlinePhase.Result) stage=7;
            if(stage==7 && game.Room.phase==OnlinePhase.Lobby)
            { Debug.Log("ONLINE_TEST RETURN_LOBBY_OK"); stage=8; start=Time.unscaledTime; }
            if(stage==8 && Time.unscaledTime-start>1) { stage=9; _=game.Leave(); }
            if(stage==9 && !game.Connected) { Debug.Log("ONLINE_TEST PASS "+(host?"HOST":"CLIENT")); Running=false; Application.Quit(0); }
        }
        public static OnlineInput Drive(NetworkGladiator mine,OnlineInput input)
        {
            var probe=FindAnyObjectByType<OnlineSmokeProbe>();
            if(probe==null || probe.game==null || !mine.IsActive) return input;
            var other=FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None).FirstOrDefault(a=>a!=mine);
            if(other==null) return input;
            Vector3 delta=other.transform.position-mine.transform.position; delta.y=0;
            input.yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg; input.pitch=0; input.locked=true; input.guard=false;
            mine.GetComponent<PlayerLockOn>().SetOnlineTarget(other.GetComponent<DamageReceiver>());
            float elapsed=Time.unscaledTime-probe.fightStart;
            input.axis=delta.magnitude>(mine.ClassIndex.Value==3?7:1.3f)?Vector2.up:Vector2.zero;
            if(elapsed>6 && probe.ability && Time.unscaledTime>probe.nextAttack && (probe.host || elapsed<15)) { input.light++; probe.nextAttack=Time.unscaledTime+.7f; }
            if(elapsed>.5f && mine.DisplayCooldown<=0 && (mine.ClassIndex.Value!=1 || delta.magnitude<3) && Time.unscaledTime>probe.nextSpecial) { input.ability++; probe.nextSpecial=Time.unscaledTime+.25f; }
            if(elapsed>2 && elapsed<4 && Time.unscaledTime>probe.nextDodge) { input.dodge++; input.axis=Vector2.right; probe.nextDodge=Time.unscaledTime+1.5f; }
            return input;
        }
        void Fail(string message) { Debug.LogError("ONLINE_TEST FAIL "+message); Running=false; Application.Quit(2); enabled=false; }
    }
}
#endif




