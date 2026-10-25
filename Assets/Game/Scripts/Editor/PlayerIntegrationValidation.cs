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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class PlayerIntegrationValidation
    {
        const string SessionKey="Bellator.PlayerStage2.Test";
        static IEnumerator routine; static int frame=-1; static double deadline;
        static readonly StringBuilder report=new StringBuilder(); static readonly List<string> errors=new List<string>();
        static GameManager game; static ThirdPersonMotor motor; static PlayerCombat combat; static PlayerAbility ability;
        static PlayerAnimationBridge bridge; static PlayerLockOn locked; static ThirdPersonCamera orbit;
        static CharacterHealth health; static CharacterStamina stamina; static ShieldGuard guard; static PlayerActionStateMachine state;
        static EnemyBrain enemy; static Texture2D sheet;
        static InputSettings.EditorInputBehaviorInPlayMode oldInput;
        static PlayerIntegrationValidation() { EditorApplication.update+=Update; EditorApplication.playModeStateChanged+=PlayState; }
        [MenuItem("Bellator/Player Stage 2/Test integrated player in real arena")]
        public static void Begin()
        {
            File.Delete(PlayerIntegrationTools.Root+"/Evidence/IntegrationValidation.txt");
            EditorSceneManager.OpenScene(GameInterface.MainScene); SessionState.SetBool(SessionKey,true); SessionState.SetBool(SessionKey+"Batch",Application.isBatchMode);
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.position=new Rect(20,20,1440,1000); view.Focus();
            EditorApplication.EnterPlaymode();
        }
        static void PlayState(PlayModeStateChange value)
        {
            if(value!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey,false)) return;
            if(Keyboard.current==null) InputSystem.AddDevice<Keyboard>("PlayerStage2Keyboard"); if(Mouse.current==null) InputSystem.AddDevice<Mouse>("PlayerStage2Mouse");
            InputSystem.EnableDevice(Keyboard.current); InputSystem.EnableDevice(Mouse.current);
            oldInput=InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            report.Clear(); errors.Clear(); report.AppendLine("BELLATOR - ETAPA 2 / Unity "+Application.unityVersion+" / real scenes, CharacterController, Animator, Input System");
            Application.logMessageReceived+=Log; Time.captureFramerate=60; deadline=EditorApplication.timeSinceStartup+480; routine=Flatten(Checks());
        }
        static void Log(string message,string stack,LogType type)
        {
            if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (stack.Contains("Bellator.") || message.Contains("Animator") || message.Contains("NullReference") || message.Contains("MissingReference"))) errors.Add(message);
        }
        static void Update()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && SessionState.GetBool(SessionKey+"Continue",false))
            { SessionState.SetBool(SessionKey+"Continue",false); SoulslikeStageTools.BatchRun(); return; }
            if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(SessionKey+"ExitFailure",false)) { SessionState.SetBool(SessionKey+"ExitFailure",false); EditorApplication.Exit(1); return; }
            if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(SessionKey+"ExitSuccess",false)) { SessionState.SetBool(SessionKey+"ExitSuccess",false); EditorApplication.Exit(0); return; }
            if(routine==null || !EditorApplication.isPlaying || frame==Time.frameCount) return;
            frame=Time.frameCount;
            if(game!=null && !game.IsTerminal && game.Player.GetComponent<ThirdPersonInput>().enabled) game.Player.GetComponent<ThirdPersonInput>().SendMessage("OnApplicationFocus",true);
            try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Integrated player test timed out"); if(!routine.MoveNext()) Finish(null); }
            catch(Exception e) { Finish(e); }
        }
        static void Finish(Exception e)
        {
            Application.logMessageReceived-=Log; if(e==null && errors.Count>0) e=new InvalidOperationException(string.Join("\n",errors));
            routine=null; SessionState.SetBool(SessionKey,false); Time.captureFramerate=0; Time.timeScale=1; InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;
            if(sheet!=null) UnityEngine.Object.DestroyImmediate(sheet);
            report.AppendLine(e==null?"RESULT: PASS":"RESULT: FAIL\n"+e); File.WriteAllText(PlayerIntegrationTools.Root+"/Evidence/IntegrationValidation.txt",report.ToString());
            if(e!=null) Debug.LogException(e);
            bool batch=SessionState.GetBool(SessionKey+"Batch",false);
            if(batch) SessionState.SetBool(SessionKey+(e!=null?"ExitFailure":Environment.GetCommandLineArgs().Contains("-bellatorSkipRegressions")?"ExitSuccess":"Continue"),true);
            EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator start)
        {
            var stack=new Stack<IEnumerator>(); stack.Push(start);
            while(stack.Count>0) { var item=stack.Peek(); if(!item.MoveNext()) {stack.Pop();continue;} if(item.Current is IEnumerator nested) {stack.Push(nested);continue;} yield return null; }
        }
        static void Assert(bool result,string message) { if(!result) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message); File.WriteAllText(PlayerIntegrationTools.Root+"/Evidence/IntegrationProgress.txt",report.ToString()); }
        static IEnumerator Wait(float seconds) { float until=Time.time+seconds; while(Time.time<until) yield return null; }
        static IEnumerator Scene(string name) { while(SceneManager.GetActiveScene().name!=name) yield return null; yield return null; yield return null; }
        static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        static void Cache()
        {
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(game!=null,"GameManager missing");
            var player=game.Player; motor=player.GetComponent<ThirdPersonMotor>(); combat=player.GetComponent<PlayerCombat>(); ability=player.GetComponent<PlayerAbility>();
            bridge=player.GetComponent<PlayerAnimationBridge>(); locked=player.GetComponent<PlayerLockOn>(); orbit=Camera.main.GetComponent<ThirdPersonCamera>();
            health=player.GetComponent<CharacterHealth>(); stamina=player.GetComponent<CharacterStamina>(); guard=player.GetComponent<ShieldGuard>(); state=player.GetComponent<PlayerActionStateMachine>();
            game.Waves.ManualSimulation=true; motor.AcceptInput=false; combat.ManualSimulation=true; ability.ManualSimulation=true; stamina.enabled=false; orbit.AcceptInput=false;
            player.GetComponent<ThirdPersonInput>().enabled=false; player.GetComponent<ThirdPersonInput>().enabled=true;
        }
        static void Step(Vector2 axis,bool run=false,bool dodge=false,float dt=.02f)
        {
            combat.TickCombat(dt); ability.TickAbility(dt); state.Tick(dt); motor.ProcessMovement(axis,run,dodge,dt); Physics.SyncTransforms();
            bridge.TickVisual(dt); if(bridge.Animator.speed>0) bridge.Animator.Update(dt); orbit.UpdateOrbit(Vector2.zero,0,dt);
        }
        static void Steps(int count,Vector2 axis,bool run=false) { for(int i=0;i<count;i++) Step(axis,run); }
        static void ResetPlayer()
        {
            locked.Clear(); combat.ResetTrainingState(); ability.ResetAbility(); game.Player.GetComponent<ActorPosture>().ResetPosture(); state.ResetState(); stamina.Restore();
            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); Camera.main.transform.rotation=Quaternion.identity;
            orbit.Configure(motor.transform,game.Player.GetComponent<ThirdPersonInput>()); Steps(15,Vector2.zero);
            // Resetting a class/actor also resets visual hit/death sampling.
            bridge.Bind(bridge.Animator); bridge.TickVisual(.02f); bridge.Animator.Update(.15f);
        }
        static void PlaceEnemy(Vector3 position)
        {
            enemy.AutoThink=false; enemy.GetComponent<CharacterHealth>().Restore(); enemy.ResetActor(); enemy.GetComponent<EnemyNavigation>().Warp(position,Quaternion.Euler(0,180,0));
            enemy.GetComponent<EnemyNavigation>().Stop(); enemy.GetComponent<ShieldGuard>().Request(false); enemy.SetTarget(game.Player.GetComponent<DamageReceiver>()); Physics.SyncTransforms();
        }
        static void Visual(string expected)
        {
            Assert(bridge.VisualState==expected,"Expected visual "+expected+", got "+bridge.VisualState);
            Assert(bridge.Animator.GetCurrentAnimatorStateInfo(0).IsName(expected),"Animator state does not match gameplay: "+expected);
        }
        static IEnumerator Inputs(Key[] keys,ushort buttons,float seconds,Vector2 mouseDelta=default)
        {
            var keyboard=Keyboard.current; var mouse=Mouse.current; bool down=true;
            Action inject=()=> { if(InputState.currentUpdateType==InputUpdateType.Dynamic) {InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(keys):new KeyboardState()); InputSystem.QueueStateEvent(mouse,new MouseState{buttons=down?buttons:(ushort)0,delta=down?mouseDelta:Vector2.zero});} };
            InputSystem.onBeforeUpdate+=inject;
            try { yield return Wait(seconds); down=false; yield return null; yield return null; }
            finally { InputSystem.onBeforeUpdate-=inject; }
        }
        static IEnumerator Checks()
        {
            yield return null; yield return null; Assert(PlayerSession.EnterGuest(),"Guest session failed");
            Click("PlayButton"); Click("StoryModeButton"); yield return Scene("RomanArena"); Cache();
            Assert(!game.IsInfinite && game.Waves.WaveCount==5,"Story mode altered");
            var definitions=game.Player.Catalog.classes;
            for(int c=0;c<4;c++)
            {
                if(c>0) { var reload=game.Restart(); while(!reload.isDone) yield return null; yield return Scene("RomanArena"); Cache(); }
                var definition=definitions[c]; game.Player.Apply(definition); yield return null; yield return null; Cache(); ResetPlayer();
                Assert(bridge.DrivesAnimator && bridge.Animator.transform.IsChildOf(motor.transform) && !bridge.Animator.applyRootMotion,"Animator not connected on actual player");
                Assert(motor.GetComponents<ThirdPersonMotor>().Length==1 && motor.GetComponents<CharacterController>().Length==1 && motor.GetComponent<Rigidbody>()==null,"Duplicate movement/physics");
                Assert(game.Player.GetComponentsInChildren<PrimitiveHumanoidAnimator>().Length==0,"Procedural locomotion still on active rig");
                Assert(game.Player.GetComponent<WeaponEquipment>().IsFixed && game.Player.GetComponent<WeaponEquipment>().Current==definition.equipment[0],"Class weapon replaced");
                Assert(bridge.Animator.GetComponentsInChildren<Animator>().Length==1,"Duplicate Animators");
                sheet=new Texture2D(1280,720,TextureFormat.RGB24,false);
                Steps(20,Vector2.zero); Assert(bridge.Animator.GetFloat("Speed")<.03f,"Idle speed"); Capture(0,"Idle");
                Vector3 start=motor.transform.position; Steps(35,Vector2.up); Assert(motor.transform.position.z-start.z>1,"Walk did not translate root"); Visual("FreeLocomotion"); Assert(bridge.Animator.GetFloat("Speed")>.8f,"Walk parameter"); Capture(1,"Walk");
                float straight=Vector3.Distance(start,motor.transform.position); ResetPlayer(); start=motor.transform.position; Steps(35,Vector2.one);
                Assert(Mathf.Abs(Vector3.Distance(start,motor.transform.position)-straight)<.12f,"Diagonal speed bonus");
                ResetPlayer(); Camera.main.transform.rotation=Quaternion.Euler(0,90,0); start=motor.transform.position;
                for(int i=0;i<35;i++) {motor.ProcessMovement(Vector2.up,false,false,.02f);Physics.SyncTransforms();} Assert(motor.transform.position.x-start.x>1,"Camera-relative movement failed");
                ResetPlayer(); Steps(35,Vector2.up,true); Assert(motor.IsRunning && stamina.Current<definition.stamina,"Sprint/stamina"); bridge.Animator.Update(.2f); Visual("FreeRunning"); Capture(2,"Run");
                Steps(30,Vector2.zero); Assert(motor.PlanarSpeed<.05f && bridge.Animator.GetFloat("Speed")<.05f,"Slide/Walk after stopping");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Gladiator.prefab"); enemy=UnityEngine.Object.Instantiate(prefab).GetComponent<EnemyBrain>(); yield return null;
                if(Environment.GetCommandLineArgs().Contains("-bellatorAllRolls")) yield return RollChecks(definition);
                ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,6)); Assert(locked.Toggle(),"Existing target acquisition failed"); Steps(25,Vector2.left); bridge.Animator.Update(.2f); Visual("LockedLocomotion");
                Assert(bridge.Animator.GetBool("IsLockedOn") && bridge.Animator.GetFloat("MoveX")<-.5f,"Strafe parameter");
                Assert(Vector3.Dot(motor.transform.forward,Vector3.ProjectOnPlane(locked.TargetPoint-motor.transform.position,Vector3.up).normalized)>.97f,"Strafe lost target facing"); Capture(3,"Locked strafe");
                Steps(25,Vector2.down); Assert(bridge.Animator.GetFloat("MoveY")<-.5f,"Backward parameter"); Capture(4,"Locked backward");
                ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,6)); Assert(locked.Toggle(),"Reacquire");
                start=motor.transform.position; float before=stamina.Current; Step(Vector2.right,false,true);
                Assert(motor.IsDodging && before-stamina.Current>=definition.dodge.cost-.01f,"Dodge start/cost");
                string dodge=definition.dodge.style==DodgeStyle.Roll?"DodgeRoll":"Quickstep"; Visual(dodge);
                int dodgeSequence=motor.DodgeSequence; bool invulnerable=false,open=false,dodgeCaptured=false; float maxTurn=0;
                for(int i=0;i<Mathf.CeilToInt((definition.dodge.duration+definition.dodge.recovery)/.02f)+3;i++)
                {
                    Quaternion camera=Camera.main.transform.rotation; bool inv=motor.IsInvulnerable; invulnerable|=inv; open|=!inv;
                    if(inv) { float hp=health.Current; Assert(!game.Player.GetComponent<DamageReceiver>().Receive(new DamageInfo(1,motor.transform.position,enemy.transform.position,enemy.transform)),"Invulnerability rejected late"); Assert(health.Current==hp,"Invulnerability health changed"); }
                    Step(Vector2.zero,true,motor.IsDodging); maxTurn=Mathf.Max(maxTurn,Quaternion.Angle(camera,Camera.main.transform.rotation));
                    if(!dodgeCaptured && motor.IsDodging && motor.DodgeAnimationProgress>=.42f)
                    {
                        if(definition.dodge.style==DodgeStyle.Roll) Assert(Quaternion.Angle(Quaternion.identity,bridge.Animator.transform.Find("MotionPivot").localRotation)>70,"Roll clip did not rotate visual body");
                        Capture(5,dodge); dodgeCaptured=true;
                    }
                }
                Assert(motor.DodgeSequence==dodgeSequence,"Repeated dodge started during same dodge");
                Assert(!motor.IsDodging && !motor.IsRunning && invulnerable && open,"Dodge did not recover / invalid window");
                Assert(Vector3.Distance(start,motor.transform.position)>definition.dodge.distance*.7f && locked.IsLocked,"Dodge movement or lock persistence failed"); Assert(maxTurn<9,"Abrupt locked camera during dodge");
                ResetPlayer(); stamina.TryConsume(stamina.Current); Step(Vector2.up,false,true); Assert(!motor.IsDodging,"No-stamina dodge accepted"); stamina.Restore();
                bool ranged=definition.equipment[0].IsRanged;
                foreach(bool heavy in new[]{false,true})
                {
                    ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,1.3f));
                    if(ranged && heavy) { Assert(!combat.TryAttack(true),"Hunter sword/heavy attack introduced"); Capture(7,"Hunter ranged only"); continue; }
                    int hits=combat.ConfirmedHits; float hp=enemy.GetComponent<CharacterHealth>().Current;
                    Assert(combat.TryAttack(heavy),"Attack refused"); int sequence=combat.AttackSequence;
                    Step(Vector2.zero); Visual(ranged?"Fire":heavy?"HeavyAttack":"LightAttack");
                    Assert(enemy.GetComponent<CharacterHealth>().Current==hp,"Damage before active window");
                    bool sampled=false;
                    for(int i=0;i<180 && combat.IsAttacking;i++)
                    {
                        Step(Vector2.zero);
                        if(combat.Phase==AttackPhase.Active)
                        {
                            Assert(Mathf.Abs(bridge.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime-combat.AnimationAttackProgress)<.04f,"Visual/contact phase desynchronized");
                            if(!sampled && combat.AnimationAttackProgress >= (ranged?.4f:heavy?.49f:.33f)) { Capture(heavy?7:6,heavy?"Heavy active":ranged?"Crossbow fire":"Light active"); sampled=true; }
                        }
                    }
                    Assert(!combat.IsAttacking && sampled && combat.AttackSequence==sequence,"Attack stuck or active not sampled");
                    if(!ranged) Assert(combat.ConfirmedHits==hits+1 && enemy.GetComponent<CharacterHealth>().Current<hp,"Melee active window/hit count");
                    else { Assert(combat.IsReloading,"Hunter reload missing"); Steps(40,Vector2.zero); Visual("Reload"); Capture(10,"Reload"); }
                    Steps(160,Vector2.zero); Steps(25,Vector2.right); Assert(motor.PlanarSpeed>.5f && bridge.VisualState=="FreeLocomotion","Movement stuck after attack: speed="+motor.PlanarSpeed+", visual="+bridge.VisualState);
                }
                ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,6)); guard.Request(true); Step(Vector2.zero);
                if(definition.shield!=null) { Assert(guard.IsBlocking,"Shield block missing"); Steps(15,Vector2.zero); Visual("Block"); Capture(8,"Block held"); }
                else { Assert(!guard.IsBlocking,"Shield added to class"); Capture(8,"No shield"); }
                guard.Request(false); Steps(10,Vector2.zero); Assert(!guard.IsBlocking && bridge.VisualState!="Block","Block stuck after release");
                ResetPlayer(); Assert(game.Player.GetComponent<DamageReceiver>().Receive(new DamageInfo(3,motor.transform.position+Vector3.up,enemy.transform.position,enemy.transform,0)),"Hit rejected");
                Steps(5,Vector2.zero); Visual("HitReaction"); Capture(9,"Hit reaction"); Steps(40,Vector2.zero);
                ResetPlayer(); Assert(ability.TryActivate(),"Q ability broken: "+definition.displayName); Step(Vector2.zero);
                if(!ranged) { if(ability.IsCasting) Steps(Mathf.CeilToInt(ability.Definition.windup/.02f*.7f),Vector2.zero); Capture(10,"Q ability"); }
                Steps(140,Vector2.zero); Assert(ability.CooldownRemaining>0,"Ability cooldown missing");
                Pass(definition.displayName+": actual rig/controller/equipment, idle/walk/run/diagonal/camera-relative movement, lock strafe/backward/facing, translated dodge + stamina/i-frames/recovery, active-window attacks, block release, hit, original Q ability.");
                if(c==2) yield return RealInputChecks();
                ResetPlayer(); health.Damage(10000); Step(Vector2.zero); Visual("Death");
                Assert(!combat.TryAttack(false) && !ability.TryActivate(),"Dead commands accepted"); start=motor.transform.position; Steps(90,Vector2.up,true);
                Assert(Vector3.Distance(start,motor.transform.position)<.1f && bridge.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime>.98f,"Death root moved / final pose not held"); Capture(11,"Death final");
                File.WriteAllBytes(PlayerIntegrationTools.Root+"/Evidence/"+definition.id+"Arena.png",sheet.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(sheet); sheet=null;
                Pass(definition.displayName+": death final pose, no commands or animation root drift; original terminal flow preserved.");
            }
            game.ReturnToMenu(); yield return Scene("MainMenu"); Click("PlayButton"); Click("InfiniteModeButton"); yield return Scene("RomanArena"); Cache();
            Assert(game.IsInfinite && game.Player.GetComponent<PlayerAnimationBridge>().DrivesAnimator && !game.CanRecordResult,"Infinite mode / guest recording semantics changed");
            ResetPlayer(); Steps(25,Vector2.up); Assert(motor.PlanarSpeed>.5f,"Integrated player failed in Infinite mode");
            if(Environment.GetCommandLineArgs().Contains("-bellatorAllRolls"))
            {
                foreach(var definition in game.Player.Catalog.classes)
                {
                    game.Player.Apply(definition); yield return null; Cache(); ResetPlayer();
                    Step(Vector2.left,false,true); Visual("DodgeRoll"); Assert(motor.IsDodging,"Infinite roll failed");
                    Steps(80,Vector2.zero); Assert(!motor.IsDodging,"Infinite recovery failed");
                    if(definition.hasQuickstep) { motor.ProcessMovement(Vector2.right,false,false,.02f,true); bridge.TickVisual(.02f); Visual("Quickstep"); Steps(80,Vector2.zero); }
                }
                Pass("Infinite: all four primary rolls and both secondary Quicksteps execute and recover on actual scene player.");
            }
            Pass("Main menu -> Story and Infinite actual arena loads; all four class restarts retained; guest remains excluded from ranking. Full five-wave/exit, unlimited-wave/difficulty/ranking and account regressions follow in existing suites.");
        }
        static IEnumerator RollChecks(GladiatorClassDefinition definition)
        {
            Assert(definition.dodge.style==DodgeStyle.Roll,"Primary dodge must be Roll");
            var directions=new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right,new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1)};
            foreach(bool useLock in new[]{false,true}) foreach(var axis in directions)
            {
                ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,8));
                if(useLock) Assert(locked.Toggle(),"Roll lock acquisition");
                // Also exercise a rotated camera when unlocked.
                if(!useLock) Camera.main.transform.rotation=Quaternion.Euler(0,37,0);
                Vector3 f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;
                Vector3 expected=(f*axis.y+Vector3.Cross(Vector3.up,f)*axis.x).normalized;
                Vector3 start=motor.transform.position; float energy=stamina.Current;
                Step(axis,false,true); Visual("DodgeRoll");
                Assert(motor.DodgeStyle==DodgeStyle.Roll && Mathf.Abs(energy-stamina.Current-definition.dodge.cost)<.01f,"Roll style/cost");
                Assert(Vector3.Dot(motor.DodgeDirection,expected)>.999f,"Wrong camera-relative roll direction");
                Assert(!combat.TryAttack(false) && !ability.TryActivate(),"Attack/Q accepted during roll");
                int sequence=motor.DodgeSequence; bool posed=false; float maxTurn=0;
                while(motor.IsDodging)
                {
                    var rotation=Camera.main.transform.rotation; float progress=motor.DodgeAnimationProgress;
                    // Opposite command must not replace an active roll, including recovery.
                    motor.ProcessMovement(Vector2.zero,false,false,.02f,true); Physics.SyncTransforms(); bridge.TickVisual(.02f); orbit.UpdateOrbit(Vector2.zero,0,.02f);
                    maxTurn=Mathf.Max(maxTurn,Quaternion.Angle(rotation,Camera.main.transform.rotation));
                    if(motor.IsDodging) Assert(motor.DodgeStyle==DodgeStyle.Roll && motor.DodgeSequence==sequence,"Quickstep interrupted roll");
                    if(progress>=.4f && progress<.6f)
                    {
                        posed=true; Assert(Quaternion.Angle(Quaternion.identity,bridge.Animator.transform.Find("MotionPivot").localRotation)>70,"Native roll clip missing rotation");
                        Assert(Mathf.Abs(bridge.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime-motor.DodgeAnimationProgress)<.015f,"Roll timeline out of sync");
                    }
                }
                Vector3 travelled=Vector3.ProjectOnPlane(motor.transform.position-start,Vector3.up);
                Assert(posed && Mathf.Abs(travelled.magnitude-definition.dodge.distance)<.15f && Vector3.Dot(travelled.normalized,expected)>.99f,"Roll distance/direction/pose");
                if(useLock) Assert(locked.IsLocked && maxTurn<9,"Roll lost target / abrupt camera");
                Steps(20,Vector2.right); Assert(motor.PlanarSpeed>.5f,"Movement stuck after roll");
                Assert(combat.TryAttack(false),"Attack unavailable after roll"); combat.ResetTrainingState(); stamina.Restore();
                Assert(ability.TryActivate(),"Q unavailable after roll"); ability.ResetAbility();
            }
            ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,8));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="RollValidationWall"; wall.transform.position=new Vector3(0,2,1.5f); wall.transform.localScale=new Vector3(10,4,.2f); Physics.SyncTransforms();
            Step(Vector2.up,false,true); Steps(80,Vector2.zero); Assert(motor.transform.position.z<1.4f,"Roll crossed wall");
            Assert(motor.GetComponent<CharacterController>().enabled,"Roll disabled collider"); UnityEngine.Object.DestroyImmediate(wall);
            ResetPlayer(); int prior=motor.DodgeSequence;
            motor.ProcessMovement(Vector2.left,false,false,.02f,true); bridge.TickVisual(.02f);
            if(definition.hasQuickstep)
            {
                Assert(motor.IsDodging && motor.DodgeStyle==DodgeStyle.Quickstep,"Secondary Quickstep missing"); Visual("Quickstep");
                Assert(Mathf.Abs(stamina.Current-(definition.stamina-definition.quickstep.cost))<.01f,"Quickstep original cost changed");
                while(motor.IsDodging) Step(Vector2.zero,false,true);
                Assert(motor.DodgeSequence==prior+1,"Roll interrupted Quickstep");
                ResetPlayer(); stamina.TryConsume(stamina.Current); motor.ProcessMovement(Vector2.up,false,false,.02f,true); Assert(!motor.IsDodging,"Quickstep accepted without stamina");
                ResetPlayer(); motor.AcceptInput=true;
                yield return Inputs(new[]{Key.A,Key.LeftAlt},0,.04f); Assert(motor.IsDodging && motor.DodgeStyle==DodgeStyle.Quickstep,"Real Alt Quickstep failed"); yield return Wait(.8f);
                stamina.Restore(); yield return Inputs(new[]{Key.D,Key.Space},0,.04f); Assert(motor.IsDodging && motor.DodgeStyle==DodgeStyle.Roll,"Real Space roll failed"); yield return Wait(1);
                var input=motor.GetComponent<ThirdPersonInput>(); input.RebindQuickstep("<Keyboard>/g"); stamina.Restore();
                yield return Inputs(new[]{Key.G},0,.04f); Assert(motor.IsDodging && motor.DodgeStyle==DodgeStyle.Quickstep,"Quickstep rebind failed"); yield return Wait(.8f); input.RebindQuickstep("<Keyboard>/leftAlt");
                motor.AcceptInput=false; ResetPlayer();
                motor.ProcessMovement(Vector2.up,false,true,.02f,true); Assert(motor.DodgeStyle==DodgeStyle.Roll,"Space priority failed"); Steps(80,Vector2.zero);
            }
            else Assert(!motor.IsDodging && motor.DodgeSequence==prior,"Quickstep added to heavy/balanced class");
            Pass(definition.displayName+": 8 directions x locked/unlocked, rotated camera, exact stamina/distance, native roll pose/timing, recovery/attack/Q, wall/collider, secondary Quickstep gating and real Space/Alt where enabled.");
            ResetPlayer();
        }
        static IEnumerator RealInputChecks()
        {
            ResetPlayer(); PlaceEnemy(new Vector3(0,.04f,6));
            motor.AcceptInput=true; combat.ManualSimulation=false; ability.ManualSimulation=false; orbit.AcceptInput=true;
            Vector3 start=motor.transform.position; yield return Inputs(new[]{Key.W},0,.4f); Assert(Vector3.Distance(start,motor.transform.position)>.4f,"Real W input did not move player");
            yield return Inputs(new[]{Key.W,Key.LeftShift},0,.3f); Assert(motor.PlanarSpeed>motor.WalkSpeed,"Real Shift sprint failed");
            yield return Inputs(Array.Empty<Key>(),0,.3f); yield return Inputs(new[]{Key.Tab},0,.02f); Assert(locked.IsLocked,"Real Tab lock failed");
            int sequence=motor.DodgeSequence; yield return Inputs(new[]{Key.A,Key.Space},0,.04f); Assert(motor.DodgeSequence==sequence+1 && locked.IsLocked,"Real Space/locked dodge failed"); yield return Wait(1.3f);
            int attack=combat.AttackSequence; yield return Inputs(Array.Empty<Key>(),1,.04f); Assert(combat.AttackSequence>attack,"Real left click attack failed"); yield return Wait(1.5f);
            stamina.Restore(); yield return Inputs(new[]{Key.E},0,.04f); Assert(combat.IsAttacking && combat.IsHeavy,"Real E heavy failed"); yield return Wait(1.8f);
            yield return Inputs(Array.Empty<Key>(),2,.1f); Assert(!guard.IsBlocking,"Right mouse release stuck");
            ability.ResetAbility(); stamina.Restore(); yield return Inputs(new[]{Key.Q},0,.04f); Assert(ability.CooldownRemaining>0,"Real Q ability failed");
            locked.Clear(); Quaternion rotation=Camera.main.transform.rotation; yield return Inputs(Array.Empty<Key>(),0,.1f,new Vector2(20,0)); Assert(Quaternion.Angle(rotation,Camera.main.transform.rotation)>1,"Real mouse look failed");
            motor.AcceptInput=false; combat.ManualSimulation=true; ability.ManualSimulation=true; orbit.AcceptInput=false; ResetPlayer();
            Pass("Real Input System frames drove W, Shift, Tab, A+Space, left click, E, right-button release, Q and mouse orbit in the real arena.");
        }
        static void Capture(int index,string label)
        {
            var camera=Camera.main; var target=new RenderTexture(320,240,24); var texture=new Texture2D(320,240,TextureFormat.RGB24,false);
            var prior=RenderTexture.active; var previous=camera.targetTexture;
            try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,320,240),0,0); texture.Apply(); sheet.SetPixels(index%4*320,(2-index/4)*240,320,240,texture.GetPixels()); sheet.Apply(); }
            finally { camera.targetTexture=previous; RenderTexture.active=prior; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
            report.AppendLine("  Arena capture ["+index+"]: "+label+"; root="+motor.transform.position+"; visual="+bridge.VisualState);
            File.WriteAllBytes(PlayerIntegrationTools.Root+"/Evidence/Diagnostics.png",sheet.EncodeToPNG());
        }
    }
}

