using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Arena;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class MatchStageValidation
    {
        const string Active="Bellator.Stage6.Test";
        static IEnumerator routine; static int frame=-1;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static MatchStageValidation() { EditorApplication.update+=Check; EditorApplication.playModeStateChanged+=State; }
        static void Check()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            { frame=Time.frameCount; try { if(!routine.MoveNext()) Finish(null); } catch(Exception e) { Finish(e); } }
            if(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorMatch.test")) return;
            File.Delete("Temp/BellatorMatch.test"); Run();
        }
        [MenuItem("Bellator/Test Stage 6 Complete Matches in Play Mode")]
        public static void Run()
        { if(EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return; EditorSceneManager.OpenScene(PlayerClassController.SelectionScene); SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode(); }
        static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear();errors.Clear();report.AppendLine("Stage 6 — Unity "+Application.unityVersion+" — real Play Mode, scene loads, NavMesh, CharacterController and UI buttons");
            Application.logMessageReceived+=Log; routine=Checks(); frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("NavMeshAgent") || message.Contains("MissingReferenceException"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log; if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null; SessionState.SetBool(Active,false); Time.timeScale=1; report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/Stage6Validation.txt",report.ToString());File.WriteAllText("Temp/BellatorMatch.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error); else Debug.Log("Bellator Stage 6 complete-match validation passed."); EditorApplication.ExitPlaymode();
        }
        static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message);File.WriteAllText("Temp/BellatorMatch.progress.txt",report.ToString()); }
        static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        static void FreezeEnemies(WaveManager waves)
        { foreach(var e in waves.Actors) if(e!=null) { e.AutoThink=false; e.GetComponent<EnemyNavigation>().Stop(); } }
        static void Kill(EnemyBrain actor,Transform player)
        { if(actor!=null && actor.GetComponent<CharacterHealth>().IsAlive) Assert(actor.GetComponent<DamageReceiver>().Receive(new DamageInfo(10000,actor.transform.position+Vector3.up,player.position,player)),"Shared damage rejected enemy kill"); }
        static IEnumerator Checks()
        {
            yield return null; yield return null;
            Assert(PlayerSession.EnterGuest(),"Could not enter guest session for story regression");
            int[,] expected={{2,0,0},{3,0,0},{2,1,0},{3,0,1},{2,2,2}};
            for(int classIndex=0;classIndex<4;classIndex++)
            {
                var selection=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>(); Assert(selection!=null,"Selection scene missing");
                var chosen=selection.Catalog.classes[classIndex]; selection.Select(classIndex);selection.Confirm();
                while(SceneManager.GetActiveScene().name!="MainMenu") yield return null;yield return null;yield return null;
                Button("PlayButton").onClick.Invoke();
                Button("StoryModeButton").onClick.Invoke();
                while(SceneManager.GetActiveScene().name!="RomanArena") yield return null; yield return null;yield return null;
                var game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(game!=null,"GameManager not installed");
                var waves=game.Waves; var player=game.Player; var motor=player.GetComponent<ThirdPersonMotor>(); var combat=player.GetComponent<PlayerCombat>(); var ability=player.GetComponent<PlayerAbility>(); var health=player.GetComponent<CharacterHealth>();
                Assert(player.Current==chosen && health.Current==chosen.health,"Selected class lost entering survival");
                Assert(game.State==MatchState.Preparing && waves.AliveCount==0 && UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length==0,"Player did not start alone");
                Assert(!UnityEngine.Object.FindAnyObjectByType<TrainingArenaControls>().enabled,"Training cheat controls active");
                waves.ManualSimulation=true; motor.AcceptInput=false; Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=false;
                Assert(waves.Schedule.waves.Length==5,"Wave count");
                for(int w=0;w<5;w++) for(int role=0;role<3;role++) Assert(waves.Schedule.waves[w].groups[role].count==expected[w,role],"Default wave composition");
                if(classIndex==0)
                {
                    float countdown=waves.Countdown; game.TogglePause(); Assert(game.State==MatchState.Paused && Time.timeScale==0,"Pause failed");
                    float until=Time.realtimeSinceStartup+.3f; while(Time.realtimeSinceStartup<until) yield return null;
                    Assert(Mathf.Abs(countdown-waves.Countdown)<.01f && waves.AliveCount==0,"Countdown advanced in pause");
                    Assert(!combat.TryAttack(false) && !ability.TryActivate(),"Combat/ability allowed during pause");
                    ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Stage6_Pause.png"); yield return null;yield return null;
                    Button("ResumeButton").onClick.Invoke(); Assert(game.State==MatchState.Preparing && Time.timeScale==1,"Resume button failed"); motor.AcceptInput=false; Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=false;
                    var entries=UnityEngine.Object.FindObjectsByType<ArenaSpawnPoint>(FindObjectsSortMode.None).Where(p=>p.Kind==ArenaSpawnKind.Enemy).ToArray();
                    var barriers=new List<GameObject>();
                    foreach(var entry in entries) { var b=GameObject.CreatePrimitive(PrimitiveType.Cube); b.name="SpawnBlockTest";b.transform.position=entry.transform.position+Vector3.up*1.5f;b.transform.localScale=new Vector3(7,3,7);barriers.Add(b); }
                    Physics.SyncTransforms(); waves.Tick(waves.Countdown+.01f); waves.Tick(1);
                    Assert(waves.SpawnBlocked && waves.AliveCount==0 && waves.PendingCount==2,"Blocked spawn lost or overlapped enemies");
                    foreach(var b in barriers) UnityEngine.Object.Destroy(b); yield return null;Physics.SyncTransforms();waves.Tick(1);yield return null;
                    Assert(waves.AliveCount==1 && waves.PendingCount==1,"Blocked spawn did not retry after clearance"); FreezeEnemies(waves);
                    var enemy=waves.Actors[0]; var camera=Camera.main;camera.transform.rotation=Quaternion.identity;
                    for(int n=0;n<8;n++) motor.ProcessMovement(Vector2.zero,false,false,1f/60); motor.ProcessMovement(Vector2.up,false,true,1f/60);
                    Assert(motor.IsDodging,"Death-during-dodge setup");
                    player.GetComponent<DamageReceiver>().Receive(new DamageInfo(10000,player.transform.position,enemy.transform.position,enemy.transform));
                    yield return null;yield return null;
                    Assert(game.State==MatchState.Defeat && waves.State==WaveState.Stopped && waves.PendingCount==0,"Death did not terminate spawning");
                    Assert(combat.ControlsSuspended && !motor.AcceptInput && !combat.TryAttack(false) && !ability.TryActivate(),"Defeat controls not stopped");
                    var visual=player.transform.Find("ClassVisual_"+chosen.id+"/BodyPivot"); Assert(Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.z,65))<2,"Death pose missing during dodge");
                    int actors=waves.Actors.Count; until=Time.realtimeSinceStartup+.3f; while(Time.realtimeSinceStartup<until) yield return null; Assert(waves.Actors.Count==actors,"Spawn continued after defeat");
                    ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Stage6_GameOver.png");yield return null;yield return null;
                    Button("RestartButton").onClick.Invoke();
                    // Wait for an actual new manager, not merely the still-loaded old scene name.
                    while(game!=null) yield return null; yield return null;yield return null;
                    game=UnityEngine.Object.FindAnyObjectByType<GameManager>();waves=game.Waves;player=game.Player;motor=player.GetComponent<ThirdPersonMotor>();combat=player.GetComponent<PlayerCombat>();ability=player.GetComponent<PlayerAbility>();health=player.GetComponent<CharacterHealth>();
                    Assert(player.Current==chosen && health.Current==chosen.health && waves.AliveCount==0 && game.State==MatchState.Preparing && !game.Gate.IsOpen && Time.timeScale==1,"Defeat restart was not fresh or lost class");
                    waves.ManualSimulation=true;motor.AcceptInput=false;Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=false;
                    Pass("Initial solitude/countdown; pause freezes waves and rejects combat; all blocked entries retry safely; fatal shared damage cancels pending spawns; death pose works during dodge; Game Over restart preserves class and resets run.");
                }
                var points=UnityEngine.Object.FindObjectsByType<ArenaSpawnPoint>(FindObjectsSortMode.None).Where(p=>p.Kind==ArenaSpawnKind.Enemy).ToArray();
                if(classIndex==0)
                {
                    Vector3 old=player.transform.position;var rotation=player.transform.rotation;motor.Teleport(points[0].transform.position,Quaternion.identity);Physics.SyncTransforms();
                    Assert(waves.TryFindSpawn(waves.Schedule.waves[0].groups[0].prefab,out var safe,out _) && Vector3.Distance(safe,player.transform.position)>=waves.Schedule.minimumPlayerDistance,"Spawn too close to player");motor.Teleport(old,rotation);Physics.SyncTransforms();
                }
                for(int w=0;w<5;w++)
                {
                    float wait=waves.Countdown; if(wait>.2f) { waves.Tick(wait-.1f);Assert(waves.State==(w==0?WaveState.Preparing:WaveState.Interval),"Countdown ended early"); }
                    waves.Tick(.11f); yield return null;FreezeEnemies(waves);
                    int expectedTotal=expected[w,0]+expected[w,1]+expected[w,2];
                    Assert(game.State==MatchState.Fighting,"Wave did not change match state");
                    if(w==0 && classIndex==0)
                    {
                        Kill(waves.Actors[0],player.transform);waves.Tick(.01f);Assert(waves.State==WaveState.Spawning && waves.PendingCount==1,"Wave completed before pending enemies spawned");
                    }
                    int safety=0;
                    while(waves.PendingCount>0 && safety++<40) { waves.Tick(1);yield return null;FreezeEnemies(waves); }
                    Assert(waves.PendingCount==0 && waves.Actors.Count==expectedTotal,"Spawn total mismatch on wave "+(w+1));
                    for(int role=0;role<3;role++) Assert(waves.Actors.Count(e=>(int)e.Definition.role==role)==expected[w,role],"Role composition mismatch");
                    foreach(var e in waves.Actors)
                    {
                        Assert(e.Target==player.GetComponent<DamageReceiver>() && e.PursueAssignedTarget,"Wave target assignment");
                        Assert(points.Any(p=>Vector3.Distance(p.transform.position,e.transform.position)<3.6f),"Spawn not near an entrance");
                        if(!e.GetComponent<CharacterHealth>().IsAlive) continue;
                        Assert(e.GetComponent<EnemyNavigation>().Ready,"Spawn outside NavMesh");var c=e.GetComponent<CharacterController>();
                        var overlap=Physics.OverlapCapsule(e.transform.position+Vector3.up*(c.radius+.12f),e.transform.position+Vector3.up*(c.height-c.radius),c.radius+.02f,~0,QueryTriggerInteraction.Ignore);
                        Assert(overlap.All(o=>o.transform.IsChildOf(e.transform)),"Spawn intersects arena or another actor");
                    }
                    waves.Tick(2); Assert(game.State==MatchState.Fighting,"Wave cleared with living enemies");
                    if(w==4 && classIndex==0) { ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Stage6_FinalWave.png");yield return null;yield return null; }
                    foreach(var enemy in waves.Actors) Kill(enemy,player.transform);waves.Tick(.01f);
                    Assert(waves.AliveCount==0,"Dead enemies counted alive");
                    if(w<4) Assert(game.State==MatchState.Intermission && waves.State==WaveState.Interval,"Intermission missing");
                }
                Assert(game.State==MatchState.Escape && !game.IsTerminal && waves.State==WaveState.Complete && waves.PendingCount==0,"Final wave did not unlock escape phase");
                int finalCount=waves.Actors.Count; waves.Tick(100);Assert(waves.Actors.Count==finalCount,"Spawning continued after final wave");
                float gateDeadline=Time.time+5;while(!game.Gate.IsOpen && Time.time<gateDeadline) yield return null;
                Assert(game.Gate.IsOpen && game.State==MatchState.Escape,"Gate did not fully open, or victory occurred before crossing");
                var gate=game.Gate.transform; var cam=Camera.main;cam.GetComponent<ThirdPersonCamera>().AcceptInput=false;cam.transform.rotation=gate.rotation;
                motor.Teleport(gate.TransformPoint(new Vector3(0,.08f,-1.5f)),gate.rotation);Physics.SyncTransforms();yield return null;
                for(int n=0;n<300 && game.State==MatchState.Escape;n++) { motor.ProcessMovement(Vector2.up,false,false,1f/60); yield return null; }
                Assert(game.State==MatchState.Victory && game.Exit.IsBeyondThreshold(player.transform),"Physical gate crossing did not win for "+chosen.displayName+" at "+player.transform.position);
                Assert(combat.ControlsSuspended && !motor.AcceptInput && Cursor.lockState==CursorLockMode.None,"Victory controls/cursor");
                yield return null;yield return null;
                if(classIndex==0) { ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Stage6_Victory.png");yield return null;yield return null; }
                Pass(chosen.displayName+": exact five wave compositions; no clear before all spawns/deaths; safe entrance placement; intervals; final gate opens; actual CharacterController crossing shows victory.");
                if(classIndex==3)
                {
                    Button("RestartButton").onClick.Invoke();while(game!=null) yield return null;yield return null;yield return null;
                    game=UnityEngine.Object.FindAnyObjectByType<GameManager>();Assert(game.Player.Current==chosen && game.State==MatchState.Preparing && game.Waves.AliveCount==0 && !game.Gate.IsOpen,"Victory restart failed");
                    game.TogglePause();yield return null;yield return null;Button("MenuButton").onClick.Invoke();
                }
                else Button("MenuButton").onClick.Invoke();
                while(SceneManager.GetActiveScene().name!="MainMenu") yield return null;yield return null;yield return null;
                Assert(Time.timeScale==1 && Cursor.lockState==CursorLockMode.None,"Menu retained pause or locked cursor");
                Button("SelectCharacterButton").onClick.Invoke();
                while(SceneManager.GetActiveScene().name!="GladiatorSelection") yield return null;yield return null;yield return null;
            }
            Pass("Victory restart, pause-to-menu and terminal menu buttons load fresh scenes; selected classes persist on restart; time scale and cursor restored. No training resurrection in survival.");
        }
    }
}
