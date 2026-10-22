using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class CombatStageBuilder
    {
        private const string Request="Temp/BellatorCombat.install";
        private static Material iron,bronze,timber,linen;
        static CombatStageBuilder() { EditorApplication.delayCall+=CheckRequest; }
        private static void CheckRequest()
        {
            if(!File.Exists(Request) || EditorApplication.isCompiling) return;
            File.Delete(Request);
            try { Install(); } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorCombat.error.txt",e.ToString()); }
        }
        [MenuItem("Bellator/Install Stage 3 Combat")]
        public static void Install()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing.");
            Scene scene=SceneManager.GetSceneByPath(ArenaBuilder.ScenePath);
            if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(ArenaBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var player=scene.GetRootGameObjects().First(g=>g.GetComponent<ThirdPersonMotor>()!=null);
            if(player.GetComponent<PlayerCombat>()!=null) throw new InvalidOperationException("Combat already installed; existing components preserved.");
            foreach(string folder in new[]{"Scripts/Combat","Prefabs/Weapons","Prefabs/Projectiles","Prefabs/Training","Materials/Combat","ScriptableObjects/Weapons"})
                Directory.CreateDirectory("Assets/Game/"+folder);
            AssetDatabase.Refresh();
            iron=Mat("Iron",new Color(.42f,.47f,.5f),.7f);
            bronze=Mat("Bronze",new Color(.55f,.34f,.1f),.6f);
            timber=Mat("Timber",new Color(.32f,.16f,.065f),0);
            linen=Mat("TrainingCloth",new Color(.66f,.53f,.31f),0);
            var arrow=Projectile("Arrow",false); var bolt=Projectile("Bolt",true);
            var shortSword=Definition("ShortSword","Gládio",EquipmentType.ShortSword,24,1.8f,14,1.15f);
            shortSword.permitsShield=true; shortSword.mountEuler=new Vector3(90,0,0);
            shortSword.lightCombo=new[]{
                new AttackConfiguration{name="Corte I",windup=.16f,active=.14f,recovery=.23f,arc=100},
                new AttackConfiguration{name="Corte II",damageMultiplier=1.1f,windup=.18f,active=.14f,recovery=.23f,arc=110},
                new AttackConfiguration{name="Finalização",damageMultiplier=1.4f,staminaMultiplier=1.2f,windup=.26f,active=.16f,recovery=.32f,arc=90}};
            shortSword.visualPrefab=WeaponVisual("ShortSword",EquipmentType.ShortSword);
            var heavySword=Definition("HeavySword","Espada pesada",EquipmentType.HeavySword,38,2.45f,23,.8f);
            heavySword.mountEuler=new Vector3(90,0,0);
            heavySword.lightCombo=new[]{new AttackConfiguration{name="Corte amplo",windup=.28f,active=.2f,recovery=.32f,arc=130},new AttackConfiguration{name="Corte de retorno",damageMultiplier=1.25f,windup=.3f,active=.2f,recovery=.4f,arc=125}};
            heavySword.heavyAttack=new AttackConfiguration{name="Golpe esmagador",damageMultiplier=1.9f,staminaMultiplier=1.7f,windup=.5f,active=.22f,recovery=.5f,arc=90};
            heavySword.visualPrefab=WeaponVisual("HeavySword",EquipmentType.HeavySword);
            var bow=Definition("Bow","Arco",EquipmentType.Bow,27,60,16,1);
            bow.leftHandMount=true; bow.mountEuler=new Vector3(80,0,0); bow.projectilePrefab=arrow; bow.projectileSpeed=34;
            bow.lightCombo=new[]{new AttackConfiguration{name="Puxar e soltar",windup=.4f,active=.06f,recovery=.35f}};
            bow.visualPrefab=WeaponVisual("Bow",EquipmentType.Bow);
            var crossbow=Definition("Crossbow","Besta",EquipmentType.Crossbow,42,85,12,1);
            crossbow.mountEuler=new Vector3(85,0,0); crossbow.projectilePrefab=bolt; crossbow.projectileSpeed=60; crossbow.projectileGravity=3.5f; crossbow.reloadSeconds=1.6f;
            crossbow.lightCombo=new[]{new AttackConfiguration{name="Disparo",windup=.14f,active=.04f,recovery=.26f}};
            crossbow.visualPrefab=WeaponVisual("Crossbow",EquipmentType.Crossbow);
            var shield=Definition("Shield","Escudo de treino",EquipmentType.Shield,0,1,0,1);
            shield.mountEuler=new Vector3(80,0,0); shield.mountOffset=new Vector3(0,-.05f,.1f); shield.visualPrefab=WeaponVisual("Shield",EquipmentType.Shield);
            foreach(var definition in new[]{shortSword,heavySword,bow,crossbow,shield}) EditorUtility.SetDirty(definition);

            Transform body=player.transform.Find("VisualRoot_Replaceable/BodyPivot");
            Transform left=body.Find("LeftShoulder"),right=body.Find("RightShoulder");
            var rightSocket=Parent("RightHandSocket",right); rightSocket.localPosition=new Vector3(0,-.65f,0);
            var leftSocket=Parent("LeftHandSocket",left); leftSocket.localPosition=new Vector3(0,-.65f,0);
            var equipment=player.AddComponent<WeaponEquipment>(); equipment.Configure(rightSocket,leftSocket,new[]{shortSword,heavySword,bow,crossbow},shield);
            player.AddComponent<CombatInput>(); player.AddComponent<ShieldGuard>(); player.AddComponent<DamageReceiver>(); player.AddComponent<ImpactFeedback>();
            var combat=player.AddComponent<PlayerCombat>();
            var camera=scene.GetRootGameObjects().First(g=>g.name=="PlayerCamera").GetComponent<Camera>();
            combat.Configure(camera);
            player.AddComponent<CombatPoseAnimator>().Configure(body,left,right);
            PrefabUtility.ApplyPrefabInstance(player,InteractionMode.AutomatedAction);

            var training=new GameObject("Training");
            var dummyPrefab=Dummy();
            var positions=new[]{new Vector3(0,.04f,-9),new Vector3(-5,.04f,-9),new Vector3(5,.04f,-3),new Vector3(-5,.04f,4)};
            for(int i=0;i<positions.Length;i++)
            {
                var dummy=(GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab,scene);
                dummy.name="TrainingDummy_"+(i+1); dummy.transform.SetParent(training.transform,false);
                dummy.transform.position=positions[i]; dummy.transform.rotation=Quaternion.Euler(0,180,0);
            }
            training.AddComponent<TrainingArenaControls>().Configure(combat);
            training.AddComponent<CombatHUD>().Configure(combat);
            Physics.SyncTransforms();
            VerifyReferences(player,training);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            File.WriteAllText("Temp/BellatorCombat.installed.txt","Stage 3 combat saved; references verified; Unity "+Application.unityVersion);
            Debug.Log("Bellator Stage 3 combat installed.");
        }
        private static void VerifyReferences(GameObject player,GameObject training)
        {
            foreach(var g in player.GetComponentsInChildren<Transform>(true).Select(t=>t.gameObject).Concat(training.GetComponentsInChildren<Transform>(true).Select(t=>t.gameObject)))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g)>0) throw new InvalidOperationException("Missing script: "+g.name);
            var equipment=player.GetComponent<WeaponEquipment>();
            if(equipment.Loadout.Length!=4 || equipment.Loadout.Any(w=>w==null || w.visualPrefab==null || (w.IsRanged && w.projectilePrefab==null)))
                throw new InvalidOperationException("Missing weapon references.");
        }
        private static WeaponDefinition Definition(string file,string label,EquipmentType type,float damage,float reach,float cost,float speed)
        {
            string path="Assets/Game/ScriptableObjects/Weapons/"+file+".asset";
            var w=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if(w==null) { w=ScriptableObject.CreateInstance<WeaponDefinition>(); AssetDatabase.CreateAsset(w,path); }
            w.displayName=label; w.type=type; w.damage=damage; w.reach=reach; w.staminaCost=cost; w.attackSpeed=speed; return w;
        }
        private static Material Mat(string name,Color color,float metallic)
        {
            string path="Assets/Game/Materials/Combat/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m!=null) return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name}; m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",metallic>0 ? 0.35f:.1f);
            AssetDatabase.CreateAsset(m,path); return m;
        }
        private static Transform Parent(string name,Transform parent)
        { var t=new GameObject(name).transform; t.SetParent(parent,false); return t; }
        private static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material material,Vector3 rotation=default)
        {
            var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=p; g.transform.localScale=scale; g.transform.localRotation=Quaternion.Euler(rotation);
            g.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        private static void Beam(string name,Transform parent,Vector3 from,Vector3 to,float width,Material material)
        {
            var g=Shape(name,PrimitiveType.Cube,parent,(from+to)*.5f,new Vector3(width,(to-from).magnitude,width),material);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
        }
        private static GameObject Save(GameObject root,string path)
        { var prefab=PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root); return prefab; }
        private static GameObject WeaponVisual(string name,EquipmentType type)
        {
            string path="Assets/Game/Prefabs/Weapons/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var root=new GameObject(name); var visuals=Parent("Visuals_Replaceable",root.transform);
            if(type==EquipmentType.ShortSword || type==EquipmentType.HeavySword)
            {
                bool heavy=type==EquipmentType.HeavySword; float length=heavy?1.3f:.85f;
                Shape("Grip",PrimitiveType.Cylinder,visuals,new Vector3(0,0,.03f),new Vector3(.08f,.14f,.08f),timber,new Vector3(90,0,0));
                Shape("Pommel",PrimitiveType.Sphere,visuals,new Vector3(0,0,-.13f),Vector3.one*.12f,bronze);
                Shape("Guard",PrimitiveType.Cube,visuals,new Vector3(0,0,.19f),new Vector3(heavy ? 0.43f:.28f,.07f,.07f),bronze);
                Shape("Blade",PrimitiveType.Cube,visuals,new Vector3(0,0,.23f+length*.5f),new Vector3(heavy ? 0.16f:.105f,.045f,length),iron);
                Shape("BladeTip",PrimitiveType.Cube,visuals,new Vector3(0,0,.23f+length),new Vector3(.08f,.035f,.1f),iron,new Vector3(0,0,45));
            }
            else if(type==EquipmentType.Shield)
            {
                Shape("TimberFace",PrimitiveType.Cylinder,visuals,Vector3.zero,new Vector3(.95f,.05f,.95f),timber,new Vector3(90,0,0));
                Shape("IronFace",PrimitiveType.Cylinder,visuals,new Vector3(0,0,.055f),new Vector3(.82f,.012f,.82f),iron,new Vector3(90,0,0));
                Shape("Boss",PrimitiveType.Sphere,visuals,new Vector3(0,0,.08f),new Vector3(.25f,.25f,.18f),bronze);
                Shape("Grip",PrimitiveType.Cube,visuals,new Vector3(0,0,-.09f),new Vector3(.12f,.34f,.1f),timber);
            }
            else if(type==EquipmentType.Bow)
            {
                Vector3 previous=new Vector3(0,-.72f,-.14f);
                for(int i=1;i<=8;i++) { float y=-.72f+i*.18f; var next=new Vector3(0,y,.22f*(1-y*y/(.72f*.72f))-.14f); Beam("Limb_"+i,visuals,previous,next,.065f,timber); previous=next; }
                Beam("String",visuals,new Vector3(0,-.72f,-.14f),new Vector3(0,.72f,-.14f),.013f,linen);
                Shape("Grip",PrimitiveType.Cube,visuals,new Vector3(0,0,.07f),new Vector3(.1f,.25f,.1f),bronze);
            }
            else
            {
                Shape("Stock",PrimitiveType.Cube,visuals,new Vector3(0,0,.2f),new Vector3(.12f,.14f,.9f),timber);
                Shape("Grip",PrimitiveType.Cube,visuals,new Vector3(0,-.13f,-.04f),new Vector3(.1f,.3f,.13f),timber);
                Beam("LeftLimb",visuals,new Vector3(-.55f,0,.48f),new Vector3(0,0,.68f),.075f,iron);
                Beam("RightLimb",visuals,new Vector3(0,0,.68f),new Vector3(.55f,0,.48f),.075f,iron);
                Beam("StringLeft",visuals,new Vector3(-.55f,0,.48f),new Vector3(0,0,0),.014f,linen);
                Beam("StringRight",visuals,new Vector3(0,0,0),new Vector3(.55f,0,.48f),.014f,linen);
            }
            if(type==EquipmentType.Bow || type==EquipmentType.Crossbow) Parent("Muzzle",root.transform).localPosition=new Vector3(0,0,type==EquipmentType.Bow ? 0.4f:.82f);
            return Save(root,path);
        }
        private static GameObject Projectile(string name,bool bolt)
        {
            string path="Assets/Game/Prefabs/Projectiles/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var root=new GameObject(name); var visual=Parent("Visuals_Replaceable",root.transform);
            float length=bolt ? 0.45f:.8f;
            Shape("Shaft",PrimitiveType.Cylinder,visual,new Vector3(0,0,-length*.5f),new Vector3(.025f,length*.5f,.025f),timber,new Vector3(90,0,0));
            Shape("Tip",PrimitiveType.Cube,visual,new Vector3(0,0,.025f),new Vector3(.065f,.045f,.12f),iron);
            Shape("FeatherA",PrimitiveType.Cube,visual,new Vector3(0,0,-length*.85f),new Vector3(.16f,.016f,.14f),linen);
            Shape("FeatherB",PrimitiveType.Cube,visual,new Vector3(0,0,-length*.85f),new Vector3(.016f,.16f,.14f),linen);
            root.AddComponent<CombatProjectile>(); return Save(root,path);
        }
        private static GameObject Dummy()
        {
            const string path="Assets/Game/Prefabs/Training/TrainingDummy.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var root=new GameObject("TrainingDummy");
            root.AddComponent<CharacterHealth>(); root.AddComponent<DamageReceiver>();
            var solid=root.AddComponent<CapsuleCollider>(); solid.center=new Vector3(0,1.05f,0); solid.height=2.1f; solid.radius=.42f;
            var visual=Parent("Visuals_Replaceable",root.transform);
            Shape("Post",PrimitiveType.Cylinder,visual,new Vector3(0,.55f,0),new Vector3(.22f,.55f,.22f),timber);
            Shape("Torso",PrimitiveType.Capsule,visual,new Vector3(0,1.3f,0),new Vector3(.65f,.4f,.42f),linen);
            var head=Shape("Head",PrimitiveType.Sphere,visual,new Vector3(0,1.95f,0),Vector3.one*.4f,timber); head.AddComponent<SphereCollider>();
            Shape("ArmLeft",PrimitiveType.Capsule,visual,new Vector3(-.55f,1.45f,0),new Vector3(.18f,.35f,.18f),timber,new Vector3(0,0,80));
            Shape("ArmRight",PrimitiveType.Capsule,visual,new Vector3(.55f,1.45f,0),new Vector3(.18f,.35f,.18f),timber,new Vector3(0,0,-80));
            Shape("TargetPlate",PrimitiveType.Cylinder,visual,new Vector3(0,1.35f,.23f),new Vector3(.35f,.025f,.35f),bronze,new Vector3(90,0,0));
            Shape("Base",PrimitiveType.Cylinder,root.transform,new Vector3(0,.07f,0),new Vector3(1.1f,.07f,1.1f),timber);
            root.AddComponent<ImpactFeedback>().Configure(visual);
            var obstacle=root.AddComponent<UnityEngine.AI.NavMeshObstacle>(); obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Capsule; obstacle.center=new Vector3(0,1,0); obstacle.radius=.5f; obstacle.height=2; obstacle.carving=true;
            return Save(root,path);
        }
    }
}


