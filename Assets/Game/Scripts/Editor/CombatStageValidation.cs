using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class CombatStageValidation
    {
        private const string Active="Bellator.Stage3.Test";
        private static IEnumerator routine;
        private static int frame=-1;
        private static readonly System.Collections.Generic.List<string> runtimeErrors=new System.Collections.Generic.List<string>();
        private static readonly StringBuilder report=new StringBuilder();
        static CombatStageValidation()
        {
            EditorApplication.update+=CheckRequest; EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=StateChanged;
        }
        private static void CheckRequest()
        {
            const string request="Temp/BellatorCombat.test";
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            File.Delete(request); Run();
        }
        [MenuItem("Bellator/Test Stage 3 Combat in Play Mode")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!UnityEngine.Object.FindAnyObjectByType<PlayerCombat>()) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ArenaBuilder.ScenePath);
            SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode();
        }
        private static void StateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active,false))
            { report.Clear(); report.AppendLine("Stage 3 — Unity "+Application.unityVersion+" — Play Mode"); runtimeErrors.Clear(); Application.logMessageReceived+=CaptureError; routine=Checks(); frame=-1; }
        }
        private static void Tick()
        {
            if(routine==null || !EditorApplication.isPlaying || frame==Time.frameCount) return;
            frame=Time.frameCount;
            try { if(!routine.MoveNext()) Finish(null); } catch(Exception e) { Finish(e); }
        }
        private static void Finish(Exception error)
        {
            Application.logMessageReceived-=CaptureError;
            if(error==null && runtimeErrors.Count>0) error=new InvalidOperationException(string.Join("\\n",runtimeErrors));
            routine=null; SessionState.SetBool(Active,false);
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/Stage3Validation.txt",report.ToString());
            File.WriteAllText("Temp/BellatorCombat.test-result.txt",report.ToString());
            if(error==null) Debug.Log("Bellator Stage 3 Play Mode validation passed."); else Debug.LogException(error);
            EditorApplication.ExitPlaymode();
        }
        private static void CaptureError(string message,string trace,LogType type)
        {
            if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("Bellator"))) runtimeErrors.Add(message);
        }
        private static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        private static void Pass(string value) { report.AppendLine("PASS: "+value); File.WriteAllText("Temp/BellatorCombat.progress.txt",report.ToString()); }
        private static IEnumerator Checks()
        {
            MatchStageBuilder.PrepareLegacySandbox();
            var encounter=UnityEngine.Object.FindAnyObjectByType<Bellator.Enemies.EnemyEncounter>();
            if(encounter!=null) encounter.gameObject.SetActive(false);
            yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<Bellator.Classes.PlayerClassController>()?.RestoreLegacyForValidation();
            var combat=UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            var motor=combat.GetComponent<ThirdPersonMotor>(); var input=combat.GetComponent<CombatInput>();
            var equipment=combat.GetComponent<WeaponEquipment>(); var guard=combat.GetComponent<ShieldGuard>();
            var health=combat.GetComponent<CharacterHealth>(); var stamina=combat.GetComponent<CharacterStamina>(); var receiver=combat.GetComponent<DamageReceiver>();
            var camera=Camera.main; var orbit=camera.GetComponent<ThirdPersonCamera>();
            var target=GameObject.Find("TrainingDummy_1").GetComponent<DamageReceiver>();
            combat.ManualSimulation=true; motor.AcceptInput=false; orbit.AcceptInput=false;
            var keyboard=InputSystem.AddDevice<Keyboard>("CombatValidationKeyboard");
            var mouse=InputSystem.AddDevice<Mouse>("CombatValidationMouse");
            try
            {
                InputSystem.QueueStateEvent(mouse,new MouseState{buttons=3}); InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E,Key.R,Key.Digit3));
                InputSystem.Update();
                var actions=InputSystem.ListEnabledActions().Where(a=>a.actionMap!=null && a.actionMap.name=="BellatorCombat").ToArray();
                Assert(actions.First(a=>a.name=="Light").IsPressed() && actions.First(a=>a.name=="Guard").IsPressed(),"Mouse combat bindings");
                Assert(actions.First(a=>a.name=="Heavy").IsPressed() && actions.First(a=>a.name=="Reload").IsPressed() && actions.First(a=>a.name=="Slot3").IsPressed(),"Keyboard combat bindings");
                input.RebindHeavy("<Keyboard>/f");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F)); InputSystem.Update();
                Assert(actions.First(a=>a.name=="Heavy").IsPressed(),"Configurable heavy binding");
                input.RebindHeavy("<Keyboard>/e"); Pass("Combat Input System bindings and configurable heavy key.");
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); }
            input.enabled=false; guard.enabled=false;
            Assert(equipment.Loadout.Length==4 && equipment.Loadout.All(w=>w!=null && w.visualPrefab!=null),"Loadout references");
            Pass("Four weapon definitions, shield, sockets and visual/projectile references present.");
            motor.Teleport(target.transform.position-Vector3.forward*1.3f,Quaternion.identity); Physics.SyncTransforms();
            camera.transform.position=motor.transform.position+new Vector3(3,2.7f,-4); camera.transform.LookAt(motor.transform.position+Vector3.up*1.2f);
            equipment.Equip(0); combat.ResetTrainingState(); stamina.Restore(); target.Health.Restore();
            int hits=combat.ConfirmedHits;
            Assert(combat.TryAttack(false),"Start light attack");
            Assert(!combat.TryAttack(true),"Heavy bypassed interval");
            for(int i=0;i<45;i++) { combat.TickCombat(1f/60); if(i==12) Capture(camera,"Sword"); yield return null; }
            Assert(Mathf.Abs(target.Health.Current-76)<.05f && combat.ConfirmedHits-hits==1,"Single-swing duplicate damage: "+target.Health.Current);
            Pass("Light attack damages once across multiple frames/colliders; interval enforced; stamina consumed.");

            combat.ResetTrainingState(); stamina.Restore(); target.Health.Restore(); hits=combat.ConfirmedHits;
            Assert(combat.TryAttack(false),"Combo start");
            for(int i=0;i<12;i++){combat.TickCombat(1f/60); yield return null;}
            combat.TryAttack(false);
            for(int i=0;i<36;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(combat.ComboStep==2 && combat.ConfirmedHits-hits==2,"Buffered combo continuation");
            for(int i=0;i<35;i++){combat.TickCombat(1f/60); yield return null;}
            target.Health.Restore(); stamina.Restore(); Assert(combat.TryAttack(false) && combat.ComboStep==3,"Combo finisher");
            for(int i=0;i<65;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(target.Health.Current<70,"Finisher multiplier");
            Pass("Buffered light combo and third-step finisher.");

            combat.ResetTrainingState(); equipment.Equip(1); target.Health.Restore(); stamina.Restore();
            Assert(combat.TryAttack(true),"Heavy sword attack");
            for(int i=0;i<120;i++){combat.TickCombat(1f/60); if(i==32) Capture(camera,"Heavy"); yield return null;}
            Assert(Mathf.Abs(target.Health.Current-(100-38*1.9f))<.1f,"Heavy damage");
            Pass("Heavy sword configured reach, heavy damage and timing.");
            equipment.Equip(0); combat.ResetTrainingState(); stamina.Restore(); target.Health.Restore();
            motor.Teleport(target.transform.position-Vector3.forward*1.3f,Quaternion.Euler(0,180,0)); Physics.SyncTransforms();
            combat.TryAttack(false); for(int i=0;i<50;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(target.Health.Current==100,"Attack hit behind player");
            motor.Teleport(target.transform.position-Vector3.forward*4,Quaternion.identity); Physics.SyncTransforms();
            stamina.Restore(); combat.TryAttack(false); for(int i=0;i<50;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(target.Health.Current==100,"Out-of-range melee");
            Pass("Melee respects facing and range.");

            motor.Teleport(target.transform.position-Vector3.forward*1.3f,Quaternion.identity); Physics.SyncTransforms();
            var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube); barrier.name="ValidationBarrier";
            barrier.transform.position=target.transform.position+new Vector3(0,1.3f,-.7f); barrier.transform.localScale=new Vector3(2,3,.12f); Physics.SyncTransforms();
            stamina.Restore(); combat.TryAttack(false); for(int i=0;i<55;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(target.Health.Current==100,"Melee passed through barrier");
            UnityEngine.Object.Destroy(barrier); yield return null; Physics.SyncTransforms();
            Pass("Melee cannot hit through solid geometry.");

            combat.ResetTrainingState(); stamina.Restore(); health.Restore(); guard.ResetGuard(); guard.Request(true);
            receiver.Receive(new DamageInfo(24,motor.transform.position+Vector3.up,motor.transform.position+motor.transform.forward*2,null));
            Assert(receiver.LastImpact.Blocked && Mathf.Abs(health.Current-95.2f)<.1f && stamina.Current<100,"Frontal shield reduction/cost");
            for(int i=0;i<4;i++) yield return null; Capture(camera,"Shield");
            health.Restore(); stamina.Restore();
            receiver.Receive(new DamageInfo(24,motor.transform.position+Vector3.up,motor.transform.position-motor.transform.forward*2,null));
            Assert(!receiver.LastImpact.Blocked && health.Current==76,"Rear shield protection");
            health.Restore(); stamina.Restore(); stamina.TryConsume(98);
            receiver.Receive(new DamageInfo(24,motor.transform.position+Vector3.up,motor.transform.position+motor.transform.forward*2,null));
            Assert(guard.IsBroken && receiver.LastImpact.GuardBroken && health.Current==76 && stamina.Current==0,"Guard break");
            stamina.Restore(); health.Restore();
            receiver.Receive(new DamageInfo(24,motor.transform.position+Vector3.up,motor.transform.position+motor.transform.forward*2,null));
            Assert(health.Current==76,"Broken guard cooldown bypassed");
            guard.enabled=true;
            float brokenUntil=Time.time+1.3f;
            while(Time.time<brokenUntil) yield return null;
            Assert(!guard.IsBroken,"Guard break recovery timer");
            guard.enabled=false;
            equipment.Equip(1); guard.ResetGuard(); guard.Request(true); health.Restore(); stamina.Restore();
            Assert(!guard.IsBlocking,"No-shield block");
            receiver.Receive(new DamageInfo(24,motor.transform.position+Vector3.up,motor.transform.position+motor.transform.forward*2,null));
            Assert(health.Current==76,"No-shield damage");
            Pass("Shield frontal reduction, stamina cost, rear exposure, guard break/recovery and no-shield restriction.");

            guard.Request(false); equipment.Equip(0); guard.ResetGuard(); guard.Request(true); stamina.Restore(); guard.enabled=true;
            float startStamina=stamina.Current;
            for(int i=0;i<20;i++) yield return null;
            Assert(stamina.Current<startStamina,"Guard hold drain");
            guard.enabled=false; guard.Request(false);
            Pass("Holding shield consumes stamina continuously.");

            health.Restore(); target.Health.Restore();
            motor.Teleport(target.transform.position-Vector3.forward*3,Quaternion.identity); Physics.SyncTransforms();
            var arrowPrefab=equipment.Loadout[2].projectilePrefab;
            var arrow=UnityEngine.Object.Instantiate(arrowPrefab,motor.transform.position+Vector3.up*1.3f,Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(motor.transform,17,Vector3.forward*100,0,2,20); arrow.Simulate(.3f);
            Assert(arrow.HasHit && target.Health.Current==83 && health.Current==100,"Swept projectile/self collision");
            arrow.Simulate(.2f); Assert(target.Health.Current==83,"Projectile repeated damage");
            yield return null;
            Pass("High-speed projectile sweep, owner filtering and one impact per projectile.");

            target.Health.Restore();
            barrier=GameObject.CreatePrimitive(PrimitiveType.Cube); barrier.transform.position=target.transform.position+new Vector3(0,1.3f,-1.4f); barrier.transform.localScale=new Vector3(2,3,.12f); Physics.SyncTransforms();
            arrow=UnityEngine.Object.Instantiate(arrowPrefab,motor.transform.position+Vector3.up*1.3f,Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(motor.transform,17,Vector3.forward*100,0,2,20); arrow.Simulate(.3f);
            Assert(arrow.HasHit && target.Health.Current==100,"Projectile crossed wall");
            UnityEngine.Object.Destroy(barrier); yield return null; Physics.SyncTransforms();
            arrow=UnityEngine.Object.Instantiate(arrowPrefab,new Vector3(0,5,-15),Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(motor.transform,10,Vector3.right*5,9.81f,2,20); arrow.Simulate(.2f);
            Assert(Mathf.Abs(arrow.transform.position.y-(5-.1962f))<.01f,"Ballistic gravity");
            arrow.Simulate(3); Assert(arrow.HasHit,"Projectile lifetime"); yield return null;
            arrow=UnityEngine.Object.Instantiate(arrowPrefab,new Vector3(0,5,-15),Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(motor.transform,10,Vector3.right*10,0,10,1); arrow.Simulate(.2f);
            Assert(arrow.HasHit && Mathf.Abs(arrow.transform.position.x-1)<.01f,"Projectile range"); yield return null;
            arrow=UnityEngine.Object.Instantiate(arrowPrefab,new Vector3(0,30,-15),Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(motor.transform,10,Vector3.zero,0,.1f,20); arrow.Simulate(.2f);
            Assert(arrow.HasHit,"Lifetime without any collision"); yield return null;
            Pass("Projectile walls, gravity, lifetime and maximum range.");

            motor.Teleport(target.transform.position-Vector3.forward*6,Quaternion.identity); Physics.SyncTransforms();
            camera.transform.position=motor.transform.position+new Vector3(0,2.6f,-4);
            camera.transform.LookAt(target.transform.position+Vector3.up*1.35f);
            equipment.Equip(2); combat.ResetTrainingState(); stamina.Restore(); target.Health.Restore();
            Assert(combat.TryAttack(false),"Bow fire");
            for(int i=0;i<105;i++){combat.TickCombat(1f/60); if(i==17) Capture(camera,"Bow"); yield return null;}
            Assert(Mathf.Abs(target.Health.Current-73)<.1f,"Bow camera-aimed hit: "+target.Health.Current);
            Pass("Bow draw/release spawns an aimed ballistic arrow that damages target.");

            equipment.Equip(3); combat.ResetTrainingState(); stamina.Restore(); target.Health.Restore();
            Assert(combat.TryAttack(false),"Crossbow fire");
            for(int i=0;i<50;i++){combat.TickCombat(1f/60); if(i==6) Capture(camera,"Crossbow"); yield return null;}
            Assert(!combat.CrossbowLoaded && combat.IsReloading && !combat.TryAttack(false),"Crossbow reload fire lockout");
            for(int i=0;i<130;i++){combat.TickCombat(1f/60); yield return null;}
            Assert(combat.CrossbowLoaded && !combat.IsReloading && Mathf.Abs(target.Health.Current-58)<.1f,"Crossbow hit/reload completion: "+target.Health.Current);
            Pass("Crossbow bolt damage, reload lockout and timed reload completion.");

            var training=UnityEngine.Object.FindAnyObjectByType<TrainingArenaControls>();
            training.ResetTraining();
            Assert(target.Health.Current==100 && health.Current==100 && stamina.Current==100,"Training reset");
            equipment.Equip(0); guard.ResetGuard(); guard.Request(true);
            motor.Teleport(new Vector3(0,.08f,-12),Quaternion.identity);
            camera.transform.position=motor.transform.position+new Vector3(3,2.5f,-4); camera.transform.LookAt(motor.transform.position+new Vector3(0,1.2f,1));
            for(int i=0;i<4;i++) yield return null;
            Capture(camera,"Overview");
            ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/CombatHUD.png");
            yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<Bellator.Classes.PlayerClassController>()?.RestoreLegacyForValidation();
            Pass("Training reset and combat visual captures.");
        }
        private static void Capture(Camera camera,string pose)
        {
            var rt=new RenderTexture(1280,800,24); var previous=RenderTexture.active; camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1280,800),0,0); texture.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/Combat"+pose+".png",texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}






