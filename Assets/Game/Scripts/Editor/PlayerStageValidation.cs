using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellator.Editor
{
    /// <summary>Opt-in Play Mode verification against the real arena colliders; no test packages required.</summary>
    [InitializeOnLoad]
    public static class PlayerStageValidation
    {
        private static IEnumerator routine;
        private static int frame = -1;
        private static readonly StringBuilder report = new StringBuilder();
        private const string Active = "Bellator.Stage2.Validation";
        static PlayerStageValidation()
        {
            EditorApplication.delayCall += CheckRequest;
            EditorApplication.playModeStateChanged += StateChanged;
            EditorApplication.update += Tick;
            EditorApplication.update += CheckRequest;
        }
        private static void CheckRequest()
        {
            const string path = "Temp/BellatorPlayer.test";
            if (!File.Exists(path)) return;
            File.Delete(path);
            Run();
        }
        [MenuItem("Bellator/Test Stage 2 in Play Mode")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>()) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ArenaBuilder.ScenePath);
            SessionState.SetBool(Active,true);
            EditorApplication.EnterPlaymode();
        }
        private static void StateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active,false))
            {
                report.Clear(); report.AppendLine("Stage 2 — Unity "+Application.unityVersion+" — Play Mode");
                routine=Checks(); frame=-1;
            }
        }
        private static void Tick()
        {
            if(routine==null || !EditorApplication.isPlaying || frame==Time.frameCount) return;
            frame=Time.frameCount;
            try { if(!routine.MoveNext()) Finish(null); }
            catch(Exception e) { Finish(e); }
        }
        private static void Finish(Exception error)
        {
            routine=null; SessionState.SetBool(Active,false);
            report.AppendLine(error==null ? "RESULT: PASS" : "RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/Stage2Validation.txt",report.ToString());
            File.WriteAllText("Temp/BellatorPlayer.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error); else Debug.Log("Bellator Stage 2: Play Mode verification passed.");
            EditorApplication.ExitPlaymode();
        }
        private static void Assert(bool condition,string message)
        { if(!condition) throw new InvalidOperationException(message); }
        private static void Pass(string message) => report.AppendLine("PASS: "+message);
        private static IEnumerator Checks()
        {
            MatchStageBuilder.PrepareLegacySandbox();
            var encounter=UnityEngine.Object.FindAnyObjectByType<Bellator.Enemies.EnemyEncounter>();
            if(encounter!=null) encounter.gameObject.SetActive(false);
            yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<Bellator.Classes.PlayerClassController>()?.RestoreLegacyForValidation();
            var motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();
            var camera=UnityEngine.Object.FindAnyObjectByType<ThirdPersonCamera>();
            var stamina=motor.GetComponent<CharacterStamina>(); var health=motor.GetComponent<CharacterHealth>();
            var input=motor.GetComponent<ThirdPersonInput>();
            motor.AcceptInput=false; camera.AcceptInput=false;
            var keyboard=InputSystem.AddDevice<Keyboard>("BellatorValidationKeyboard");
            var mouse=InputSystem.AddDevice<Mouse>("BellatorValidationMouse");
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift,Key.Space));
                InputSystem.QueueStateEvent(mouse,new MouseState { delta=new Vector2(12,-6),scroll=new Vector2(0,120) });
                InputSystem.Update();
                var actions=InputSystem.ListEnabledActions().Where(a=>a.actionMap!=null && a.actionMap.name=="BellatorThirdPerson").ToArray();
                Assert(actions.First(a=>a.name=="Move").ReadValue<Vector2>().y>.9f,"W binding failed");
                Assert(actions.First(a=>a.name=="Sprint").IsPressed(),"Shift binding failed");
                Assert(actions.First(a=>a.name=="Dodge").IsPressed(),"Space binding failed");
                Assert(actions.First(a=>a.name=="Look").ReadValue<Vector2>().x>0,"Mouse binding failed");
                Assert(actions.First(a=>a.name=="Zoom").ReadValue<Vector2>().y>0,"Wheel binding failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A)); InputSystem.Update();
                Assert(actions.First(a=>a.name=="Move").ReadValue<Vector2>().x<-.9f,"A binding failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S)); InputSystem.Update();
                Assert(actions.First(a=>a.name=="Move").ReadValue<Vector2>().y<-.9f,"S binding failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); InputSystem.Update();
                Assert(actions.First(a=>a.name=="Move").ReadValue<Vector2>().x>.9f,"D binding failed");
                Pass("Input System: WASD, Shift, Space, mouse delta and wheel.");
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); }
            input.enabled=false;
            health.Damage(200); Assert(health.Current==0,"Health lower clamp");
            health.Heal(200); Assert(health.Current==health.Maximum,"Health upper clamp"); health.Restore();
            stamina.Restore(); Assert(!stamina.TryConsume(stamina.Maximum+1),"Stamina overdraft"); Assert(!stamina.TryConsume(-1),"Negative stamina consumption"); Pass("Independent health/stamina bounds.");

            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
            for(int i=0;i<60;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(motor.transform.position.y>-.1f && motor.transform.position.y<.15f,"Ground stability");
            Pass("Gravity settles capsule onto sand without falling through.");
            Vector3 start=motor.transform.position;
            for(int i=0;i<120;i++){ Step(motor,camera,Vector2.up); if(i==60) Capture(camera,"Walk"); yield return null; }
            float walk=Vector3.Distance(start,motor.transform.position);
            Assert(walk>6 && walk<7.3f,"Walk distance "+walk);
            for(int i=0;i<45;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(motor.PlanarSpeed<.05f,"Deceleration failed");
            Pass("Walk/acceleration/deceleration: "+walk.ToString("F2")+" m over 2 simulated seconds.");

            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
            for(int i=0;i<10;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            stamina.Restore(); start=motor.transform.position;
            for(int i=0;i<120;i++){ Step(motor,camera,Vector2.up,true); if(i==60) Capture(camera,"Run"); yield return null; }
            float run=Vector3.Distance(start,motor.transform.position);
            Assert(run>walk+2 && stamina.Current<85,"Sprint speed/drain");
            Pass("Sprint faster than walk ("+run.ToString("F2")+" m); stamina consumed.");

            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
            for(int i=0;i<10;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            stamina.Restore(); start=motor.transform.position;
            Step(motor,camera,Vector2.up,false,true);
            Assert(motor.IsDodging && Mathf.Abs(stamina.Current-72)<.1f,"Dodge start/cost"); yield return null;
            for(int i=0;i<26;i++){ Step(motor,camera,Vector2.zero); if(i==12) Capture(camera,"Dodge"); yield return null; }
            float dodge=Vector3.Distance(start,motor.transform.position);
            Assert(!motor.IsDodging && Mathf.Abs(dodge-3.6f)<.2f,"Dodge duration/distance "+dodge);
            float before=stamina.Current; Step(motor,camera,Vector2.up,false,true);
            Assert(!motor.IsDodging && stamina.Current>=before-.01f,"Dodge cooldown"); yield return null;
            Pass("Dodge: 28 stamina, 0.42 s, "+dodge.ToString("F2")+" m; repeat blocked during cooldown.");

            motor.Teleport(new Vector3(0,.08f,-21),Quaternion.identity);
            for(int i=0;i<150;i++){ Step(motor,camera,Vector2.down); yield return null; }
            Assert(motor.transform.position.z>-24.2f && motor.transform.position.y>-.1f,"Wall walk collision");
            Pass("Continuous walking stopped by southern wall.");
            motor.Teleport(new Vector3(0,.08f,-22.5f),Quaternion.identity);
            for(int i=0;i<10;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            stamina.Restore(); Step(motor,camera,Vector2.down,false,true); yield return null;
            for(int i=0;i<30;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(motor.transform.position.z>-24.2f,"Dodge crossed wall");
            Pass("Dodge stopped by solid wall.");
            motor.Teleport(new Vector3(-9,.08f,-7),Quaternion.identity);
            for(int i=0;i<100;i++){ Step(motor,camera,Vector2.up); yield return null; }
            Assert(motor.transform.position.z< -5.15f,"Obstacle collision");
            Pass("Stone obstacle blocks walking.");

            motor.Teleport(new Vector3(0,.08f,-22.8f),Quaternion.identity);
            camera.Configure(motor.transform,input);
            for(int i=0;i<40;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(camera.CurrentDistance<1.5f,"Camera did not retract at wall: "+camera.CurrentDistance);
            Assert(!Physics.OverlapSphere(camera.transform.position,.25f).Any(c=>camera.IsExternal(c)),"Camera penetrates obstacle");
            Pass("Camera retracts before wall; camera collision volume remains clear.");
            camera.UpdateOrbit(new Vector2(0,10000),0,.016f); Assert(camera.Pitch>=-25,"Minimum pitch");
            camera.UpdateOrbit(new Vector2(0,-10000),0,.016f); Assert(camera.Pitch<=65,"Maximum pitch");
            Pass("Mouse orbit pitch clamped to -25..65 degrees.");

            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
            camera.Configure(motor.transform,input);
            camera.UpdateOrbit(new Vector2(0,391.6667f),-10000,.016f);
            for(int i=0;i<90;i++){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(camera.CurrentDistance>7.8f && camera.CurrentDistance<=8.01f,"Camera recovery/maximum zoom");
            camera.UpdateOrbit(Vector2.zero,10000,.016f);
            Assert(camera.CurrentDistance>=1.49f && camera.CurrentDistance<=1.51f,"Minimum zoom");
            Pass("Camera extends smoothly in free space; wheel distance clamped to 1.5..8 m.");
            stamina.Restore(); stamina.TryConsume(50); float drained=stamina.Current;
            float until=Time.time+2.2f;
            while(Time.time<until){ Step(motor,camera,Vector2.zero); yield return null; }
            Assert(stamina.Current>drained,"Stamina regeneration");
            Pass("Stamina regenerates after consumption delay.");
        }
        private static void Capture(ThirdPersonCamera orbit,string pose)
        {
            var camera=orbit.GetComponent<Camera>(); var rt=new RenderTexture(960,600,24);
            var previous=RenderTexture.active; camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var image=new Texture2D(960,600,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,960,600),0,0); image.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/Player"+pose+".png",image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        }        private static void Step(ThirdPersonMotor motor,ThirdPersonCamera camera,Vector2 axis,bool sprint=false,bool dodge=false)
        { motor.ProcessMovement(axis,sprint,dodge,1f/60); Physics.SyncTransforms(); camera.UpdateOrbit(Vector2.zero,0,1f/60); }
    }
}






