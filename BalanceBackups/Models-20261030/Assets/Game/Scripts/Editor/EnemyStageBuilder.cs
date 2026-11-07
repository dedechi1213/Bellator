using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class EnemyStageBuilder
    {
        static EnemyStageBuilder() { EditorApplication.delayCall+=InstallRequested; }
        private static void InstallRequested()
        {
            const string request="Temp/BellatorEnemies.install";
            if(!File.Exists(request)) return;
            File.Delete(request);
            try { Install(); } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorEnemies.error.txt",e.ToString()); }
        }
        [MenuItem("Bellator/Install Stage 4 Enemies")]
        public static void Install()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing.");
            var scene=SceneManager.GetSceneByPath(ArenaBuilder.ScenePath);
            if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(ArenaBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            if(scene.GetRootGameObjects().Any(g=>g.GetComponent<EnemyEncounter>()!=null))
                throw new InvalidOperationException("Initial enemies already installed; preserved.");
            foreach(string folder in new[]{"Prefabs/Enemies","Materials/Enemies","ScriptableObjects/Enemies"})
                Directory.CreateDirectory("Assets/Game/"+folder);
            AssetDatabase.Refresh();
            var player=scene.GetRootGameObjects().First(g=>g.GetComponent<PlayerCombat>()!=null);
            var faction=player.GetComponent<CombatFaction>(); if(faction==null) faction=player.AddComponent<CombatFaction>();
            faction.Configure(FactionTeam.Player);
            PrefabUtility.ApplyPrefabInstance(player,InteractionMode.AutomatedAction);
            var common=Definition("Gladiator","Gladiador comum",EnemyRole.Gladiator,85,3.1f,.4f,2.1f,CloneWeapon("CommonSword","ShortSword",12,12,.95f,.32f,.14f,.25f));
            common.shield=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/ScriptableObjects/Weapons/Shield.asset");
            var heavy=Definition("HeavyGladiator","Gladiador pesado",EnemyRole.Heavy,160,2.05f,.54f,2.5f,CloneWeapon("HeavySword","HeavySword",27,23,.8f,.55f,.22f,.45f));
            heavy.engageDistance=2.25f; heavy.attackPause=.95f; heavy.stunSeconds=.14f; heavy.defenseChance=0;
            var archer=Definition("Archer","Arqueiro",EnemyRole.Archer,55,3.6f,.32f,2.1f,CloneWeapon("ArcherBow","Bow",11,14,1,.65f,.06f,.45f));
            archer.detectionRange=36; archer.preferredRange=10; archer.retreatRange=6; archer.attackPause=1; archer.stunSeconds=.35f; archer.defenseChance=0;
            var prefabs=new[]{
                CreateEnemy(common,player,Mat("GladiatorCrimson",new Color(.4f,.055f,.035f)),Vector3.one,42),
                CreateEnemy(heavy,player,Mat("HeavyIron",new Color(.23f,.26f,.29f)),new Vector3(1.28f,1.18f,1.28f),52),
                CreateEnemy(archer,player,Mat("ArcherOlive",new Color(.24f,.30f,.12f)),new Vector3(.8f,1,.8f),32)};
            var root=new GameObject("Enemies"); root.AddComponent<EnemyEncounter>();
            var positions=new[]{new Vector3(-13,.08f,5),new Vector3(12,.08f,7),new Vector3(2,.08f,17)};
            for(int i=0;i<prefabs.Length;i++)
            {
                var enemy=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);
                enemy.transform.SetParent(root.transform,false);
                if(!NavMesh.SamplePosition(positions[i],out var point,2,NavMesh.AllAreas)) throw new InvalidOperationException("Enemy spawn outside NavMesh.");
                enemy.transform.position=point.position+Vector3.up*.04f;
                enemy.transform.LookAt(new Vector3(player.transform.position.x,enemy.transform.position.y,player.transform.position.z));
                enemy.GetComponent<EnemyBrain>().Configure(new[]{common,heavy,archer}[i],player.GetComponent<DamageReceiver>());
                if(Physics.CheckCapsule(enemy.transform.position+Vector3.up*.65f,enemy.transform.position+Vector3.up*1.5f,.28f,~0,QueryTriggerInteraction.Ignore))
                {
                    // Check only other geometry; the actor's own controller is expected here.
                    bool blocked=Physics.OverlapCapsule(enemy.transform.position+Vector3.up*.65f,enemy.transform.position+Vector3.up*1.5f,.28f)
                        .Any(c=>!c.transform.IsChildOf(enemy.transform));
                    if(blocked) throw new InvalidOperationException("Enemy spawn obstructed: "+enemy.name);
                }
            }
            foreach(var definition in new[]{common,heavy,archer}) EditorUtility.SetDirty(definition);
            Physics.SyncTransforms();
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing enemy script.");
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            File.WriteAllText("Temp/BellatorEnemies.installed.txt","Three enemies installed; NavMesh spawns, prefabs and references verified. Unity "+Application.unityVersion);
            Debug.Log("Bellator Stage 4 installed: common gladiator, heavy gladiator and archer.");
        }
        private static WeaponDefinition CloneWeapon(string name,string source,float damage,float stamina,float speed,float windup,float active,float recovery)
        {
            string path="Assets/Game/ScriptableObjects/Enemies/"+name+".asset";
            var weapon=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if(weapon!=null) return weapon;
            weapon=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/ScriptableObjects/Weapons/"+source+".asset"));
            weapon.name=name; weapon.damage=damage; weapon.staminaCost=stamina; weapon.attackSpeed=speed;
            weapon.lightCombo=new[]{new AttackConfiguration{name="Ataque",windup=windup,active=active,recovery=recovery,arc=100}};
            AssetDatabase.CreateAsset(weapon,path); return weapon;
        }
        private static EnemyDefinition Definition(string file,string name,EnemyRole role,float hp,float speed,float radius,float height,WeaponDefinition weapon)
        {
            string path="Assets/Game/ScriptableObjects/Enemies/"+file+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path); if(definition!=null) return definition;
            definition=ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.displayName=name; definition.role=role; definition.maximumHealth=hp; definition.moveSpeed=speed; definition.radius=radius; definition.height=height; definition.weapon=weapon;
            AssetDatabase.CreateAsset(definition,path); return definition;
        }
        private static Material Mat(string name,Color color)
        {
            string path="Assets/Game/Materials/Enemies/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path); if(material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",.15f); AssetDatabase.CreateAsset(material,path); return material;
        }
        private static GameObject CreateEnemy(EnemyDefinition definition,GameObject player,Material material,Vector3 scale,int priority)
        {
            string path="Assets/Game/Prefabs/Enemies/"+definition.role+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var root=new GameObject(definition.displayName);
            var visual=UnityEngine.Object.Instantiate(player.transform.Find("VisualRoot_Replaceable").gameObject,root.transform,false);
            visual.name="VisualRoot_Replaceable"; visual.transform.localScale=scale;
            foreach(var animator in visual.GetComponentsInChildren<PrimitiveHumanoidAnimator>(true)) UnityEngine.Object.DestroyImmediate(animator);
            var body=visual.transform.Find("BodyPivot");
            body.Find("Torso").GetComponent<Renderer>().sharedMaterial=material;
            var left=body.Find("LeftShoulder"); var right=body.Find("RightShoulder");
            var controller=root.AddComponent<CharacterController>(); controller.height=definition.height; controller.radius=definition.radius;
            controller.center=Vector3.up*(definition.height*.5f); controller.skinWidth=.035f; controller.stepOffset=.28f; controller.slopeLimit=45; controller.minMoveDistance=0;
            var agent=root.AddComponent<NavMeshAgent>(); agent.agentTypeID=0; agent.radius=definition.radius+.06f; agent.height=definition.height;
            agent.speed=definition.moveSpeed; agent.acceleration=14; agent.angularSpeed=360; agent.stoppingDistance=.1f;
            agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance; agent.avoidancePriority=priority;
            agent.autoRepath=true; agent.autoTraverseOffMeshLink=false;
            var health=root.AddComponent<CharacterHealth>(); var serialized=new SerializedObject(health); serialized.FindProperty("maximum").floatValue=definition.maximumHealth; serialized.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<CharacterStamina>();
            root.AddComponent<CombatFaction>().Configure(FactionTeam.Gladiators);
            root.AddComponent<WeaponEquipment>().Configure(right.Find("RightHandSocket"),left.Find("LeftHandSocket"),new[]{definition.weapon},definition.shield);
            root.AddComponent<ShieldGuard>(); root.AddComponent<DamageReceiver>(); root.AddComponent<ImpactFeedback>();
            root.AddComponent<EnemyNavigation>(); root.AddComponent<EnemyBrain>().Configure(definition);
            root.AddComponent<EnemyPoseAnimator>().Configure(body,left,right,body.Find("LeftHip"),body.Find("RightHip"));
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }
    }
}

