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

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class ClassBalanceTools
    {
        public const string LabScene="Assets/Game/Scenes/BalanceLab.unity";
        const string Active="Bellator.Balance.Validation";
        static IEnumerator routine;
        static int frame;
        static double deadline;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static ClassBalanceTools()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active,false))
                { report.Clear(); errors.Clear(); report.AppendLine("Unity "+Application.unityVersion+" | Real Play Mode | "+DateTime.UtcNow.ToString("u")); Application.logMessageReceived+=Log; routine=Checks(); frame=-1; deadline=EditorApplication.timeSinceStartup+240; }
            };
        }
        static void Update()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;
                try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Balance tests timed out"); if(!routine.MoveNext()) Finish(null); }
                catch(Exception error) { Finish(error); }
            }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!File.Exists("Temp/BellatorBalance.test")) return;
            File.Delete("Temp/BellatorBalance.test");
            try { Run(); } catch(Exception error) { File.WriteAllText("Temp/BellatorBalance.test-result.txt","RESULT: FAIL\n"+error); Debug.LogException(error); }
        }
        static void Log(string message,string trace,LogType type)
        { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) errors.Add(message); }
        [MenuItem("Bellator/Balance/Open Development Lab")]
        public static void BuildLab()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before opening the lab.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before opening the lab.");
            if(File.Exists(LabScene)) { EditorSceneManager.OpenScene(LabScene); return; }
            if(!AssetDatabase.CopyAsset(PlayerClassController.ArenaScene,LabScene)) throw new InvalidOperationException("Could not copy arena to isolated lab.");
            var scene=EditorSceneManager.OpenScene(LabScene);
            foreach(var manager in UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(manager.gameObject);
            foreach(var ui in UnityEngine.Object.FindObjectsByType<GameInterface>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(ui.gameObject);
            foreach(var training in UnityEngine.Object.FindObjectsByType<TrainingArenaControls>(FindObjectsInactive.Include,FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(training.gameObject);
            foreach(var encounter in UnityEngine.Object.FindObjectsByType<EnemyEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(encounter.gameObject);
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include,FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            var lab=new GameObject("DevelopmentBalanceLab").AddComponent<BalanceTrainingLab>();
            lab.player=UnityEngine.Object.FindAnyObjectByType<PlayerClassController>();
            lab.enemyPrefabs=new[]{"Gladiator","Heavy","Archer"}.Select(name=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/"+name+".prefab")).ToArray();
            if(lab.player==null || lab.enemyPrefabs.Any(prefab=>prefab==null)) throw new InvalidOperationException("Lab references missing");
            EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Bellator/Balance/Export Attribute Calculations")]
        public static void ExportCalculations()
        {
            foreach(var path in Directory.GetFiles("Assets/Game/ScriptableObjects/Classes","*.asset")) AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var classes=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset").classes;
            var csv=new StringBuilder("Class,Health,Stamina,Defense,Walk,Run,ComboDamage,ComboSeconds,BurstDPS,ComboStamina,SustainedCycleDPS,HeavyDamage,HeavySeconds,RollSeconds,Recovery,RollCost,InvulnerableSeconds,SpecialDamage,Cooldown\n");
            foreach(var c in classes)
            {
                var w=c.equipment[0]; float scale=w.IsRanged?c.rangedDamage:c.meleeDamage;
                float damage=w.lightCombo.Sum(a=>w.damage*scale*a.damageMultiplier);
                float duration=ComboSeconds(w);
                float cost=w.lightCombo.Sum(a=>w.staminaCost*a.staminaMultiplier)+(w.IsRanged?w.reloadStaminaCost:0);
                float sustained=damage/(duration+c.staminaRegenerationDelay+cost/c.staminaRegeneration);
                float special=c.ability.kind==AbilityKind.PreciseShot?w.damage*scale*c.ability.shotMultiplier:c.ability.kind==AbilityKind.DevastatingBlow?c.ability.areaDamage*scale:0;
                var values=new[]{c.health,c.stamina,c.defense,c.walkSpeed,c.runSpeed,damage,duration,damage/duration,cost,sustained,w.IsRanged?0:w.damage*scale*w.heavyAttack.damageMultiplier,w.IsRanged?0:AttackSeconds(w,w.heavyAttack),c.dodge.duration,c.dodge.recovery,c.dodge.cost,(c.dodge.invulnerableEnd-c.dodge.invulnerableStart)*c.dodge.duration,special,c.ability.cooldown};
                csv.AppendLine(c.id+","+string.Join(",",values.Select(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture))));
            }
            File.WriteAllText("Assets/Game/Documentation/ClassBalanceCalculations.csv",csv.ToString());
            var pairs=new StringBuilder("Analise local teorica; nao sao partidas PvP nem taxas de vitoria.\nTTK abaixo: combo ciclico, 100% de acerto, sem bloqueio/esquiva, sem habilidade, sem deslocamento; stamina e pausas de regeneracao simuladas a 10 ms. Tempo de voo excluido.\n");
            for(int i=0;i<classes.Length;i++) for(int j=i+1;j<classes.Length;j++)
                pairs.AppendLine(classes[i].displayName+" vs "+classes[j].displayName+": TTK A->B="+SimulateTtk(classes[i],classes[j])+" s; B->A="+SimulateTtk(classes[j],classes[i])+" s");
            File.WriteAllText("Assets/Game/Documentation/ClassBalanceMatchups.txt",pairs.ToString());
        }
        public static float AttackSeconds(WeaponDefinition w,AttackConfiguration a) => (a.windup+a.execution+a.active+a.recovery)/w.attackSpeed+w.recoveryTime;
        public static float ComboSeconds(WeaponDefinition w) => w.lightCombo.Sum(a=>AttackSeconds(w,a))+(w.IsRanged?w.reloadSeconds:w.finisherRecovery);
        // Event-stepped local model: recovery only after actions and the regeneration delay.
        public static string SimulateTtk(GladiatorClassDefinition source,GladiatorClassDefinition target)
        {
            var w=source.equipment[0]; float hp=target.health,stamina=source.stamina,t=0;
            for(int cycle=0;cycle<1000;cycle++)
            {
                foreach(var a in w.lightCombo)
                {
                    float cost=w.staminaCost*a.staminaMultiplier+(w.IsRanged?w.reloadStaminaCost:0);
                    if(cost>source.stamina) return "unaffordable";
                    if(stamina<cost) { t+=source.staminaRegenerationDelay; while(stamina<cost) { stamina=Mathf.Min(source.stamina,stamina+source.staminaRegeneration*source.pvp.staminaRecovery*.01f); t+=.01f; } }
                    stamina-=cost;
                    float contact=(a.windup+a.execution)/w.attackSpeed;
                    hp-=w.damage*(w.IsRanged?source.rangedDamage:source.meleeDamage)*a.damageMultiplier*source.pvp.damage*(1-target.defense);
                    if(hp<=0) return (t+contact).ToString("F2",System.Globalization.CultureInfo.InvariantCulture);
                    t+=AttackSeconds(w,a)+(w.IsRanged?w.reloadSeconds:0);
                }
                if(!w.IsRanged) t+=w.finisherRecovery;
            }
            return "timeout";
        }
        [MenuItem("Bellator/Balance/Test All Four Classes in Play Mode")]
        public static void Run()
        { ExportCalculations(); BuildLab(); SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode(); }
        static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Near(float a,float b,string message) => Assert(Mathf.Abs(a-b)<.02f,message+": "+a+" != "+b);
        static void Pass(string value) { report.AppendLine("PASS: "+value); File.WriteAllText("Temp/BellatorBalance.progress.txt",report.ToString()); }
        static IEnumerator Checks()
        {
            yield return null; yield return null;
            var lab=UnityEngine.Object.FindAnyObjectByType<BalanceTrainingLab>(); Assert(lab!=null,"Lab missing");
            var player=lab.player; var motor=player.GetComponent<ThirdPersonMotor>(); var combat=player.GetComponent<PlayerCombat>();
            var ability=player.GetComponent<PlayerAbility>(); var health=player.GetComponent<CharacterHealth>(); var stamina=player.GetComponent<CharacterStamina>();
            var guard=player.GetComponent<ShieldGuard>(); var posture=player.GetComponent<ActorPosture>(); var receiver=player.GetComponent<DamageReceiver>();
            motor.AcceptInput=false; combat.ManualSimulation=true; ability.ManualSimulation=true; stamina.enabled=false;
            var spawn=player.transform.position;
            var trials=new StringBuilder("Short real Play Mode AI encounters. Six seconds maximum per encounter; simple scripted approach, light/heavy attacks, reactive dodge and Q attempt. Not optimal play, not win rates, not a full campaign.\n");
            foreach(var c in player.Catalog.classes)
            {
                Camera.main.transform.rotation=Quaternion.identity;
                player.Apply(c); motor.Teleport(spawn,Quaternion.identity); combat.TickCombat(0);
                for(int n=0;n<30;n++) motor.ProcessMovement(Vector2.zero,false,false,.02f);
                Near(health.Maximum,c.health,c.id+" HP"); Near(stamina.Maximum,c.stamina,c.id+" stamina"); Near(motor.RunSpeed,c.runSpeed,"Run speed");
                Assert(c.staminaRegeneration==(c.id==GladiatorClassId.Warrior?24:c.id==GladiatorClassId.Colossus?20:c.id==GladiatorClassId.Survivor?23:21),"Serialized class regeneration missing");
                var start=player.transform.position;
                for(int n=0;n<100;n++) motor.ProcessMovement(Vector2.up,false,false,.02f);
                Assert(Vector3.Distance(start,player.transform.position)>c.walkSpeed*1.5f,c.id+" walk displacement");
                motor.Teleport(spawn,Quaternion.identity); for(int n=0;n<20;n++) motor.ProcessMovement(Vector2.zero,false,false,.02f);
                stamina.Restore(); int rolls=motor.DodgeSequence;
                motor.ProcessMovement(Vector2.up,false,true,.02f); Assert(motor.DodgeSequence==rolls+1,c.id+" roll accepted");
                Near(stamina.Current,c.stamina-c.dodge.cost,"Roll cost");
                float iframeTime=0; int frames=0;
                while(motor.IsDodging && frames++<100)
                {
                    if(motor.IsInvulnerable)
                    {
                        iframeTime+=.01f; float before=health.Current;
                        Assert(!receiver.Receive(new DamageInfo(10,player.transform.position,player.transform.position+Vector3.forward,null)),"I-frame damage accepted"); Near(health.Current,before,"I-frame HP");
                    }
                    motor.ProcessMovement(Vector2.up,false,true,.01f);
                }
                Assert(motor.DodgeSequence==rolls+1,"Repeated input bypassed roll recovery");
                Assert(iframeTime>.15f && iframeTime<.26f,c.id+" i-frame window "+iframeTime);
                float beforeDamage=health.Current; Assert(receiver.Receive(new DamageInfo(10,player.transform.position,player.transform.position+Vector3.forward,null)),"Damage refused after recovery");
                Near(beforeDamage-health.Current,10*(1-c.defense),"Defense");
                player.Apply(c); combat.TickCombat(0);
                Assert(combat.TryAttack(false),c.id+" light attack"); float expected=c.stamina-c.equipment[0].staminaCost*c.equipment[0].lightCombo[0].staminaMultiplier;
                Near(stamina.Current,expected,"Light stamina");
                for(int n=0;n<400;n++) combat.TickCombat(.01f);
                Assert(!combat.IsBusy,c.id+" attack/reload recovery");
                if(!c.equipment[0].IsRanged)
                { stamina.Restore(); Assert(combat.TryAttack(true),"Heavy accepted"); Near(stamina.Current,c.stamina-c.equipment[0].staminaCost*c.equipment[0].heavyAttack.staminaMultiplier,"Heavy stamina"); for(int n=0;n<250;n++) combat.TickCombat(.01f); }
                else { Assert(!combat.TryAttack(true),"Crossbow heavy unexpectedly exists"); Assert(combat.CrossbowLoaded,"Reload failed"); Near(stamina.Current,expected-c.equipment[0].reloadStaminaCost,"Reload stamina"); }
                player.Apply(c); combat.TickCombat(0); Assert(ability.TryActivate(),c.id+" ability accepted");
                Near(stamina.Current,c.stamina-c.ability.staminaCost,"Ability cost"); Assert(!ability.TryActivate(),"Cooldown bypass");
                for(int n=0;n<700;n++) { ability.TickAbility(.01f); combat.TickCombat(.01f); }
                Assert(!ability.IsActive && ability.CooldownRemaining>0,"Ability duration/cooldown");
                foreach(var projectile in UnityEngine.Object.FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(projectile.gameObject);
                player.Apply(c); combat.TickCombat(0); health.Restore(); stamina.Restore(); guard.Request(true);
                float beforeBlock=health.Current;
                receiver.Receive(new DamageInfo(12,player.transform.position,player.transform.position+Vector3.forward*2,null));
                float shieldScale=c.shield!=null?1-c.shield.shieldReduction:1;
                Near(beforeBlock-health.Current,12*(1-c.defense)*shieldScale,"Frontal block"); guard.ResetGuard();
                Assert(!CombatFaction.CanDamage(player.transform,receiver),"Player friendly fire enabled");
                for(int index=0;index<3;index++)
                {
                    player.Apply(c); combat.TickCombat(0); motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
                    for(int n=0;n<20;n++) motor.ProcessMovement(Vector2.zero,false,false,.02f);
                    var brain=lab.Spawn(index); yield return null;
                    var target=brain.GetComponent<DamageReceiver>(); brain.AutoThink=false; brain.GetComponent<EnemyNavigation>().Stop();
                    float hp=target.Health.Current; Assert(target.Receive(new DamageInfo(10,target.transform.position,player.transform.position,player.transform)),"PvE damage rejected"); Near(hp-target.Health.Current,10*c.pve.damage,"PvE rules");
                    target.Health.Restore();
                    brain.GetComponent<EnemyNavigation>().Warp(player.transform.position+Vector3.forward*(c.equipment[0].IsRanged?6:1.4f),Quaternion.Euler(0,180,0));
                    Camera.main.transform.SetPositionAndRotation(player.transform.position+Vector3.up*1.4f-Vector3.forward*3,Quaternion.identity);
                    Physics.SyncTransforms();
                    Assert(combat.TryAttack(false),"Physical attack start");
                    for(int n=0;n<150;n++)
                    {
                        combat.TickCombat(.01f);
                        foreach(var bolt in UnityEngine.Object.FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) bolt.Simulate(.01f);
                    }
                    Assert(target.Health.Current<target.Health.Maximum,c.id+" physical hit failed against "+brain.Definition.role);
                    if(c.equipment[0].IsRanged) Assert(c.equipment[0].reloadStaminaCost>0,"Hunter reload stamina missing");
                    brain.gameObject.SetActive(false); UnityEngine.Object.Destroy(brain.gameObject);
                }
                Pass(c.id+": HP/stamina, walk/run, light/heavy or crossbow reload, defense/block, finite roll/recovery and i-frames, ability duration/cost/cooldown, physical light hit against common/heavy/archer prefabs, PvP remains disabled.");
                // Frame-driven smoke encounters: real enemy AI, navigation, stamina recovery and physics.
                for(int index=0;index<3;index++)
                {
                    lab.ResetTrial(c); motor.Teleport(new Vector3(0,.08f,0),Quaternion.identity);
                    combat.ManualSimulation=true; ability.ManualSimulation=true; motor.AcceptInput=false; stamina.enabled=true;
                    Camera.main.GetComponent<ThirdPersonCamera>().enabled=false;
                    lab.passiveTargets=false; var enemy=lab.Spawn(index); yield return null; yield return null;
                    float began=Time.time; bool specialUsed=false;
                    while(Time.time-began<6 && health.IsAlive && enemy.GetComponent<CharacterHealth>().IsAlive)
                    {
                        float dt=Mathf.Min(Time.deltaTime,.1f);
                        Vector3 delta=Vector3.ProjectOnPlane(enemy.transform.position-player.transform.position,Vector3.up);
                        float distance=delta.magnitude;
                        var camera=Camera.main; camera.transform.position=player.transform.position+Vector3.up*1.4f-Vector3.forward*3;
                        camera.transform.LookAt(PlayerLockOn.AimPoint(enemy.GetComponent<DamageReceiver>()));
                        Vector3 forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
                        Vector3 direction=distance>.001f?delta/distance:Vector3.zero;
                        if(c.equipment[0].IsRanged) direction*=distance<5?-1:distance>9?1:0;
                        else if(distance<1.25f) direction=Vector3.zero;
                        Vector2 axis=new Vector2(Vector3.Dot(direction,Vector3.Cross(Vector3.up,forward)),Vector3.Dot(direction,forward));
                        bool dodge=enemy.State==EnemyState.Attack && enemy.AttackProgress>.45f && enemy.AttackProgress<.7f;
                        motor.ProcessMovement(axis,false,dodge,dt);
                        if(!specialUsed && Time.time-began>2) specialUsed=ability.TryActivate();
                        if(!combat.IsBusy && (c.equipment[0].IsRanged || distance<c.equipment[0].reach+.2f)) combat.TryAttack(!c.equipment[0].IsRanged && combat.AttackSequence%4==3);
                        ability.TickAbility(dt); combat.TickCombat(dt);
                        yield return null;
                    }
                    Assert(enemy.AttacksStarted>0 || !enemy.GetComponent<CharacterHealth>().IsAlive,c.id+" AI never engaged "+enemy.Definition.role);
                    trials.AppendLine(c.id+" vs "+enemy.Definition.role+" | enemyHP="+enemy.GetComponent<CharacterHealth>().Current.ToString("F1")+" | enemyAttacks="+enemy.AttacksStarted+"\n"+lab.Metrics()+"\n");
                    File.WriteAllText("Assets/Game/Documentation/ClassBalanceAITrials.txt",trials.ToString());
                    enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject);
                    foreach(var projectile in UnityEngine.Object.FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(projectile.gameObject);
                    yield return null;
                }
                lab.passiveTargets=true; stamina.enabled=false;
                Pass(c.id+": real AI encounter smoke checks completed against common, heavy and archer; measurements saved separately.");
                yield return null;
            }
            Assert(errors.Count==0,string.Join("\n",errors));
            Pass("All four classes use independent assets; theoretical DPS and six matchups exported. These checks do not represent full AI duels or PvP matches.");
        }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log; routine=null; SessionState.SetBool(Active,false);
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Temp/BellatorBalance.test-result.txt",report.ToString()); File.WriteAllText("Assets/Game/Documentation/ClassBalanceValidation.txt",report.ToString());
            if(error!=null) Debug.LogException(error); EditorApplication.ExitPlaymode();
        }
    }
}
