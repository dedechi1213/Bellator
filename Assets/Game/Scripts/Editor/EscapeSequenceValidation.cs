using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Object=UnityEngine.Object;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class EscapeSequenceValidation
    {
        const string Key="Bellator.Escape.Validation";
        static IEnumerator routine;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static int frame=-1; static double deadline;
        static EscapeSequenceValidation() { EditorApplication.update+=Poll; EditorApplication.playModeStateChanged+=State; }
        static void Poll()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;
                try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Escape validation timed out"); if(!routine.MoveNext()) Finish(null); }
                catch(Exception ex) { Finish(ex); }
            }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/EscapeSequence.test")) return;
            File.Delete("Temp/EscapeSequence.test");
            if(SceneManager.GetActiveScene().isDirty) { File.WriteAllText("Logs/Escape-validation.txt","BLOCKED: unsaved scene edits"); return; }
            EditorSceneManager.OpenScene(GameInterface.MainScene); SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            report.Clear(); errors.Clear(); report.AppendLine("BELLATOR PHYSICAL EXIT / Unity "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
            deadline=EditorApplication.timeSinceStartup+420; frame=-1; routine=Flatten(Checks()); Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        { if((type==LogType.Exception || type==LogType.Error || type==LogType.Assert) && (stack.Contains("Bellator.") || message.Contains("NullReference") || message.Contains("NavMeshAgent"))) errors.Add(message); }
        static void Require(bool value,string message) { if(!value) throw new Exception(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message); File.WriteAllText("Logs/Escape-validation.txt",report.ToString()); }
        static IEnumerator Flatten(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>(); stack.Push(root);
            while(stack.Count>0) { var current=stack.Peek(); if(!current.MoveNext()) { stack.Pop(); continue; } if(current.Current is IEnumerator nested) stack.Push(nested); else yield return null; }
        }
        static IEnumerator Load(string path)
        { var load=SceneManager.LoadSceneAsync(path,LoadSceneMode.Single); while(!load.isDone) yield return null; yield return null; yield return null; }
        static IEnumerator Checks()
        {
            yield return null; yield return null; PlayerSession.EnterGuest();
            var catalog=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            for(int index=0;index<catalog.classes.Length;index++)
            {
                ClassSelectionState.Confirm(catalog.classes[index]); GameModeManager.Select(GameMode.Story);
                yield return Load(PlayerClassController.ArenaScene);
                var game=Object.FindAnyObjectByType<GameManager>(); var sequence=game.GetComponent<EscapeVictorySequence>();
                var player=game.Player; var motor=player.GetComponent<ThirdPersonMotor>(); var combat=player.GetComponent<PlayerCombat>();
                var gate=game.Gate.transform; var waves=game.Waves; waves.ManualSimulation=true;
                motor.AcceptInput=false; player.GetComponent<ThirdPersonInput>().enabled=false; player.GetComponent<CombatInput>().enabled=false;
                Require(sequence!=null && player.Current==catalog.classes[index] && waves.WaveCount==5,"Wrong actor, scene or wave schedule");
                Require(!game.Gate.IsOpen && !sequence.TryBegin(game,player.transform),"Escape began before five completed waves");
                var direction=new GameObject("ValidationMovementReference").transform; direction.rotation=gate.rotation; motor.Configure(direction);
                var initial=player.transform.position; var initialRotation=player.transform.rotation;
                motor.Teleport(gate.TransformPoint(new Vector3(0,.08f,-2)),gate.rotation); Physics.SyncTransforms();
                for(int step=0;step<90;step++) { motor.ProcessMovement(Vector2.up,false,false,1f/60); yield return null; }
                Require(gate.InverseTransformPoint(player.transform.position).z<0 && game.Gate.OpenCount==0,"Closed gate did not physically block the actor");
                motor.Teleport(initial,initialRotation); Physics.SyncTransforms();
                int safety=0;
                while(game.State!=MatchState.Escape)
                {
                    Require(++safety<350,"Five-wave progression stalled");
                    waves.Tick(100);
                    foreach(var enemy in waves.Actors.ToArray())
                    {
                        enemy.AutoThink=false; enemy.GetComponent<EnemyNavigation>().Stop();
                        if(enemy.GetComponent<CharacterHealth>().IsAlive) Require(enemy.GetComponent<DamageReceiver>().Receive(new DamageInfo(100000,enemy.transform.position+Vector3.up,player.transform.position,player.transform)),"Shared damage could not defeat wave enemy");
                    }
                    if(waves.State!=WaveState.Complete) Require(!game.Gate.IsOpen && game.Gate.OpenCount==0,"Gate opened while waves, pending spawns or live enemies remained");
                    yield return null;
                }
                Require(waves.WaveNumber==5 && waves.State==WaveState.Complete && waves.AliveCount==0 && waves.PendingCount==0,"Exit unlocked without clearing all five waves");
                int count=game.Gate.OpenCount; game.Gate.Open(); game.Gate.Open(); Require(count==1 && game.Gate.OpenCount==1,"Gate opening restarted");
                float openDeadline=Time.realtimeSinceStartup+8; while(!game.Gate.IsOpen && Time.realtimeSinceStartup<openDeadline) yield return null;
                Require(game.Gate.IsOpen && game.State==MatchState.Escape,"Gate failed to open or victory fired prematurely");
                Physics.SyncTransforms();
                foreach(var hit in Physics.SphereCastAll(gate.TransformPoint(new Vector3(0,1.3f,-1)),.6f,gate.forward,41,~0,QueryTriggerInteraction.Ignore))
                    Require(hit.collider.transform.IsChildOf(player.transform),"Physical corridor blocked by "+hit.collider.name+" at "+hit.point);
                for(float z=0;z<=40;z+=.75f) Require(Physics.Raycast(gate.TransformPoint(new Vector3(0,.6f,z)),Vector3.down,out var floor,1.5f,~0,QueryTriggerInteraction.Ignore) && Mathf.Abs(floor.point.y-gate.position.y)<.12f,"Floor discontinuity at "+z);
                Pass(catalog.classes[index].displayName+": closed gate physically blocks; no early opening through five real waves; single opening; continuous floor and clear physical route.");
                motor.Teleport(gate.TransformPoint(new Vector3(0,.08f,-2)),gate.rotation); motor.Configure(direction); Physics.SyncTransforms();
                var animator=player.GetComponent<PlayerAnimationBridge>().Animator; int visualId=animator.GetInstanceID();
                for(int step=0;step<300 && !sequence.IsPlaying;step++) { motor.ProcessMovement(Vector2.up,false,false,1f/60); yield return null; }
                Require(sequence.IsPlaying && game.State==MatchState.Escape,"Physical entry did not trigger cinematic");
                Require(!sequence.TryBegin(game,player.transform),"Cutscene started twice");
                bool walking=false,behind=false,wide=false,stopped=false; Vector3 previous=player.transform.position;
                Vector3 firstCamera=Camera.main.transform.position; float cameraTravel=0; Vector3 previousCamera=firstCamera;
                var shots=new HashSet<string>(); float finishDeadline=Time.realtimeSinceStartup+65;
                while(!sequence.Completed && Time.realtimeSinceStartup<finishDeadline)
                {
                    Require(sequence.IsPlaying,"Cutscene aborted");
                    Require(!player.GetComponent<ThirdPersonInput>().enabled && !player.GetComponent<CombatInput>().enabled && !combat.TryAttack(false),"User retained combat/movement control during cutscene");
                    Require(GameObject.Find("CombatHUD")==null,"Combat UI visible during cutscene");
                    Require(Vector3.Distance(previous,player.transform.position)<.45f,"Visible character teleport during cinematic"); previous=player.transform.position;
                    Require(player.transform.position.y>gate.position.y-.25f,"Actor fell through passage");
                    var camera=Camera.main;
                    Require(camera!=null && !camera.GetComponent<ThirdPersonCamera>().enabled,"Player camera did not hand control to cinematic");
                    cameraTravel+=Vector3.Distance(previousCamera,camera.transform.position); previousCamera=camera.transform.position;
                    foreach(var obstacle in Physics.OverlapSphere(camera.transform.position,.11f,~0,QueryTriggerInteraction.Ignore))
                        Require(obstacle.transform.IsChildOf(player.transform),"Cinematic camera clipped inside "+obstacle.name);
                    walking|=motor.PlanarSpeed>.2f && animator.GetFloat("Speed")>.01f;
                    behind|=sequence.Shot=="Caminhando para a liberdade"; wide|=sequence.Shot=="Saindo do Coliseu"; stopped|=sequence.Shot=="Observando o exterior";
                    if(index==0 && sequence.Shot.Length>0 && shots.Add(sequence.Shot)) Capture(Camera.main,"Cinematic_"+shots.Count);
                    yield return null;
                }
                Require(sequence.Completed && game.State==MatchState.Victory && sequence.DistanceWalked>25,"No real walk to exterior or no final victory");
                Require(walking && behind && wide && stopped && cameraTravel>12 && animator.GetInstanceID()==visualId,"Missing shots, real walking animation or original actor identity");
                Require(gate.InverseTransformPoint(player.transform.position).z>38 && sequence.FadeAmount>.99f,"Player did not reach outside before fade");
                Require(game.FinalStatistics!=null && game.FinalStatistics.wavesCompleted==5,"Final five-wave victory not registered");
                yield return null; yield return null;
                Require(GameObject.Find("ResultTitle")?.GetComponent<Text>()?.text=="A LIBERDADE FOI CONQUISTADA" || Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="A LIBERDADE FOI CONQUISTADA"),"Victory message missing");
                Pass(catalog.classes[index].displayName+": real original actor/Animator walked >25m with collisions; four cinematic stages; HUD/control suspension; no duplicate/teleport/fall; fade and five-wave victory.");
                Object.Destroy(direction.gameObject);
                GameObject.Find("MenuButton").GetComponent<Button>().onClick.Invoke();
                while(SceneManager.GetActiveScene().name!="MainMenu") yield return null; yield return null;
                Require(Object.FindAnyObjectByType<EscapeVictorySequence>()==null && Time.timeScale==1 && Cursor.visible,"Menu return leaked cinematic state");
            }
            GameModeManager.Select(GameMode.Infinite); ClassSelectionState.Confirm(catalog.classes[0]); yield return Load(PlayerClassController.ArenaScene);
            var infinite=Object.FindAnyObjectByType<GameManager>(); var isolated=infinite.GetComponent<EscapeVictorySequence>();
            Require(infinite.IsInfinite && infinite.Waves.IsInfinite && !isolated.TryBegin(infinite,infinite.Player.transform) && !infinite.Gate.IsOpen,"Infinite mode contaminated by escape sequence");
            Pass("Infinite mode keeps infinite rules and closed gate; cinematic refuses activation. Shared online arena/prefab are checked separately against backup/Git.");
            infinite.ReturnToMenu(); while(SceneManager.GetActiveScene().name!="MainMenu") yield return null;
        }
        static void Capture(Camera camera,string name)
        {
            if(camera==null) return;
            var rt=new RenderTexture(1280,800,24); var previous=RenderTexture.active; var target=camera.targetTexture;
            try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; var image=new Texture2D(1280,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply(); File.WriteAllBytes(EscapeSequenceTools.Evidence+"/"+name+".png",image.EncodeToPNG()); Object.DestroyImmediate(image); }
            finally { camera.targetTexture=target; RenderTexture.active=previous; rt.Release(); Object.DestroyImmediate(rt); }
        }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log; routine=null; SessionState.SetBool(Key,false);
            if(error==null && errors.Count>0) error=new Exception(string.Join("\n",errors));
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Logs/Escape-validation.txt",report.ToString()); Directory.CreateDirectory(EscapeSequenceTools.Evidence);
            File.WriteAllText(EscapeSequenceTools.Evidence+"/PlayModeValidation.txt",report.ToString());
            if(error!=null) Debug.LogException(error);
            Time.timeScale=1; EditorApplication.ExitPlaymode();
        }
    }
}
