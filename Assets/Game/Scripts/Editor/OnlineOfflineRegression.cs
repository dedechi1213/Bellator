using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Match;
using Bellator.Multiplayer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class OnlineOfflineRegression
    {
        const string Key="Bellator.Online.OfflineRegression";
        static IEnumerator routine;
        static readonly StringBuilder report=new StringBuilder();
        static int frame;
        static double deadline;
        static bool accountBlocked;
        static OnlineOfflineRegression() { EditorApplication.update+=Update; EditorApplication.playModeStateChanged+=State; }
        static void Update()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;
                try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException(); if(!routine.MoveNext()) Finish(null); }
                catch(Exception ex) { Finish(ex); }
            }
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/OnlineOffline.test")) return;
            File.Delete("Temp/OnlineOffline.test");
            if(EditorSceneManager.GetActiveScene().isDirty) { File.WriteAllText("Logs/Online-offline-regression.txt","BLOCKED: scene has unsaved changes"); return; }
            EditorSceneManager.OpenScene(GameInterface.MainScene); SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            accountBlocked=false; report.Clear(); report.AppendLine("Bellator offline regression / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
            deadline=EditorApplication.timeSinceStartup+180; frame=-1; routine=Checks();
        }
        static void Require(bool value,string message) { if(!value) throw new Exception(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message); File.WriteAllText("Logs/Online-offline-regression.txt",report.ToString()); }
        static IEnumerator Checks()
        {
            yield return null; yield return null;
            Require(!PlayerSession.HasSession && GameObject.Find("AccessScreen")!=null,"Initial access screen missing");
            Require(GameObject.Find("GuestRankingNotice").GetComponent<TextMeshProUGUI>()!=null,"Current TMP guest notice missing");
            GameObject.Find("GuestButton").GetComponent<Button>().onClick.Invoke(); yield return null;
            Require(PlayerSession.HasSession && !PlayerSession.IsAccount,"Guest flow failed");
            Require(GameObject.Find("OnlineHomeButton")!=null,"Online menu option missing");
            var guestScore=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro");
            Require(!guestScore.CanRecord,"Guest can record a rank"); Pass("Current TMP access screen, guest entry, online menu option and guest ranking exclusion.");
            PlayerSession.Logout();
            var store=new LocalAccountStore(Path.GetFullPath("Temp/OnlineAccounts/"+Guid.NewGuid().ToString("N")));
            var create=store.CreateAsync("OnlineRegression","BellatorTest_123!","BellatorTest_123!");
            while(!create.IsCompleted) yield return null;
            if(!create.IsFaulted && !create.Result.Success && store.LastError!=null && store.LastError.Contains("Access to the path"))
            {
                accountBlocked=true; report.AppendLine("BLOCKED: local account persistence denied by this environment: "+store.LastError);
            }
            else
            {
                Require(!create.IsFaulted && create.Result.Success,"Local account creation failed: "+(create.IsFaulted?create.Exception.ToString():create.Result.Error)+" / "+store.LastError);
            Require(PlayerSession.EnterAccount(create.Result) && PlayerSession.IsAccount,"Local identity login failed");
            var wrong=store.LoginAsync("OnlineRegression","wrong"); while(!wrong.IsCompleted) yield return null;
            Require(!wrong.IsFaulted && !wrong.Result.Success,"Wrong password accepted");
            var correct=store.LoginAsync("OnlineRegression","BellatorTest_123!"); while(!correct.IsCompleted) yield return null;
            Require(!correct.IsFaulted && correct.Result.Success,"Correct password rejected");
            var score=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro"); score.BeginWave(1); score.Defeated(Bellator.Enemies.EnemyRole.Gladiator,1); score.Finish();
            var ranking=new LocalRankingManager(Path.GetFullPath("Temp/OnlineAccounts/"+Guid.NewGuid().ToString("N")+"/ranking.json"));
            Require(score.CanRecord && score.SaveResult(ranking),"Account local ranking failed");
            Pass("Isolated local account creation, correct and incorrect password validation, Bellator session and local account-bound ranking.");
            }
            PlayerSession.Logout(); PlayerSession.EnterGuest();
            var online=OnlineGame.Open(); yield return null;
            Require(online!=null && GameObject.Find("OnlineCanvas")!=null && !online.Connected,"Online entry UI missing"); online.Visible=false;
            var configuration=online.Configuration;
            Require(configuration.catalog.classes.Length==4,"Class catalog changed");
            ClassSelectionState.Confirm(configuration.catalog.classes[2]); GameModeManager.Select(GameMode.Story);
            var load=SceneManager.LoadSceneAsync(PlayerClassController.ArenaScene,LoadSceneMode.Single); while(!load.isDone) yield return null;
            yield return null; yield return null;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Require(game!=null && game.Mode==GameMode.Story && game.Waves.WaveCount==5,"Traditional match changed");
            Require(game.Player.GetComponent<NetworkGladiator>()==null && !game.Player.GetComponent<CharacterHealth>().RemoteReplica,"Offline player has network authority restrictions");
            var motor=game.Player.GetComponent<ThirdPersonMotor>(); motor.AcceptInput=false;
            var before=game.Player.transform.position;
            for(int i=0;i<20;i++) { motor.ProcessMovement(Vector2.up,false,false,.02f); yield return null; }
            Require(Vector3.Distance(before,game.Player.transform.position)>.1f,"Offline movement failed");
            var ability=game.Player.GetComponent<PlayerAbility>(); Require(ability.TryActivate(),"Offline ability failed");
            var health=game.Player.GetComponent<CharacterHealth>(); float hp=health.Current;
            Require(game.Player.GetComponent<DamageReceiver>().Receive(new DamageInfo(10,game.Player.transform.position,game.Player.transform.position+Vector3.forward,null)) && health.Current<hp,"Offline damage failed");
            Pass("Online entry can coexist with original traditional scene: five waves, original prefab, movement, Q ability, damage and no network restrictions.");
            UnityEngine.Object.Destroy(online.gameObject);
        }
        static void Finish(Exception error)
        {
            routine=null; SessionState.SetBool(Key,false);
            report.AppendLine(error==null?(accountBlocked?"RESULT: PARTIAL - guest, traditional match and offline mechanics passed; account persistence blocked by filesystem permissions":"RESULT: PASS"):"RESULT: FAIL\n"+error);
            File.WriteAllText("Logs/Online-offline-regression.txt",report.ToString());
            if(error!=null) Debug.LogException(error);
            EditorApplication.ExitPlaymode();
        }
    }
}




