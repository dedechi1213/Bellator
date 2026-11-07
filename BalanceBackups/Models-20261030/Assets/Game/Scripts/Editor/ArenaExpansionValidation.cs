using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using Object=UnityEngine.Object;

namespace Bellator.Editor
{
    /// <summary>Opt-in physical traversal, camera, lock, AI and performance checks on the expanded scene.</summary>
    [InitializeOnLoad]
    public static class ArenaExpansionValidation
    {
        const string Active="Bellator.Expansion.Validation";
        static IEnumerator routine;static int frame;static double deadline;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static ArenaExpansionValidation() { EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=State; }
        static void Update()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            { frame=Time.frameCount;try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Arena test deadline");if(!routine.MoveNext()) Finish(null); }catch(Exception e) { Finish(e); } }
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/BellatorExpansion.test")) return;
            File.Delete("Temp/BellatorExpansion.test");Run();
        }
        [MenuItem("Bellator/Arena Expansion/4 Test expanded arena in Play Mode")]
        public static void Run()
        { EditorSceneManager.OpenScene(ArenaBuilder.ScenePath);GameModeManager.Select(GameMode.Story);SessionState.SetBool(Active,true);EditorApplication.EnterPlaymode(); }
        static void State(PlayModeStateChange value)
        {
            if(value!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear();errors.Clear();report.AppendLine("BELLATOR expanded arena — "+DateTime.UtcNow.ToString("u")+" — Unity "+Application.unityVersion+" — real Play Mode");
            Application.logMessageReceived+=Log;routine=Checks();frame=-1;deadline=EditorApplication.timeSinceStartup+240;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("NavMeshAgent"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;routine=null;SessionState.SetBool(Active,false);Time.timeScale=1;
            if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);File.WriteAllText(ArenaExpansionTools.Evidence+"/PlayModeValidation.txt",report.ToString());File.WriteAllText("Temp/BellatorExpansion.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error);EditorApplication.ExitPlaymode();
        }
        static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message);File.WriteAllText("Temp/BellatorExpansion.progress.txt",report.ToString()); }
        static IEnumerator Checks()
        {
            yield return null;yield return null;var game=Object.FindAnyObjectByType<GameManager>();Require(game!=null,"Missing real match manager");
            var waves=game.Waves;waves.ManualSimulation=true;
            var player=game.Player;var motor=player.GetComponent<ThirdPersonMotor>();var orbit=Camera.main.GetComponent<ThirdPersonCamera>();var lockOn=player.GetComponent<PlayerLockOn>();var stamina=player.GetComponent<CharacterStamina>();
            motor.AcceptInput=false;orbit.AcceptInput=false;var camera=Camera.main;
            // Physical travel crosses the newly added floor in all directions, with no fixture geometry.
            foreach(float yaw in new[]{0f,45,90,135,180,225,270,315})
            {
                motor.Teleport(new Vector3(0,.08f,0),Quaternion.Euler(0,yaw,0));camera.transform.rotation=Quaternion.Euler(0,yaw,0);
                for(int step=0;step<72;step++) { stamina.Restore();motor.ProcessMovement(Vector2.up,true,false,.05f); }
                Require(motor.transform.position.y>-.1f && motor.transform.position.y<.2f,"Floor fall during radial run");
                Require(Vector3.ProjectOnPlane(motor.transform.position,Vector3.up).magnitude>16,"Radial run obstructed: "+yaw);
                int sequence=motor.DodgeSequence;stamina.Restore();motor.ProcessMovement(Vector2.right,false,true,.02f);
                Require(motor.DodgeSequence>sequence,"Dodge rejected on expanded floor");
                for(int n=0;n<55;n++) motor.ProcessMovement(Vector2.zero,false,false,.02f);
            }
            Pass("CharacterController running in 8 directions across the expanded floor, lateral dodges and recovery; stable floor height.");
            for(int segment=0;segment<8;segment++)
            {
                float angle=(22.5f+segment*45)*Mathf.Deg2Rad;
                Vector3 start=new Vector3(30.3f*Mathf.Sin(angle),.08f,27.3f*Mathf.Cos(angle));float yaw=Mathf.Atan2(Mathf.Sin(angle)/32,Mathf.Cos(angle)/29)*Mathf.Rad2Deg;
                motor.Teleport(start,Quaternion.Euler(0,yaw,0));camera.transform.rotation=Quaternion.Euler(0,yaw,0);
                for(int n=0;n<80;n++) { stamina.Restore();motor.ProcessMovement(Vector2.up,true,n==4,.05f); }
                Vector3 p=motor.transform.position;Require(p.x*p.x/(32*32)+p.z*p.z/(29*29)<1.01f,"Controller crossed oval wall: "+p);
                motor.Teleport(p,Quaternion.Euler(0,yaw+180,0));orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());
                for(int n=0;n<30;n++) orbit.UpdateOrbit(Vector2.zero,0,.02f);
                Require(orbit.CurrentDistance<4,"Camera did not retract at wall: "+orbit.CurrentDistance);
                Require(!Physics.OverlapSphere(camera.transform.position,.18f,~0,QueryTriggerInteraction.Ignore).Any(orbit.IsExternal),"Camera inside arena wall");
            }
            Pass("Run/dodge against 8 oval wall sectors stays inside; third-person camera retracts and remains outside solid walls.");
            foreach(Transform gate in GameObject.Find("Arena").transform.Find("Gates"))
            {
                motor.Teleport(gate.TransformPoint(new Vector3(0,.08f,-1.8f)),gate.rotation);orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());
                for(int n=0;n<36;n++)
                {
                    orbit.UpdateOrbit(new Vector2(85,0),0,.02f);
                    Require(!Physics.OverlapSphere(camera.transform.position,.18f,~0,QueryTriggerInteraction.Ignore).Any(orbit.IsExternal),"Camera inside portal: "+gate.name);
                }
            }
            Pass("Camera orbits at all five original portals without intersecting solid portal geometry.");
            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());orbit.UpdateOrbit(new Vector2(250,0),0,.02f);
            Require(Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y,0))>1,"Free orbit failed");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Gladiator.prefab").GetComponent<EnemyBrain>();
            var enemies=new List<EnemyBrain>();
            for(int n=0;n<3;n++) { var e=Object.Instantiate(prefab,new Vector3((n-1)*3,.08f,8),Quaternion.Euler(0,180,0));e.AutoThink=false;e.SetTarget(player.GetComponent<DamageReceiver>());enemies.Add(e); }
            yield return null;yield return null;
            foreach(var e in enemies) Require(e.GetComponent<EnemyNavigation>().Ready,"Enemy navigation not ready");
            camera.transform.position=new Vector3(0,3,-5);camera.transform.LookAt(new Vector3(0,1.4f,8));lockOn.Configure(camera);lockOn.ControlsSuspended=false;
            Require(lockOn.Toggle(),"Aim Lock acquisition failed");var first=lockOn.Target;Require(lockOn.SwitchTarget(1) && first!=lockOn.Target,"Aim Lock target switch failed");
            stamina.Restore();motor.ProcessMovement(Vector2.right,false,false,.1f);stamina.Restore();int dodge=motor.DodgeSequence;motor.ProcessMovement(Vector2.left,false,true,.02f);
            Require(motor.DodgeSequence>dodge && lockOn.IsLocked,"Locked strafe/roll failed");
            for(int n=0;n<50;n++) motor.ProcessMovement(Vector2.zero,false,false,.02f);
            lockOn.Clear();Pass("Free camera, Aim Lock acquisition/switching, lateral movement and roll with target retained.");
            foreach(var e in enemies) { e.gameObject.SetActive(false);Object.Destroy(e.gameObject); }yield return null;
            // Real pursuit and ranged retreat use actual agents and frame updates.
            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);var receiver=player.GetComponent<DamageReceiver>();
            var chaser=Object.Instantiate(prefab,new Vector3(22,.08f,10),Quaternion.identity);chaser.SetTarget(receiver);chaser.PursueAssignedTarget=true;
            float until=Time.time+3;while(Time.time<until) yield return null;chaser.AutoThink=false;chaser.GetComponent<EnemyNavigation>().Stop();
            Require(Vector3.Distance(chaser.transform.position,motor.transform.position)<21,"Enemy did not pursue across extended arena");
            chaser.gameObject.SetActive(false);Object.Destroy(chaser.gameObject);yield return null;
            var archerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Archer.prefab").GetComponent<EnemyBrain>();
            var archer=Object.Instantiate(archerPrefab,new Vector3(0,.08f,3.5f),Quaternion.Euler(0,180,0));archer.SetTarget(receiver);archer.PursueAssignedTarget=true;
            until=Time.time+2;while(Time.time<until) yield return null;archer.AutoThink=false;archer.GetComponent<EnemyNavigation>().Stop();
            Require(Vector3.Distance(archer.transform.position,motor.transform.position)>5,"Archer retreat blocked");
            archer.gameObject.SetActive(false);Object.Destroy(archer.gameObject);Pass("Real AI chases from extended floor; archer retreats through connected NavMesh.");yield return null;
            // Informational editor performance sample with fifteen real agents; never a hardware guarantee.
            for(int n=0;n<15;n++) { var e=Object.Instantiate(prefab,new Vector3((n%5-2)*3,.08f,6+n/5*3),Quaternion.identity);e.SetTarget(receiver);e.PursueAssignedTarget=true;enemies.Add(e); }
            motor.Teleport(new Vector3(0,.08f,-10),Quaternion.identity);orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());orbit.AcceptInput=true;
            int frames=0;float startTime=Time.realtimeSinceStartup;double sum=0;float worst=0;
            while(Time.realtimeSinceStartup-startTime<4) { yield return null;frames++;sum+=Time.unscaledDeltaTime;worst=Mathf.Max(worst,Time.unscaledDeltaTime); }
            report.AppendLine("EDITOR SAMPLE (not standalone benchmark): 15 AI; "+frames+" frames / "+(Time.realtimeSinceStartup-startTime).ToString("F2")+"s; average "+(frames/Math.Max(.001,sum)).ToString("F1")+" FPS; worst frame "+(worst*1000).ToString("F1")+"ms; Unity allocated "+(Profiler.GetTotalAllocatedMemoryLong()/1048576f).ToString("F1")+" MiB; reserved "+(Profiler.GetTotalReservedMemoryLong()/1048576f).ToString("F1")+" MiB.");
            ScreenCapture.CaptureScreenshot(ArenaExpansionTools.Evidence+"/Gameplay.png");yield return null;yield return null;
            Pass("15 simultaneous agents active during performance sample; no Bellator errors logged.");
        }
    }
}
