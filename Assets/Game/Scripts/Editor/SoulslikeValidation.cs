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
    public static class SoulslikeValidation
    {
        const string Active = "Bellator.Soulslike.Test";
        static IEnumerator routine;
        static int frame;
        static double deadline;
        static readonly StringBuilder report = new StringBuilder();
        static readonly List<string> errors = new List<string>();
        static InputSettings.EditorInputBehaviorInPlayMode oldInput;
        static GameManager game;
        static ThirdPersonMotor motor;
        static PlayerCombat combat;
        static PlayerAbility ability;
        static ThirdPersonCamera orbit;
        static PlayerLockOn lockOn;
        static CharacterStamina stamina;
        static ShieldGuard guard;
        static CharacterHealth health;
        static PlayerActionStateMachine states;
        static ActorPosture posture;
        static float oldHorizontal, oldVertical, oldShake;
        static bool oldInvert;
        static SoulslikeValidation() { EditorApplication.update += Update; EditorApplication.playModeStateChanged += State; }
        static void Update()
        {
            if (routine != null && EditorApplication.isPlaying && frame != Time.frameCount)
            {
                frame = Time.frameCount;
                try { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Soulslike tests timed out"); if (!routine.MoveNext()) Finish(null); }
                catch (Exception error) { Finish(error); }
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/BellatorSoulslike.test")) return;
            File.Delete("Temp/BellatorSoulslike.test");
            try { Run(); }
            catch (Exception error) { File.WriteAllText("Temp/BellatorSoulslike.test-result.txt", "RESULT: FAIL\n" + error); Debug.LogException(error); }
        }
        [MenuItem("Bellator/Soulslike/Test Movement Camera and Combat")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SoulslikeStageTools.CheckIntegrity();
            EditorSceneManager.OpenScene(GameInterface.MainScene); SessionState.SetBool(Active, true); EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            report.Clear(); errors.Clear(); report.AppendLine("Bellator Soulslike — " + DateTime.UtcNow.ToString("u") + " — Unity " + Application.unityVersion + " — real Play Mode, physics, Input System and scene transitions");
            oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            oldHorizontal=GameSettings.HorizontalSensitivity; oldVertical=GameSettings.VerticalSensitivity; oldShake=GameSettings.CameraShake; oldInvert=GameSettings.InvertVertical;
            Application.logMessageReceived += Log; deadline = EditorApplication.timeSinceStartup + 300; frame = -1; routine = Flatten(Checks());
        }
        static void Log(string message, string trace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                (trace.Contains("Bellator.") || message.Contains("NullReference") || message.Contains("MissingReference") || message.Contains("NavMeshAgent"))) errors.Add(message);
        }
        static void Finish(Exception error)
        {
            Application.logMessageReceived -= Log;
            if (error == null && errors.Count > 0) error = new InvalidOperationException(string.Join("\n", errors));
            routine = null; SessionState.SetBool(Active, false); Time.timeScale = 1;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
            GameSettings.SetHorizontalSensitivity(oldHorizontal); GameSettings.SetVerticalSensitivity(oldVertical); GameSettings.SetCameraShake(oldShake); GameSettings.SetInvertVertical(oldInvert); GameSettings.Save();
            report.AppendLine(error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error);
            File.WriteAllText("Assets/Game/Documentation/SoulslikeValidation.txt", report.ToString()); File.WriteAllText("Temp/BellatorSoulslike.test-result.txt", report.ToString());
            if (error != null) Debug.LogException(error); else Debug.Log("Bellator Soulslike validation passed.");
            EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            while (stack.Count > 0) { var top = stack.Peek(); if (!top.MoveNext()) { stack.Pop(); continue; } if (top.Current is IEnumerator nested) { stack.Push(nested); continue; } yield return null; }
        }
        static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: " + message); File.WriteAllText("Temp/BellatorSoulslike.progress.txt", report.ToString()); }
        static IEnumerator Wait(float seconds) { float end = Time.time + seconds; while (Time.time < end) yield return null; }
        static IEnumerator Scene(string name) { while (SceneManager.GetActiveScene().name != name) yield return null; yield return null; yield return null; }
        static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        static void Cache()
        {
            game = UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(game != null, "Arena manager missing");
            var player = game.Player; motor = player.GetComponent<ThirdPersonMotor>(); combat = player.GetComponent<PlayerCombat>(); ability = player.GetComponent<PlayerAbility>();
            health = player.GetComponent<CharacterHealth>(); stamina = player.GetComponent<CharacterStamina>(); guard = player.GetComponent<ShieldGuard>();
            states = player.GetComponent<PlayerActionStateMachine>(); posture = player.GetComponent<ActorPosture>(); lockOn = player.GetComponent<PlayerLockOn>(); orbit = Camera.main.GetComponent<ThirdPersonCamera>();
            game.Waves.ManualSimulation = true; combat.ManualSimulation = true; ability.ManualSimulation = true; motor.AcceptInput = false; orbit.AcceptInput = false; stamina.enabled = false;
        }
        static void PlacePlayer(Vector3 position, float yaw = 0)
        {
            lockOn.Clear(); guard.ResetGuard(); combat.ResetTrainingState(); states.ResetState(); posture.ResetPosture(); ability.ResetAbility();
            motor.Teleport(position, Quaternion.Euler(0,yaw,0)); Camera.main.transform.rotation = Quaternion.identity;
            for (int i=0;i<12;i++) Step(Vector2.zero);
            stamina.Restore(); health.Restore();
        }
        static void Step(Vector2 axis, bool sprint=false, bool dodge=false, float dt=.02f)
        { motor.ProcessMovement(axis,sprint,dodge,dt); Physics.SyncTransforms(); }
        static void AdvanceDodge(float seconds)
        { for (float time=0;time<seconds;time+=.01f) Step(Vector2.zero,false,false,Mathf.Min(.01f,seconds-time)); }
        static void PlaceEnemy(EnemyBrain enemy, Vector3 position)
        {
            enemy.AutoThink=false; enemy.ResetActor(); enemy.GetComponent<EnemyNavigation>().Warp(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(motor.transform.position-position,Vector3.up)));
            enemy.SetTarget(game.Player.GetComponent<DamageReceiver>()); enemy.GetComponent<ShieldGuard>().Request(false); Physics.SyncTransforms();
        }
        static float Duration(WeaponDefinition weapon, int index, bool heavy=false)
        {
            var profile = heavy ? weapon.heavyAttack : weapon.lightCombo[index];
            return (profile.windup+profile.execution+profile.active+profile.recovery)/weapon.attackSpeed+weapon.recoveryTime+(!heavy && index==weapon.lightCombo.Length-1 && !weapon.IsRanged ? weapon.finisherRecovery : 0);
        }
        static void FrameCamera()
        {
            orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());
            float verticalDelta=(orbit.Pitch-18)/(.12f*GameSettings.Sensitivity*GameSettings.VerticalSensitivity)*(GameSettings.InvertVertical?-1:1);
            orbit.UpdateOrbit(new Vector2(0,verticalDelta),0,.02f);
            for(int i=0;i<90;i++) orbit.UpdateOrbit(Vector2.zero,0,.02f);
        }
        static IEnumerator Checks()
        {
            yield return null; yield return null;
            Assert(PlayerSession.EnterGuest(),"Guest session setup");
            Button("PlayButton").onClick.Invoke(); Button("StoryModeButton").onClick.Invoke(); yield return Scene("RomanArena"); Cache();
            Assert(states!=null && lockOn!=null && posture!=null && motor.GetComponents<ThirdPersonMotor>().Length==1 && motor.GetComponent<Rigidbody>()==null,"Player architecture");
            lockOn.ControlsSuspended=true;
            var keyboard=Keyboard.current;
            var keys = new[] { Key.W,Key.S,Key.A,Key.D,Key.LeftShift,Key.Space,Key.Tab,Key.C,Key.Z };
            foreach(var key in keys)
            {
                string name=key==Key.Tab?"LockOn":key==Key.C?"NextTarget":key==Key.Z?"PreviousTarget":key==Key.Space?"Dodge":key==Key.LeftShift?"Sprint":"Move";
                KeyboardState simulated = new KeyboardState(key);
                bool observed = false;
                Action inject = () => { if (InputState.currentUpdateType == InputUpdateType.Dynamic) InputSystem.QueueStateEvent(keyboard, simulated); };
                Action observe = () =>
                {
                    if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
                    var action=InputSystem.ListEnabledActions().First(a=>a.actionMap?.name=="BellatorThirdPerson" && a.name==name);
                    observed |= name == "Move" ? action.ReadValue<Vector2>().sqrMagnitude > .5f : action.IsPressed();
                };
                InputSystem.onBeforeUpdate += inject; InputSystem.onAfterUpdate += observe;
                try
                {
                    yield return null; yield return null;
                    Assert(observed,"Input binding failed: "+key+"; enabled="+keyboard.enabled);
                    simulated = new KeyboardState(); yield return null; yield return null;
                }
                finally { InputSystem.onBeforeUpdate -= inject; InputSystem.onAfterUpdate -= observe; }
            }
            lockOn.ControlsSuspended=false;
            Pass("All three scenes have no missing scripts; one CharacterController motor; actual Input System WASD/Shift/Space/Tab/C/Z bindings.");
            foreach(var axis in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
            {
                PlacePlayer(new Vector3(0,.08f,0)); Vector3 start=motor.transform.position;
                for(int i=0;i<35;i++) Step(axis);
                Vector3 delta=motor.transform.position-start;
                Assert(Vector3.Dot(delta,new Vector3(axis.x,0,axis.y))>1.3f,"Directional movement: "+axis+" delta="+delta);
                for(int i=0;i<20;i++) Step(Vector2.zero);
                Assert(motor.PlanarSpeed<.05f,"Stopping slide");
            }
            PlacePlayer(new Vector3(0,.08f,0)); Camera.main.transform.rotation=Quaternion.Euler(0,90,0); Vector3 origin=motor.transform.position;
            for(int i=0;i<35;i++) Step(Vector2.up);
            Assert(motor.transform.position.x-origin.x>1.3f && Mathf.Abs(motor.transform.position.z-origin.z)<.2f,"Camera-relative movement");
            float straight=0, diagonal=0;
            foreach(bool diag in new[]{false,true}) { PlacePlayer(new Vector3(0,.08f,0)); origin=motor.transform.position; for(int i=0;i<35;i++) Step(diag?Vector2.one:Vector2.up); if(diag) diagonal=Vector3.Distance(origin,motor.transform.position); else straight=Vector3.Distance(origin,motor.transform.position); }
            Assert(Mathf.Abs(straight-diagonal)<.08f,"Diagonal speed bonus");
            PlacePlayer(new Vector3(0,.08f,-22.8f)); orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>());
            for(int i=0;i<20;i++) orbit.UpdateOrbit(Vector2.zero,0,.02f);
            Assert(orbit.CurrentDistance<1.5f && !Physics.OverlapSphere(orbit.transform.position,.25f).Any(orbit.IsExternal),"Camera wall clipping");
            PlacePlayer(new Vector3(0,.08f,0)); orbit.Configure(motor.transform,motor.GetComponent<ThirdPersonInput>()); orbit.UpdateOrbit(new Vector2(100,0),0,.02f);
            Assert(Mathf.Abs(Mathf.DeltaAngle(0,orbit.transform.eulerAngles.y))>1,"Free camera yaw");
            orbit.UpdateOrbit(new Vector2(0,100000),0,.02f); Assert(orbit.Pitch>=-25 && orbit.Pitch<=65,"Camera pitch clamp");
            orbit.UpdateOrbit(new Vector2(0,-100000),-10000,.02f); for(int i=0;i<80;i++) orbit.UpdateOrbit(Vector2.zero,0,.02f);
            Assert(orbit.CurrentDistance>7.8f && orbit.CurrentDistance<=8.01f,"Camera zoom maximum");
            orbit.UpdateOrbit(Vector2.zero,10000,.02f); Assert(orbit.CurrentDistance>=1.49f && orbit.CurrentDistance<=1.51f,"Camera zoom minimum");
            orbit.UpdateOrbit(Vector2.zero,-292,.02f); // Return near the default distance.
            Pass("WASD in four directions, camera-relative forward, normalized diagonals, acceleration/stopping, free mouse orbit, pitch/zoom limits and real arena wall camera collision.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Gladiator.prefab").GetComponent<EnemyBrain>();
            var enemies=new List<EnemyBrain>();
            for(int i=0;i<3;i++) { var enemy=UnityEngine.Object.Instantiate(prefab,new Vector3(i*3,.04f,7),Quaternion.identity); enemy.AutoThink=false; enemies.Add(enemy); }
            yield return null; yield return null;
            PlacePlayer(new Vector3(0,.08f,0));
            for(int i=0;i<3;i++) PlaceEnemy(enemies[i],new Vector3(i*3,.04f,7));
            enemies[2].gameObject.SetActive(false);
            FrameCamera();
            Assert(lockOn.Toggle() && lockOn.Target==enemies[0].GetComponent<DamageReceiver>(),"Lock-on central target selection");
            Assert(lockOn.SwitchTarget(1) && lockOn.Target==enemies[1].GetComponent<DamageReceiver>(),"Target switch");
            Step(Vector2.right); Assert(Vector3.Dot(motor.transform.forward,Vector3.ProjectOnPlane(lockOn.TargetPoint-motor.transform.position,Vector3.up).normalized)>.8f,"Lock facing while strafing");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="ValidationSightWall"; wall.transform.position=new Vector3(0,2,3.5f); wall.transform.localScale=new Vector3(20,4,.3f); Physics.SyncTransforms();
            lockOn.TickLock(.5f); Assert(lockOn.IsLocked,"Occlusion grace lost immediately"); lockOn.TickLock(.6f); Assert(!lockOn.IsLocked,"Occlusion timeout");
            Assert(!lockOn.Toggle(),"Selected enemy through wall"); UnityEngine.Object.Destroy(wall); yield return null; Physics.SyncTransforms();
            Assert(lockOn.Toggle(),"Reacquire after obstacle"); var locked=lockOn.Target; locked.transform.position=new Vector3(0,.04f,30); lockOn.TickLock(.02f); Assert(!lockOn.IsLocked,"Lock range release");
            PlaceEnemy(enemies[0],new Vector3(0,.04f,7)); PlaceEnemy(enemies[1],new Vector3(3,.04f,7)); Assert(lockOn.Toggle(),"Reacquire for death"); lockOn.Target.Health.Damage(10000); lockOn.TickLock(.02f); Assert(!lockOn.IsLocked,"Dead target lock release");
            Pass("Distance/screen target scoring, C/Z switching, strafe orientation, blocked acquisition, timed occlusion release, maximum range and target death release.");
            foreach(var definition in game.Player.Catalog.classes)
            {
                game.Player.Apply(definition); combat.ManualSimulation=ability.ManualSimulation=true;
                PlacePlayer(new Vector3(0,.08f,0));
                foreach(var enemy in enemies) enemy.gameObject.SetActive(false);
                var receiver=game.Player.GetComponent<DamageReceiver>(); float full=stamina.Current;
                Step(Vector2.up,false,true,.01f);
                Assert(motor.IsDodging && Mathf.Abs(stamina.Current-(full-definition.dodge.cost))<.02f,"Dodge cost/style "+definition.displayName);
                Assert(motor.DodgeStyle==definition.dodge.style && !motor.IsInvulnerable,"Dodge starts vulnerable");
                Assert(receiver.Receive(new DamageInfo(1,motor.transform.position,motor.transform.position+Vector3.forward*2,null,0)),"Damage rejected before invulnerability");
                AdvanceDodge(definition.dodge.duration*.3f-.01f); float hp=health.Current;
                Assert(motor.IsInvulnerable && motor.GetComponent<CharacterController>().enabled,"Middle dodge window/physical collision");
                Assert(!receiver.Receive(new DamageInfo(100,motor.transform.position,motor.transform.position+Vector3.forward*2,null,100)) && health.Current==hp,"Invulnerability failed");
                float spent=stamina.Current; Step(Vector2.right,false,true,.01f); Assert(stamina.Current==spent,"Repeated dodge spent stamina");
                Assert(!combat.TryAttack(false) && !ability.TryActivate(),"Incompatible dodge action");
                AdvanceDodge(definition.dodge.duration*.5f); Assert(!motor.IsInvulnerable,"Dodge invulnerability lasted too long");
                AdvanceDodge(definition.dodge.duration*.22f); Assert(motor.IsDodging && !combat.TryAttack(false),"Dodge recovery missing");
                AdvanceDodge(definition.dodge.recovery+.04f); Assert(!motor.IsDodging,"Dodge stuck after recovery");
                PlacePlayer(new Vector3(0,.08f,-22.5f)); Step(Vector2.down,false,true); AdvanceDodge(definition.dodge.duration+definition.dodge.recovery);
                Assert(motor.transform.position.z> -24.2f,"Fast dodge crossed wall: "+definition.displayName);
                PlacePlayer(new Vector3(0,.08f,0)); origin=motor.transform.position;
                for(int i=0;i<40;i++) Step(Vector2.up,true);
                Assert(stamina.Current<stamina.Maximum && motor.IsRunning,"Sprint stamina "+definition.displayName);
                for(int i=0;i<15;i++) Step(Vector2.zero);
                stamina.TryConsume(stamina.Current); origin=motor.transform.position; Step(Vector2.up,true,true); Assert(!motor.IsDodging && !motor.IsRunning,"Zero-stamina action allowed");
                for(int i=0;i<20;i++) Step(Vector2.up,true); Assert(Vector3.Distance(origin,motor.transform.position)>.4f && stamina.Current>=0,"Walking without stamina");
                for(int i=0;i<15;i++) Step(Vector2.zero); yield return Wait(1.3f); stamina.Regenerate(.5f); Assert(stamina.Current>0,"Stamina regeneration delay");
                PlacePlayer(new Vector3(0,.08f,0));
                Assert(ability.TryActivate(),"Class ability unavailable: "+definition.displayName);
                if (definition.ability.kind == AbilityKind.DesperateSteps)
                {
                    float available = stamina.Current;
                    Step(Vector2.up,false,true,.01f);
                    Assert(motor.IsDodging && Mathf.Abs(available-stamina.Current-definition.dodge.cost*definition.ability.dodgeCostMultiplier)<.02f,"Desperate Steps dodge discount");
                    AdvanceDodge(definition.dodge.duration+definition.dodge.recovery*definition.ability.dodgeCooldownMultiplier+.01f);
                    Assert(!motor.IsDodging,"Desperate Steps recovery discount");
                }
                for(int i=0;i<120;i++) ability.TickAbility(.01f);
                Assert(ability.CooldownRemaining>0 && !ability.TryActivate(),"Ability cooldown bypass"); ability.ResetAbility(); combat.ResetTrainingState();
                foreach(var projectile in UnityEngine.Object.FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(projectile.gameObject);
                Assert(game.Player.GetComponent<WeaponEquipment>().IsFixed,"Weapon switching reintroduced");
                Pass(definition.displayName+": class speed/sprint, "+definition.dodge.style+", cost, partial invulnerability, recovery, wall collision, zero-stamina restrictions/walking/regeneration and original Q ability/cooldown; fixed weapon retained.");
            }
            game.Player.Apply(game.Player.Catalog.classes.First(c=>c.id==GladiatorClassId.Warrior)); PlacePlayer(new Vector3(0,.08f,0));
            var target=enemies[0]; target.gameObject.SetActive(true); target.GetComponent<CharacterHealth>().ConfigureMaximum(10000); PlaceEnemy(target,new Vector3(0,.04f,1.4f));
            var weapon=game.Player.GetComponent<WeaponEquipment>().Current;
            for(int combo=0;combo<weapon.lightCombo.Length;combo++)
            {
                if(combo==0) Assert(combat.TryAttack(false),"Light attack start");
                Assert(combat.ComboStep==combo+1,"Combo order"); float total=Duration(weapon,combo); float before=target.GetComponent<CharacterHealth>().Current; int hits=combat.ConfirmedHits;
                combat.TickCombat(weapon.lightCombo[combo].windup/weapon.attackSpeed*.5f); Assert(target.GetComponent<CharacterHealth>().Current==before,"Damage during preparation");
                Step(Vector2.up,false,true); Assert(!motor.IsDodging && combat.IsAttacking,"Free attack dodge cancel");
                float bufferAt=total*weapon.lightCombo[combo].comboWindowStart-.08f;
                while(combat.AttackProgress*total<bufferAt) combat.TickCombat(.01f);
                Assert(target.GetComponent<CharacterHealth>().Current<before && combat.ConfirmedHits==hits+1,"Active hit or single-swing deduplication");
                combat.TryAttack(false); int oldCombo=combat.ComboStep;
                while(combat.IsAttacking && combat.ComboStep==oldCombo) combat.TickCombat(.01f);
                if(combo==weapon.lightCombo.Length-1) Assert(!combat.IsAttacking,"Infinite finisher combo");
            }
            stamina.Restore(); Assert(combat.TryAttack(true),"Heavy start");
            float heavyTotal=Duration(weapon,0,true); combat.TickCombat(.1f); Step(Vector2.up,false,true); Assert(combat.IsAttacking && !motor.IsDodging,"Heavy windup cancel");
            while(combat.AttackProgress<.95f) combat.TickCombat(.01f);
            Assert(combat.CanDodgeCancel,"Heavy recovery cancel window"); Step(Vector2.right,false,true); Assert(motor.IsDodging && !combat.IsAttacking,"Permitted recovery dodge cancel"); AdvanceDodge(1.2f);
            Pass("Three paid light attacks with preparation/active/recovery phases, one hit per swing, buffered continuation and finisher stop; heavy commitment and its late recovery dodge-cancel window.");
            PlacePlayer(new Vector3(0,.08f,0)); var playerReceiver=game.Player.GetComponent<DamageReceiver>();
            guard.Request(true); float beforeHealth=health.Current, beforeStamina=stamina.Current;
            playerReceiver.Receive(new DamageInfo(10,motor.transform.position+Vector3.up,motor.transform.position+Vector3.forward*2,null,10));
            Assert(playerReceiver.LastImpact.Blocked && health.Current>beforeHealth-5 && stamina.Current<beforeStamina,"Frontal shield block");
            beforeHealth=health.Current; playerReceiver.Receive(new DamageInfo(10,motor.transform.position+Vector3.up,motor.transform.position-Vector3.forward*2,null,10));
            Assert(!playerReceiver.LastImpact.Blocked && health.Current<beforeHealth-5,"Rear shield bypass");
            stamina.TryConsume(stamina.Current); playerReceiver.Receive(new DamageInfo(10,motor.transform.position,motor.transform.position+Vector3.forward*2,null,0));
            Assert(guard.IsBroken && states.IsStaggered,"Guard break/posture stagger");
            states.Tick(5); posture.Tick(5); guard.ResetGuard(); stamina.Restore(); health.Restore(); posture.ResetPosture();
            Assert(combat.TryAttack(true),"Posture interruption setup"); playerReceiver.Receive(new DamageInfo(1,motor.transform.position,motor.transform.position+Vector3.forward*2,null,posture.Maximum+1));
            Assert(states.IsStaggered && !combat.IsAttacking && !combat.TryAttack(false) && !ability.TryActivate(),"Posture failed to interrupt actions");
            float stagger=posture.StaggerRemaining; playerReceiver.Receive(new DamageInfo(1,motor.transform.position,motor.transform.position+Vector3.forward*2,null,posture.Maximum+1)); Assert(posture.StaggerRemaining<=stagger,"Stagger extended indefinitely");
            posture.Tick(1); states.Tick(1); playerReceiver.Receive(new DamageInfo(1,motor.transform.position,motor.transform.position+Vector3.forward*2,null,posture.Maximum+1)); Assert(!posture.IsStaggered,"Stagger immunity missing");
            Pass("Front-only shield blocks with impact stamina cost, rear damage, guard break, separate posture threshold, action interruption and protection against repeated stun.");
            PlacePlayer(new Vector3(0,.08f,0)); health.ConfigureMaximum(10000);
            for(int i=0;i<3;i++) { enemies[i].gameObject.SetActive(true); enemies[i].GetComponent<CharacterHealth>().Restore(); PlaceEnemy(enemies[i],new Vector3((i-1)*1.2f,.04f,1.4f)); }
            Assert(enemies[0].TryBeginAttack(),"Aggression first attacker"); Assert(!enemies[1].TryBeginAttack(),"Simultaneous attack spacing missing");
            yield return Wait(.5f); enemies[1].Tick(.1f); enemies[1].Tick(.1f); Assert(enemies[1].TryBeginAttack() || enemies[1].State==EnemyState.Attack,"Second attack lease; state="+enemies[1].State+"; sight="+enemies[1].HasLineOfSight());
            yield return Wait(.5f); enemies[2].Tick(.15f); Assert(!enemies[2].TryBeginAttack() && enemies.Count(e=>e.State==EnemyState.Attack)<=2,"Melee aggression budget exceeded");
            foreach(var enemy in enemies) enemy.GetComponent<CharacterHealth>().Damage(100000);
            float safeHealth=health.Current; foreach(var enemy in enemies) { enemy.Tick(1); Assert(!enemy.TryBeginAttack(),"Dead enemy attacked"); }
            Assert(health.Current==safeHealth,"Damage after enemy death");
            Pass("Three enemies compete for two melee leases with spaced starts; dead attackers cannot start or finish damage.");
            foreach(var enemy in enemies) UnityEngine.Object.Destroy(enemy.gameObject); yield return null;
            game.Player.Apply(game.Player.Catalog.classes.First(c=>c.id==GladiatorClassId.Hunter)); PlacePlayer(new Vector3(0,.08f,0));
            target=UnityEngine.Object.Instantiate(prefab,new Vector3(3,.04f,8),Quaternion.identity); target.AutoThink=false; yield return null; yield return null; PlaceEnemy(target,new Vector3(3,.04f,8));
            FrameCamera();
            Assert(lockOn.Toggle(),"Hunter ranged lock"); Assert(combat.TryAttack(false),"Crossbow attack"); combat.TickCombat(.2f);
            var bolt=UnityEngine.Object.FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None).FirstOrDefault(); Assert(bolt!=null && !combat.CrossbowLoaded,"Locked crossbow did not fire");
            for(int i=0;i<25 && bolt!=null;i++) bolt.Simulate(.01f);
            Assert(target.GetComponent<CharacterHealth>().Current<target.GetComponent<CharacterHealth>().Maximum,"Locked projectile failed to hit selected target");
            combat.TickCombat(1); combat.TickCombat(2); Assert(combat.CrossbowLoaded,"Crossbow automatic reload");
            game.TogglePause(); Assert(Time.timeScale==0 && !combat.TryAttack(false) && !ability.TryActivate(),"Paused actions");
            UnityEngine.Object.FindAnyObjectByType<GameInterface>().OpenSettings();
            Assert(GameObject.Find("HorizontalSensitivitySlider")!=null && GameObject.Find("VerticalSensitivitySlider")!=null && GameObject.Find("InvertVerticalButton")!=null && GameObject.Find("CameraShakeSlider")!=null,"Camera options missing");
            GameObject.Find("HorizontalSensitivitySlider").GetComponent<Slider>().value=1.4f; GameObject.Find("VerticalSensitivitySlider").GetComponent<Slider>().value=.8f; Button("InvertVerticalButton").onClick.Invoke(); GameObject.Find("CameraShakeSlider").GetComponent<Slider>().value=0;
            Assert(Mathf.Abs(GameSettings.HorizontalSensitivity-1.4f)<.01f && Mathf.Abs(GameSettings.VerticalSensitivity-.8f)<.01f && GameSettings.CameraShake==0,"Camera preferences");
            ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/SoulslikeSettings.png"); yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<GameInterface>().CloseSettings(); game.Resume(); motor.AcceptInput=false; orbit.AcceptInput=false;
            health.Damage(100000); yield return null; yield return null; Assert(game.State==MatchState.Defeat && !lockOn.IsLocked && !motor.IsDodging && !combat.IsAttacking,"Terminal action cleanup");
            var old=game; game.Restart(); while(old!=null) yield return null; yield return Scene("RomanArena"); Cache(); Assert(game.Player.Current.id==GladiatorClassId.Hunter && game.State==MatchState.Preparing && !game.Gate.IsOpen,"Fresh restart/class preservation");
            game.ReturnToMenu(); yield return Scene("MainMenu"); Assert(SceneManager.GetActiveScene().name=="MainMenu","Return to main menu");
            Pass("Hunter lock-aimed physical bolt hits selected target, reload works; camera settings/pause work; terminal state clears lock/dodge/attack; death restart retains class and menu transition completes.");
        }
    }
}
