using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Classes;
using Bellator.Match;
using Bellator.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class ClassStageValidation
    {
        const string Active="Bellator.Stage5.Test";
        static IEnumerator routine; static int frame=-1;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static ClassStageValidation() { EditorApplication.update+=Check; EditorApplication.playModeStateChanged+=State; }
        static void Check()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            { frame=Time.frameCount; try { if(!routine.MoveNext()) Finish(null); } catch(Exception e) { Finish(e); } }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorClasses.test")) return;
            File.Delete("Temp/BellatorClasses.test"); Run();
        }
        [MenuItem("Bellator/Test Stage 5 Classes in Play Mode")]
        public static void Run()
        { if(EditorApplication.isPlayingOrWillChangePlaymode) return; EditorSceneManager.OpenScene(PlayerClassController.SelectionScene); SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode(); }
        static void State(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear(); errors.Clear(); report.AppendLine("Stage 5 — Unity "+Application.unityVersion+" — real Play Mode and scene transitions");
            Application.logMessageReceived+=Log; routine=Checks(); frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("NavMeshAgent"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log; if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null; SessionState.SetBool(Active,false); report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/Stage5Validation.txt",report.ToString()); File.WriteAllText("Temp/BellatorClasses.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error); else Debug.Log("Bellator Stage 5 Play Mode validation passed."); EditorApplication.ExitPlaymode();
        }
        static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Near(float value,float expected,string message) { Assert(Mathf.Abs(value-expected)<.15f,message+": "+value+" expected "+expected); }
        static void Pass(string message) { report.AppendLine("PASS: "+message); File.WriteAllText("Temp/BellatorClasses.progress.txt",report.ToString()); }
        static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1280,800,24); var old=camera.targetTexture; var oldActive=RenderTexture.active;
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; var image=new Texture2D(1280,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/Stage5_"+name+".png",image.EncodeToPNG()); camera.targetTexture=old; RenderTexture.active=oldActive; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(image);
        }
        static IEnumerator Checks()
        {
            yield return null; yield return null;
            for(int i=0;i<4;i++)
            {
                var selection=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
                Assert(selection!=null && selection.Catalog.classes.Length==4,"Selection/catalog missing");
                Assert(GameObject.Find("ClassPreviews").transform.childCount==12,"Four primitive previews/pedestals/halos");
                Assert(GameObject.Find("StartButton")==null,"Selection must not offer direct start");
                var previewRoot=GameObject.Find("ClassPreviews").transform;
                foreach(var c in selection.Catalog.classes)
                {
                    if(c.shield!=null) Assert(previewRoot.Find("Preview_"+c.id).GetComponentsInChildren<Transform>().Any(t=>t.name=="SelectionShield"),"Selection shield missing: "+c.id);
                }
                Button("ClassCard_"+i).onClick.Invoke(); Assert(selection.SelectedIndex==i,"Class card button failed");
                if(i==0)
                {
                    selection.Select(1); selection.Select(0);
                    Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Stage5_Selection.png"); yield return null; yield return null;
                }
                var definition=selection.Catalog.classes[i]; Button("ConfirmButton").onClick.Invoke();
                while(SceneManager.GetActiveScene().name!="MainMenu") yield return null;yield return null;yield return null;
                Assert(ClassSelectionState.Selected==definition,"Confirmation failed");
                if(!PlayerSession.HasSession) { var selected=ClassSelectionState.Selected; PlayerSession.EnterGuest();ClassSelectionState.Confirm(selected); }
                Button("PlayButton").onClick.Invoke();Button("StoryModeButton").onClick.Invoke();
                float timeout=Time.realtimeSinceStartup+20;
                while(SceneManager.GetActiveScene().name!="RomanArena" && Time.realtimeSinceStartup<timeout) yield return null;
                yield return null; yield return null;
                MatchStageBuilder.PrepareLegacySandbox();
                yield return null; yield return null; // Let re-enabled legacy actors finish Start before testing impulses.
                var classes=UnityEngine.Object.FindAnyObjectByType<PlayerClassController>(); Assert(classes!=null && classes.Current==definition,"Selection was lost crossing scenes");
                var player=classes.GetComponent<PlayerCombat>(); var motor=classes.GetComponent<ThirdPersonMotor>(); var health=classes.GetComponent<CharacterHealth>(); var stamina=classes.GetComponent<CharacterStamina>();
                var equipment=classes.GetComponent<WeaponEquipment>(); var ability=classes.GetComponent<PlayerAbility>(); var stats=classes.GetComponent<ActorAttributes>(); var guard=classes.GetComponent<ShieldGuard>(); var receiver=classes.GetComponent<DamageReceiver>();
                Near(health.Maximum,definition.health,"Class HP"); Near(stamina.Maximum,definition.stamina,"Class stamina"); Near(stats.BaseDefense,definition.defense,"Class defense");
                Assert(equipment.Loadout.Length==definition.equipment.Length && equipment.Current==definition.equipment[0] && equipment.HasShield==(i==1 || i==2),"Class equipment/shield mismatch");
                var visual=classes.transform.Find("ClassVisual_"+definition.id); Assert(visual!=null && !classes.transform.Find("VisualRoot_Replaceable").gameObject.activeSelf,"Replaceable visual/logic separation");
                Near(classes.GetComponent<CharacterController>().height,definition.colliderHeight,"Class collider");
                var enemies=UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None); Assert(enemies.Length==3,"Initial enemy encounter changed");
                foreach(var e in enemies) { e.AutoThink=false; e.GetComponent<EnemyNavigation>().Stop(); e.gameObject.SetActive(false); }
                player.ManualSimulation=true; motor.AcceptInput=false; ability.ManualSimulation=true; guard.enabled=false;
                Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=false; Camera.main.GetComponent<ThirdPersonCamera>().enabled=false;
                var camera=Camera.main; camera.transform.position=new Vector3(0,1.4f,-6); camera.transform.rotation=Quaternion.identity;
                motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); for(int n=0;n<8;n++) motor.ProcessMovement(Vector2.zero,false,false,1f/60);
                player.GetComponent<ThirdPersonInput>().enabled=false;
                if(i==0)
                {
                    var priorInputFocus=InputSystem.settings.editorInputBehaviorInPlayMode;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    var keyboard=InputSystem.AddDevice<Keyboard>("ClassValidationKeyboard"); InputSystem.EnableDevice(keyboard);
                    try { InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q)); InputSystem.Update(); var actions=InputSystem.ListEnabledActions().Where(a=>a.actionMap?.name=="BellatorCombat").ToArray(); Assert(actions.First(a=>a.name=="Ability").IsPressed() && !actions.First(a=>a.name=="Heavy").IsPressed(),"Q input: ability="+actions.First(a=>a.name=="Ability").IsPressed()+", heavy="+actions.First(a=>a.name=="Heavy").IsPressed()+", device="+keyboard.enabled+", binding="+actions.First(a=>a.name=="Heavy").bindings[0].effectivePath); InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E)); InputSystem.Update(); Assert(actions.First(a=>a.name=="Heavy").IsPressed(),"E heavy binding"); }
                    finally { InputSystem.RemoveDevice(keyboard); InputSystem.settings.editorInputBehaviorInPlayMode=priorInputFocus; }
                    Pass("Q ability and E configurable heavy attack are independent Input System actions.");
                }
                player.GetComponent<CombatInput>().enabled=false;
                float walkDistance=0; Vector3 start=motor.transform.position;
                for(int n=0;n<90;n++) motor.ProcessMovement(Vector2.up,false,false,1f/60);
                walkDistance=Vector3.Distance(start,motor.transform.position); Assert(walkDistance>definition.walkSpeed*1.1f && walkDistance<definition.walkSpeed*1.6f,"Class speed not applied: "+walkDistance);
                motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); for(int n=0;n<8;n++) motor.ProcessMovement(Vector2.zero,false,false,1f/60);
                health.Restore(); stamina.Restore(); player.ResetTrainingState(); ability.ResetAbility();
                Assert(ability.TryActivate(),"Ability refused: "+ability.Status); Near(stamina.Current,definition.stamina-definition.ability.staminaCost,"Ability stamina cost"); Assert(!ability.TryActivate(),"Ability repeated while active/cooldown");
                if(i==0)
                {
                    Assert(motor.SpeedBuff>1 && motor.DodgeCostMultiplier<1,"Desperate steps modifiers missing");
                    float before=stamina.Current; start=motor.transform.position; motor.ProcessMovement(Vector2.up,false,true,1f/60);
                    for(int n=0;n<25;n++) motor.ProcessMovement(Vector2.zero,false,false,1f/60);
                    Near(before-stamina.Current,14,"Enhanced dodge cost"); Near(Vector3.Distance(start,motor.transform.position),4.86f,"Enhanced dodge distance"); Assert(motor.CooldownRemaining<=.41f,"Enhanced dodge cooldown");
                    ability.TickAbility(5.1f); Assert(!ability.IsActive && motor.SpeedBuff==1 && motor.DodgeCostMultiplier==1,"Survivor buff failed to expire");
                    Pass("Survivor: 70/120, fast movement, reduced-cost longer dodge, timed expiry and cooldown.");
                }
                else if(i==1)
                {
                    var heavy=enemies.First(e=>e.Definition.role==EnemyRole.Heavy); var common=enemies.First(e=>e.Definition.role==EnemyRole.Gladiator);
                    heavy.gameObject.SetActive(true); common.gameObject.SetActive(true); heavy.AutoThink=common.AutoThink=false; heavy.ResetActor(); common.ResetActor();
                    heavy.GetComponent<EnemyNavigation>().Warp(new Vector3(2,.04f,0),Quaternion.identity); common.GetComponent<EnemyNavigation>().Warp(new Vector3(-2,.04f,0),Quaternion.identity); Physics.SyncTransforms();
                    start=heavy.transform.position; float hp=heavy.GetComponent<CharacterHealth>().Current;
                    ability.TickAbility(.61f); Near(heavy.GetComponent<CharacterHealth>().Current,hp-72,"Devastating area damage"); Assert(ability.AreaHits>=2,"Area missed multiple enemies");
                    float after=heavy.GetComponent<CharacterHealth>().Current; ability.TickAbility(.01f); Near(heavy.GetComponent<CharacterHealth>().Current,after,"Area repeated damage");
                    float until=Time.time+.5f; while(Time.time<until) yield return null;
                    Assert(Vector3.Distance(start,heavy.transform.position)>.7f,"Enemy knockback absent"); ability.TickAbility(.6f);
                    heavy.gameObject.SetActive(false); common.gameObject.SetActive(false);
                    Pass("Colossus: 180/80, heavy sword plus shield, 72-damage area hits once per target and physically pushes enemies.");
                }
                else if(i==2)
                {
                    Near(stats.AbilityDamage,1.35f,"Stance damage"); Near(stats.IncomingMultiplier,.67f,"Stance defense"); Near(stats.BlockCostMultiplier,.6f,"Stance block efficiency");
                    receiver.Receive(new DamageInfo(20,motor.transform.position+Vector3.up,Vector3.forward*2,null)); Near(health.Current,120-13.4f,"Stance incoming damage");
                    stamina.Restore(); guard.Request(true); Assert(guard.IsBlocking,"Warrior shield unavailable"); receiver.Receive(new DamageInfo(20,motor.transform.position+Vector3.up,Vector3.forward*2,null)); Assert(receiver.LastImpact.Blocked,"Stance block not applied"); Near(stamina.Current,100-(6+13.4f*.65f)*.6f,"Stance guard stamina cost"); guard.Request(false);
                    var dummy=GameObject.Find("TrainingDummy_1").GetComponent<DamageReceiver>(); motor.Teleport(dummy.transform.position-Vector3.forward*1.3f,Quaternion.identity); Physics.SyncTransforms(); dummy.Health.Restore(); stamina.Restore();
                    Assert(player.TryAttack(false),"Stance sword attack failed"); for(int n=0;n<60;n++) player.TickCombat(1f/60); Near(dummy.Health.Current,100-32.4f,"Stance outgoing sword damage");
                    ability.TickAbility(6.1f); Near(stats.AbilityDamage,1,"Stance damage restore"); Near(stats.IncomingMultiplier,.82f,"Base defense preserved after stance");
                    Pass("Warrior: 120/100, sword/shield, increased real melee damage, reduced incoming damage and block costs; expiry restores base stats.");
                }
                else
                {
                    ability.ResetAbility(); player.ResetTrainingState(); stamina.Restore();Assert(!equipment.Equip(1) && equipment.Current.type==EquipmentType.Crossbow,"Hunter accepted another class weapon");
                    equipment.Equip(0); player.ResetTrainingState(); stamina.Restore();
                    var dummy=GameObject.Find("TrainingDummy_1").GetComponent<DamageReceiver>(); dummy.Health.ConfigureMaximum(300);
                    motor.Teleport(dummy.transform.position-Vector3.forward*9,Quaternion.identity); camera.transform.position=motor.transform.position+new Vector3(0,1.4f,-3); camera.transform.LookAt(dummy.transform.position+Vector3.up*1.4f); Physics.SyncTransforms(); yield return null;
                    Assert(ability.TryActivate(),"Loaded crossbow precise shot rejected"); ability.TickAbility(.41f); Assert(!player.CrossbowLoaded,"Precise shot did not discharge crossbow");
                    float until=Time.time+2; while(dummy.Health.Current>=299 && Time.time<until) yield return null;
                    Near(dummy.Health.Current,300-42*1.45f*3,"Actual precise projectile damage"); ability.TickAbility(.3f); Assert(player.IsReloading,"Precise shot did not require reload"); Assert(!ability.TryActivate(),"Precise shot bypassed cooldown/reload");
                    player.TickCombat(3); Assert(player.CrossbowLoaded,"Crossbow reload failed"); Assert(!equipment.Equip(1) && !equipment.HasShield,"Hunter fixed crossbow/shield");
                    Pass("Hunter: 80/110, fixed crossbow, 182.7-damage projectile, loaded-weapon restriction and required reload.");
                }
                Assert(ability.CooldownRemaining>0 && !ability.TryActivate(),"Cooldown bypass after ability");
                ability.TickAbility(40); Assert(ability.CooldownRemaining==0 && !ability.IsActive,"Cooldown/duration failed to complete");
                stamina.TryConsume(stamina.Current); Assert(!ability.TryActivate() && ability.CooldownRemaining==0,"Ability allowed without stamina");
                ability.ResetAbility(); health.Restore(); stamina.Restore(); player.ResetTrainingState(); guard.Request(false);
                if(i==3) equipment.Equip(0);
                Assert(ability.TryActivate(),"Death cancellation setup"); health.Damage(1000); Assert(!ability.IsActive && motor.SpeedBuff==1 && stats.AbilityDamage==1 && !player.ExternalActionBusy,"Death did not cancel ability");
                health.Restore(); ability.ResetAbility(); stamina.Restore(); player.ResetTrainingState();
                motor.Teleport(new Vector3(0,.08f,0),Quaternion.Euler(0,145,0)); camera.transform.position=new Vector3(3.3f,2.3f,-4.3f); camera.transform.LookAt(motor.transform.position+Vector3.up*1.1f); yield return null;
                Capture(camera,definition.id.ToString());
                Pass(definition.displayName+": correct selection-to-arena data, primitive proportions, collider, movement speed, cooldown restriction and death cancellation.");
                classes.ReturnToSelection(); timeout=Time.realtimeSinceStartup+20;
                while(SceneManager.GetActiveScene().name!="GladiatorSelection" && Time.realtimeSinceStartup<timeout) yield return null;
                yield return null; yield return null;
            }
            Pass("All four confirmation/start buttons and return-to-selection scene transitions succeeded. Three-enemy encounter preserved; no waves added.");
        }
    }
}




