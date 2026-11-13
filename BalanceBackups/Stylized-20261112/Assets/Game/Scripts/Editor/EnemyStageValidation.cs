using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class EnemyStageValidation
    {
        private const string Active="Bellator.Stage4.Test";
        private static IEnumerator routine;
        private static int frame=-1;
        private static readonly StringBuilder report=new StringBuilder();
        private static readonly List<string> errors=new List<string>();
        static EnemyStageValidation()
        { EditorApplication.update+=CheckRequest; EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=StateChanged; }
        private static void CheckRequest()
        {
            const string path="Temp/BellatorEnemies.test";
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(path)) return;
            File.Delete(path); Run();
        }
        [MenuItem("Bellator/Test Stage 4 Enemies in Play Mode")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!UnityEngine.Object.FindAnyObjectByType<EnemyEncounter>()) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ArenaBuilder.ScenePath);
            SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode();
        }
        private static void StateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active,false))
            { report.Clear(); errors.Clear(); report.AppendLine("Stage 4 — Unity "+Application.unityVersion+" — real Play Mode"); Application.logMessageReceived+=Log; routine=Checks(); frame=-1; }
        }
        private static void Log(string message,string trace,LogType type)
        {
            if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("NavMeshAgent"))) errors.Add(message);
        }
        private static void Tick()
        {
            if(routine==null || !EditorApplication.isPlaying || frame==Time.frameCount) return;
            frame=Time.frameCount; try { if(!routine.MoveNext()) Finish(null); } catch(Exception e) { Finish(e); }
        }
        private static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;
            if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null; SessionState.SetBool(Active,false);
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/Stage4Validation.txt",report.ToString());
            File.WriteAllText("Temp/BellatorEnemies.test-result.txt",report.ToString());
            if(error==null) Debug.Log("Bellator Stage 4 Play Mode validation passed."); else Debug.LogException(error);
            EditorApplication.ExitPlaymode();
        }
        private static void Assert(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        private static void Pass(string message)
        { report.AppendLine("PASS: "+message); File.WriteAllText("Temp/BellatorEnemies.progress.txt",report.ToString()); }
        private static void Place(EnemyBrain enemy,Vector3 position,Quaternion rotation)
        { enemy.AutoThink=false; enemy.ResetActor(); Assert(enemy.GetComponent<EnemyNavigation>().Warp(position,rotation),"Enemy Warp failed"); Physics.SyncTransforms(); }
        private static IEnumerator Checks()
        {
            MatchStageBuilder.PrepareLegacySandbox();
            yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<Bellator.Classes.PlayerClassController>()?.RestoreLegacyForValidation();
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            var motor=player.GetComponent<ThirdPersonMotor>(); var health=player.GetComponent<CharacterHealth>(); var stamina=player.GetComponent<CharacterStamina>();
            var receiver=player.GetComponent<DamageReceiver>(); var guard=player.GetComponent<ShieldGuard>();
            var camera=Camera.main; camera.GetComponent<ThirdPersonCamera>().AcceptInput=false;
            player.ManualSimulation=true; motor.AcceptInput=false;
            player.GetComponent<CombatInput>().enabled=false; player.GetComponent<ThirdPersonInput>().enabled=false;
            var enemies=UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert(enemies.Length==3,"Expected exactly three initial enemies");
            var common=enemies.First(e=>e.Definition.role==EnemyRole.Gladiator);
            var heavy=enemies.First(e=>e.Definition.role==EnemyRole.Heavy);
            var archer=enemies.First(e=>e.Definition.role==EnemyRole.Archer);
            foreach(var e in enemies) { e.AutoThink=false; e.GetComponent<EnemyNavigation>().Stop(); e.SetTarget(receiver); Assert(e.GetComponent<EnemyNavigation>().Ready,"Enemy not on NavMesh"); }
            Assert(heavy.GetComponent<CharacterHealth>().Maximum>common.GetComponent<CharacterHealth>().Maximum && common.GetComponent<CharacterHealth>().Maximum>archer.GetComponent<CharacterHealth>().Maximum,"Health archetypes");
            Assert(heavy.Definition.moveSpeed<common.Definition.moveSpeed && heavy.GetComponent<CharacterController>().radius>common.GetComponent<CharacterController>().radius,"Heavy scale/speed");
            Pass("Exactly three archetypes, health/speed/size differences, weapons and active NavMesh agents.");

            Place(heavy,new Vector3(16,.04f,-7),Quaternion.identity);
            Place(archer,new Vector3(16,.04f,1),Quaternion.identity);
            motor.Teleport(new Vector3(-13,.08f,8),Quaternion.Euler(0,180,0)); Physics.SyncTransforms();
            Place(common,new Vector3(-13,.04f,0),Quaternion.identity);
            common.AutoThink=true;
            float until=Time.time+8,maxSide=0; bool approached=false;
            while(Time.time<until)
            {
                maxSide=Mathf.Max(maxSide,Mathf.Abs(common.transform.position.x+13));
                Assert(common.transform.position.y>-.15f,"Enemy fell through ground");
                if(Vector3.Distance(common.transform.position,motor.transform.position)<2.1f) { approached=true; break; }
                yield return null;
            }
            common.AutoThink=false; common.GetComponent<EnemyNavigation>().Stop();
            Assert(approached && maxSide>1.5f,"Navigation failed to go around timber barricade; side="+maxSide);
            Pass("Detection, pursuit and physical navigation around the real timber obstacle.");

            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); health.Restore(); stamina.Restore(); player.ResetTrainingState();
            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0));
            camera.transform.position=new Vector3(4,3,-5); camera.transform.LookAt(Vector3.up);
            Assert(common.TryBeginAttack(),"Common attack did not start");
            float commonHitTime=0; int steps=0;
            while(common.State==EnemyState.Attack && steps++<120)
            { common.Tick(1f/60); commonHitTime+=1f/60; if(steps==14) Capture(camera,"Common"); yield return null; }
            Assert(health.Current==88 && common.SuccessfulHits>=1,"Common did not damage player exactly once: "+health.Current);
            Assert(!common.TryBeginAttack(),"Common bypassed recovery");
            Pass("Common melee hits player, respects active window/deduplication and recovery.");

            health.Restore(); stamina.Restore(); player.ResetTrainingState(); guard.Request(true);
            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0));
            Assert(common.TryBeginAttack(),"Shield-test attack start"); steps=0;
            while(common.State==EnemyState.Attack && steps++<120) { common.Tick(1f/60); yield return null; }
            Assert(Mathf.Abs(health.Current-97.6f)<.15f && receiver.LastImpact.Blocked && stamina.Current<100,"Player shield did not block AI attack");
            guard.Request(false); health.Restore(); stamina.Restore();
            Pass("Player shield blocks real AI damage using existing ShieldGuard and stamina.");

            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0));
            Assert(common.BeginDefense(.8f),"Common shield defense"); float hp=common.GetComponent<CharacterHealth>().Current;
            common.GetComponent<DamageReceiver>().Receive(new DamageInfo(20,common.transform.position+Vector3.up,motor.transform.position,motor.transform));
            Assert(Mathf.Abs(common.GetComponent<CharacterHealth>().Current-(hp-4))<.15f,"Enemy shield did not reduce player damage");
            var randomState=UnityEngine.Random.state; int seed=0;
            for(;seed<64;seed++) { UnityEngine.Random.InitState(seed); if(UnityEngine.Random.value<common.Definition.defenseChance) break; }
            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0));
            UnityEngine.Random.InitState(seed); common.Tick(1f/60); UnityEngine.Random.state=randomState;
            Assert(common.State==EnemyState.Defend,"AI occasional defense decision");
            Pass("Common uses shield and its probabilistic defense decision; same damage pipeline as player.");

            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0)); common.GetComponent<CharacterHealth>().Restore();
            Assert(common.TryBeginAttack(),"Stun-test attack start");
            common.GetComponent<DamageReceiver>().Receive(new DamageInfo(3,common.transform.position+Vector3.up,motor.transform.position,motor.transform));
            Assert(common.State==EnemyState.Stunned,"Damage failed to stun/cancel attack");
            float before=health.Current;
            for(int i=0;i<10;i++) { common.Tick(1f/60); yield return null; }
            Assert(health.Current==before,"Interrupted attack still hit");
            Pass("Damage reaction enters Stunned and cancels pending melee damage.");

            Place(common,new Vector3(-15,.04f,-6),Quaternion.identity);
            Place(heavy,new Vector3(0,.04f,2.0f),Quaternion.Euler(0,180,0)); health.Restore();
            Assert(heavy.TryBeginAttack(),"Heavy attack did not start");
            steps=0; float heavyHitTime=0;
            while(heavy.State==EnemyState.Attack && steps++<160)
            { heavy.Tick(1f/60); heavyHitTime+=1f/60; if(steps==28) Capture(camera,"Heavy"); yield return null; }
            Assert(health.Current==73 && heavyHitTime>commonHitTime+.3f,"Heavy damage/slower timing: "+health.Current);
            Pass("Heavy uses slower high-damage attack and shared recovery/hit rules.");

            var heavyReceiver=heavy.GetComponent<DamageReceiver>(); var commonReceiver=common.GetComponent<DamageReceiver>();
            heavyReceiver.Health.Restore(); float allyHP=heavyReceiver.Health.Current;
            Assert(!heavyReceiver.Receive(new DamageInfo(20,heavy.transform.position,common.transform.position,common.transform)) && heavyReceiver.Health.Current==allyHP,"Allied melee damaged ally");
            var commonFaction=common.GetComponent<CombatFaction>(); commonFaction.AllowFriendlyFire=true;
            Assert(heavyReceiver.Receive(new DamageInfo(20,heavy.transform.position,common.transform.position,common.transform)) && heavyReceiver.Health.Current==allyHP-20,"Configured friendly fire did not work");
            commonFaction.AllowFriendlyFire=false;
            var arrowPrefab=archer.Definition.weapon.projectilePrefab;
            var arrow=UnityEngine.Object.Instantiate(arrowPrefab,heavy.transform.position+new Vector3(0,1.3f,-2),Quaternion.identity).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(archer.transform,15,Vector3.forward*80,0,2,20); arrow.Simulate(.1f);
            Assert(heavyReceiver.Health.Current==allyHP-20,"Allied projectile damaged ally"); yield return null;
            Pass("Faction filtering blocks allied melee/projectiles; friendly-fire option can explicitly allow damage.");

            Place(heavy,new Vector3(15,.04f,-6),Quaternion.identity);
            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); health.Restore(); guard.Request(false);
            Place(archer,new Vector3(0,.04f,9),Quaternion.Euler(0,180,0));
            Assert(archer.TryBeginAttack(),"Archer attack did not start");
            steps=0;
            while(archer.State==EnemyState.Attack && steps++<140)
            { archer.Tick(1f/60); if(steps==25) Capture(camera,"Archer"); yield return null; }
            until=Time.time+1.2f; while(Time.time<until) yield return null;
            Assert(archer.ProjectilesFired>0 && health.Current==89,"AI arrow did not hit player: "+health.Current);
            Pass("Archer aims and launches real ballistic arrows that hit the player.");

            Place(archer,new Vector3(0,.04f,3.5f),Quaternion.Euler(0,180,0)); health.Restore();
            archer.AutoThink=true; until=Time.time+2;
            while(Time.time<until) yield return null;
            archer.AutoThink=false; archer.GetComponent<EnemyNavigation>().Stop();
            Assert(Vector3.Distance(archer.transform.position,motor.transform.position)>5.4f,"Archer failed to retreat");
            Pass("Threatened archer retreats over NavMesh and restores separation.");

            Place(archer,new Vector3(0,.04f,9),Quaternion.Euler(0,180,0)); archer.Tick(.01f);
            var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube); barrier.name="Stage4SightBarrier";
            barrier.transform.position=new Vector3(0,1.4f,4); barrier.transform.localScale=new Vector3(4,3,.2f); Physics.SyncTransforms();
            Assert(!archer.HasLineOfSight(),"Cover did not occlude archer");
            Assert(!archer.TryBeginAttack(),"Archer fired through solid cover");
            // A non-blocked hit interrupts the current draw but preserves the last visible target in memory.
            archer.GetComponent<DamageReceiver>().Receive(new DamageInfo(1,archer.transform.position,motor.transform.position,motor.transform));
            archer.AutoThink=true; until=Time.time+4;
            while(Time.time<until && (!archer.HasLineOfSight() || Mathf.Abs(archer.transform.position.x)<1)) yield return null;
            archer.AutoThink=false; archer.GetComponent<EnemyNavigation>().Stop();
            Assert(Mathf.Abs(archer.transform.position.x)>1 && archer.HasLineOfSight(),"Archer failed to reposition around cover");
            UnityEngine.Object.Destroy(barrier); yield return null; Physics.SyncTransforms();
            Pass("Archer does not shoot through cover and navigates sideways to recover line of sight.");
            Place(common,new Vector3(0,.04f,1.5f),Quaternion.Euler(0,180,0)); health.Restore();
            Assert(common.TryBeginAttack(),"Death-test attack start");
            int attacks=common.AttacksStarted;
            commonReceiver.Receive(new DamageInfo(1000,common.transform.position+Vector3.up,motor.transform.position,motor.transform));
            Assert(common.State==EnemyState.Dead && !common.GetComponent<NavMeshAgent>().enabled && !common.GetComponent<CharacterController>().enabled,"Death did not disable navigation/collider");
            for(int i=0;i<80;i++) { common.Tick(1f/60); yield return null; }
            Assert(health.Current==100 && common.AttacksStarted==attacks && !common.TryBeginAttack(),"Dead enemy continued attacking");
            Pass("Death immediately cancels windup, navigation and all future melee attacks.");

            Place(archer,new Vector3(0,.04f,9),Quaternion.Euler(0,180,0)); health.Restore();
            arrow=UnityEngine.Object.Instantiate(arrowPrefab,new Vector3(0,1.3f,8),Quaternion.Euler(0,180,0)).GetComponent<CombatProjectile>();
            arrow.enabled=false; arrow.Launch(archer.transform,15,Vector3.back*30,0,2,20);
            archer.GetComponent<DamageReceiver>().Receive(new DamageInfo(1000,archer.transform.position,motor.transform.position,motor.transform));
            Assert(arrow.HasHit && archer.State==EnemyState.Dead,"Dead archer did not cancel its airborne projectile");
            arrow.Simulate(.5f); Assert(health.Current==100,"Projectile damaged after owner death"); yield return null;
            Pass("Archer death also cancels previously launched projectiles.");

            Place(heavy,new Vector3(0,.04f,1.8f),Quaternion.Euler(0,180,0)); heavyReceiver.Health.Restore();
            var equipment=player.GetComponent<WeaponEquipment>(); equipment.Equip(1); player.ResetTrainingState(); stamina.Restore();
            Assert(player.TryAttack(true),"Player heavy attack against AI");
            for(int i=0;i<110;i++){player.TickCombat(1f/60); yield return null;}
            Assert(heavyReceiver.Health.Current<heavyReceiver.Health.Maximum,"Player attack did not damage enemy");
            heavyReceiver.Receive(new DamageInfo(1000,heavy.transform.position,motor.transform.position,motor.transform));
            Assert(heavy.State==EnemyState.Dead,"Heavy could not be defeated");
            Pass("Player melee damages and can defeat heavy enemy through shared combat system.");

            var encounter=UnityEngine.Object.FindAnyObjectByType<EnemyEncounter>();
            UnityEngine.Object.FindAnyObjectByType<TrainingArenaControls>().ResetTraining();
            foreach(var e in enemies) { Assert(e.State!=EnemyState.Dead && e.GetComponent<CharacterHealth>().IsAlive && e.GetComponent<EnemyNavigation>().Ready,"Training reset failed to restore enemy"); }
            Pass("Training reset restores all three actors without spawning waves.");

            // Observe the three agents together: avoidance + CharacterController must prevent body stacking.
            motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity); health.Restore();
            Place(common,new Vector3(-2,.04f,4),Quaternion.Euler(0,180,0));
            Place(heavy,new Vector3(2,.04f,4),Quaternion.Euler(0,180,0));
            Place(archer,new Vector3(0,.04f,10),Quaternion.Euler(0,180,0));
            foreach(var e in enemies) e.AutoThink=true;
            until=Time.time+3.5f;
            while(Time.time<until) { if(health.Current<40) health.Restore(); yield return null; }
            float separation=Vector3.Distance(common.transform.position,heavy.transform.position);
            Assert(separation>.65f && enemies.All(e=>e.transform.position.y>-.15f),"Agents overlapped/fell through ground: "+separation);
            camera.transform.position=new Vector3(7,5,-8); camera.transform.LookAt(new Vector3(0,1.3f,3));
            Capture(camera,"Encounter"); ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/EnemiesHUD.png");
            yield return null; yield return null;
            UnityEngine.Object.FindAnyObjectByType<Bellator.Classes.PlayerClassController>()?.RestoreLegacyForValidation();
            Pass("All three AI active together; physical separation maintained; encounter image captured.");
        }
        private static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1280,800,24); var previous=RenderTexture.active; camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/Enemy"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}




