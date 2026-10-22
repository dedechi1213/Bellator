using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class ClassStageBuilder
    {
        static ClassStageBuilder() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(File.Exists("Temp/BellatorClasses.stop")) { File.Delete("Temp/BellatorClasses.stop"); EditorApplication.ExitPlaymode(); return; }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(File.Exists("Temp/BellatorClasses.install"))
            {
                File.Delete("Temp/BellatorClasses.install");
                try { Install(); } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorClasses.error.txt",e.ToString()); }
            }
            if(File.Exists("Temp/BellatorClasses.preview")) { File.Delete("Temp/BellatorClasses.preview"); RepairPreviews(); }
            if(File.Exists("Temp/BellatorClasses.open-selection")) { File.Delete("Temp/BellatorClasses.open-selection"); EditorSceneManager.OpenScene(PlayerClassController.SelectionScene); }
        }
        [MenuItem("Bellator/Install Stage 5 Classes")]
        public static void Install()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerClassController.SelectionScene)!=null) throw new InvalidOperationException("Selection already installed; preserved.");
            var scene=SceneManager.GetSceneByPath(ArenaBuilder.ScenePath);
            if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(ArenaBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach(string folder in new[]{"Prefabs/Classes","Materials/Classes","ScriptableObjects/Classes","ScriptableObjects/Abilities","UI"}) Directory.CreateDirectory("Assets/Game/"+folder);
            AssetDatabase.Refresh();
            var player=scene.GetRootGameObjects().First(g=>g.GetComponent<PlayerCombat>()!=null);
            var legacy=player.transform.Find("VisualRoot_Replaceable");
            var old=player.GetComponent<WeaponEquipment>();
            var loadout=old.Loadout.ToArray(); var shield=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/ScriptableObjects/Weapons/Shield.asset");
            var catalog=ScriptableObject.CreateInstance<GladiatorClassCatalog>(); catalog.classes=new GladiatorClassDefinition[4]; catalog.defaultClass=2;
            string[] ids={"Survivor","Colossus","Warrior","Hunter"}; string[] names={"Sobrevivente","Colosso","Guerreiro","Caçador"};
            string[] desc={"Magro e resistente. Sobrevive usando agilidade e esquivas.","Grande e poderoso. Avança com espada pesada e escudo.","Equilibrado e disciplinado. Combina ataque e defesa.","Leve e ágil. Elimina ameaças à distância com sua besta."};
            Color[] colors={new Color(.66f,.42f,.13f),new Color(.55f,.12f,.08f),new Color(.12f,.32f,.58f),new Color(.18f,.42f,.24f)};
            Vector3[] scales={new Vector3(.68f,.95f,.68f),new Vector3(1.35f,1.25f,1.35f),Vector3.one,new Vector3(.82f,.96f,.82f)};
            float[] hp={70,180,120,80},st={120,80,100,110},walk={4.3f,2.4f,3.5f,4},run={7.5f,4.1f,6.2f,7},melee={.75f,1.5f,1,.85f},ranged={.8f,.8f,1,1.45f},defense={.05f,.3f,.18f,.07f};
            for(int i=0;i<4;i++)
            {
                var c=ScriptableObject.CreateInstance<GladiatorClassDefinition>(); c.id=(GladiatorClassId)i; c.displayName=names[i]; c.description=desc[i];
                c.health=hp[i]; c.stamina=st[i]; c.walkSpeed=walk[i]; c.runSpeed=run[i]; c.meleeDamage=melee[i]; c.rangedDamage=ranged[i]; c.defense=defense[i]; c.color=colors[i];
                c.colliderHeight=2.1f*scales[i].y; c.colliderRadius=Mathf.Max(.24f,.34f*scales[i].x);
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=ids[i]}; material.SetColor("_BaseColor",colors[i]); material.SetFloat("_Smoothness",.15f);
                AssetDatabase.CreateAsset(material,"Assets/Game/Materials/Classes/"+ids[i]+".mat");
                var visual=UnityEngine.Object.Instantiate(legacy.gameObject); visual.name=ids[i]+"_ReplaceableVisual"; visual.transform.localScale=scales[i];
                foreach(var animator in visual.GetComponentsInChildren<PrimitiveHumanoidAnimator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                visual.transform.Find("BodyPivot/Torso").GetComponent<Renderer>().sharedMaterial=material;
                c.visualPrefab=PrefabUtility.SaveAsPrefabAsset(visual,"Assets/Game/Prefabs/Classes/"+ids[i]+"Visual.prefab"); UnityEngine.Object.DestroyImmediate(visual);
                var primary=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Game/ScriptableObjects/Weapons/"+(i==1?"HeavySword":i==3?"Crossbow":"ShortSword")+".asset"));
                primary.name=ids[i]+"Weapon"; primary.permitsShield=i==1 || i==2;
                AssetDatabase.CreateAsset(primary,"Assets/Game/ScriptableObjects/Classes/"+ids[i]+"Weapon.asset");
                c.equipment=new[]{primary}; c.shield=i==1 || i==2?shield:null;
                c.ability=Ability(i); AssetDatabase.CreateAsset(c,"Assets/Game/ScriptableObjects/Classes/"+ids[i]+".asset"); catalog.classes[i]=c;
            }
            AssetDatabase.CreateAsset(catalog,"Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            player.AddComponent<ActorAttributes>(); player.AddComponent<PlayerAbility>(); player.AddComponent<AbilityPoseOverlay>();
            player.AddComponent<PlayerClassController>().Configure(catalog,legacy,loadout,shield);
            var input=new SerializedObject(player.GetComponent<CombatInput>()); input.FindProperty("heavyAttackBinding").stringValue="<Keyboard>/e"; input.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.ApplyPrefabInstance(player,InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            BuildSelection(catalog);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(PlayerClassController.SelectionScene,true),new EditorBuildSettingsScene(PlayerClassController.ArenaScene,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=PlayerClassController.SelectionScene && s.path!=PlayerClassController.ArenaScene)).ToArray();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            File.WriteAllText("Temp/BellatorClasses.installed.txt","Four classes, abilities, visual prefabs, selection and arena installed. Unity "+Application.unityVersion);
        }
        static AbilityDefinition Ability(int i)
        {
            var a=ScriptableObject.CreateInstance<AbilityDefinition>(); a.kind=(AbilityKind)i;
            a.displayName=new[]{"Passos Desesperados","Golpe Devastador","Postura de Combate","Disparo Preciso"}[i];
            a.description=new[]{"Por 5 s, corre mais rápido e esquiva mais longe gastando menos.","Golpe de área que causa dano e empurra inimigos próximos.","Por 6 s, aumenta dano e defesa e reduz o custo dos bloqueios.","Disparo de alto dano. Exige besta equipada e carregada."}[i];
            a.cooldown=new float[]{14,12,16,10}[i]; a.staminaCost=new float[]{24,40,25,30}[i]; a.duration=i==0?5:i==2?6:0;
            a.windup=i==1?.6f:.4f; a.recovery=i==1?.5f:.25f;
            a.feedbackColor=new[]{new Color(1,.72f,.1f),new Color(1,.25f,.05f),new Color(.15f,.6f,1),new Color(.2f,1,.5f)}[i];
            AssetDatabase.CreateAsset(a,"Assets/Game/ScriptableObjects/Abilities/"+a.kind+".asset"); return a;
        }
        static void BuildSelection(GladiatorClassCatalog catalog)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("SelectionCamera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.rect=new Rect(0,.48f,1,.40f);
            camera.transform.position=new Vector3(0,3,-15); camera.transform.LookAt(new Vector3(0,1.3f,0)); camera.orthographic=true; camera.orthographicSize=2.35f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.075f,.065f,.055f); camera.gameObject.AddComponent<AudioListener>();
            var sun=new GameObject("PreviewSun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.8f; sun.color=new Color(1,.85f,.65f); sun.transform.rotation=Quaternion.Euler(40,-30,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.4f,.4f,.42f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var previews=new GameObject("ClassPreviews"); var halos=new GameObject[4];
            var stone=new Material(Shader.Find("Universal Render Pipeline/Lit")); stone.SetColor("_BaseColor",new Color(.32f,.28f,.21f)); AssetDatabase.CreateAsset(stone,"Assets/Game/Materials/Classes/PreviewStone.mat");
            var gold=new Material(Shader.Find("Universal Render Pipeline/Unlit")); gold.SetColor("_BaseColor",new Color(.95f,.64f,.18f)); AssetDatabase.CreateAsset(gold,"Assets/Game/Materials/Classes/SelectionGold.mat");
            for(int i=0;i<4;i++)
            {
                var root=new GameObject("Preview_"+catalog.classes[i].id); root.transform.SetParent(previews.transform); root.transform.position=new Vector3((i-1.5f)*4,0,0); root.transform.rotation=Quaternion.Euler(0,180,0);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(catalog.classes[i].visualPrefab,scene); visual.transform.SetParent(root.transform,false); var body=visual.transform.Find("BodyPivot");
                var weapon=catalog.classes[i].equipment[0]; var w=Mount(weapon,body.Find(weapon.leftHandMount?"LeftShoulder/LeftHandSocket":"RightShoulder/RightHandSocket"));
                Transform shield=null; if(catalog.classes[i].shield!=null) shield=Mount(catalog.classes[i].shield,body.Find("LeftShoulder/LeftHandSocket")).transform;
                root.AddComponent<ClassPreviewPose>().Configure(body.Find("LeftShoulder"),body.Find("RightShoulder"),shield,weapon.type);
                var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pedestal.name="StonePedestal"; pedestal.transform.SetParent(previews.transform); pedestal.transform.position=new Vector3(root.transform.position.x,-.13f,0); pedestal.transform.localScale=new Vector3(2,.12f,2); pedestal.GetComponent<Renderer>().sharedMaterial=stone; UnityEngine.Object.DestroyImmediate(pedestal.GetComponent<Collider>());
                halos[i]=GameObject.CreatePrimitive(PrimitiveType.Cylinder); halos[i].name="SelectionHalo_"+i; halos[i].transform.SetParent(previews.transform); halos[i].transform.position=new Vector3(root.transform.position.x,-.265f,0); halos[i].transform.localScale=new Vector3(2.15f,.025f,2.15f); halos[i].GetComponent<Renderer>().sharedMaterial=gold; UnityEngine.Object.DestroyImmediate(halos[i].GetComponent<Collider>()); halos[i].SetActive(i==0);
            }
            var canvas=new GameObject("SelectionUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,800); scaler.matchWidthOrHeight=.5f;
            Panel(canvas.transform,"Header",new Vector2(0,.88f),Vector2.one,new Color(.13f,.10f,.075f));
            Label(canvas.transform,"Title","ESCOLHA SEU GLADIADOR",new Vector2(.02f,.93f),new Vector2(.98f,.995f),28,TextAnchor.MiddleCenter,new Color(1,.78f,.4f));
            Label(canvas.transform,"Subtitle","Quatro caminhos para sobreviver à arena • Selecione, confirme e inicie",new Vector2(.02f,.885f),new Vector2(.98f,.935f),16,TextAnchor.MiddleCenter,Color.white);
            var buttons=new Button[4]; var outlines=new Outline[4]; var names=new Text[4]; var details=new Text[4];
            for(int i=0;i<4;i++)
            {
                float x=.02f+i*.245f; var card=Panel(canvas.transform,"ClassCard_"+i,new Vector2(x,.145f),new Vector2(x+.225f,.475f),new Color(.15f,.13f,.105f));
                buttons[i]=card.AddComponent<Button>(); buttons[i].targetGraphic=card.GetComponent<Image>(); var colors=buttons[i].colors; colors.highlightedColor=new Color(1,.87f,.66f); colors.selectedColor=colors.normalColor; buttons[i].colors=colors;
                outlines[i]=card.AddComponent<Outline>(); outlines[i].effectColor=new Color(1,.72f,.25f); outlines[i].effectDistance=new Vector2(3,-3); outlines[i].enabled=i==0;
                names[i]=Label(card.transform,"ClassName","",new Vector2(.04f,.86f),new Vector2(.96f,1),19,TextAnchor.MiddleCenter,catalog.classes[i].color*1.4f+new Color(.15f,.15f,.15f,0));
                details[i]=Label(card.transform,"Details","",new Vector2(.055f,.035f),new Vector2(.945f,.85f),13,TextAnchor.UpperLeft,new Color(.92f,.9f,.83f));
            }
            Panel(canvas.transform,"Footer",Vector2.zero,new Vector2(1,.135f),new Color(.10f,.085f,.065f));
            var status=Label(canvas.transform,"Status","",new Vector2(.025f,.025f),new Vector2(.53f,.12f),17,TextAnchor.MiddleLeft,Color.white);
            var confirm=MakeButton(canvas.transform,"ConfirmButton","CONFIRMAR SELEÇÃO",new Vector2(.58f,.035f),new Vector2(.975f,.11f));
            new GameObject("SelectionController").AddComponent<GladiatorSelectionController>().Configure(catalog,buttons,outlines,names,details,halos,confirm,status);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(scene,PlayerClassController.SelectionScene);
        }
        [MenuItem("Bellator/Refresh Class Selection Previews")]
        public static void RepairPreviews()
        {
            var scene=EditorSceneManager.OpenScene(PlayerClassController.SelectionScene);
            var catalog=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            foreach(var c in catalog.classes)
            {
                var root=scene.GetRootGameObjects().First(g=>g.name=="ClassPreviews").transform.Find("Preview_"+c.id);
                var body=root.GetChild(0).Find("BodyPivot"); var socket=body.Find("LeftShoulder/LeftHandSocket");
                Transform shield=null;
                if(c.shield!=null)
                {
                    var existing=socket.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="SelectionShield");
                    shield=existing!=null?existing:Mount(c.shield,socket).transform; shield.name="SelectionShield"; shield.rotation=root.rotation;
                }
                var pose=root.GetComponent<ClassPreviewPose>(); pose.Configure(body.Find("LeftShoulder"),body.Find("RightShoulder"),shield,c.equipment[0].type); EditorUtility.SetDirty(pose);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/BellatorClasses.preview-result.txt","Selection shields refreshed and serialized.");
        }
        static GameObject Mount(WeaponDefinition w,Transform socket)
        { var g=UnityEngine.Object.Instantiate(w.visualPrefab,socket,false); g.transform.localPosition=w.mountOffset; g.transform.localRotation=Quaternion.Euler(w.mountEuler); return g; }
        static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        { var g=new GameObject(name,typeof(RectTransform),typeof(Image)); g.transform.SetParent(parent,false); var r=g.GetComponent<RectTransform>(); r.anchorMin=min; r.anchorMax=max; r.offsetMin=r.offsetMax=Vector2.zero; g.GetComponent<Image>().color=color; return g; }
        static Text Label(Transform parent,string name,string content,Vector2 min,Vector2 max,int size,TextAnchor alignment,Color color)
        { var g=new GameObject(name,typeof(RectTransform),typeof(Text)); g.transform.SetParent(parent,false); var r=g.GetComponent<RectTransform>(); r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero; var t=g.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text=content;t.fontSize=size;t.alignment=alignment;t.color=color;t.raycastTarget=false; return t; }
        static Button MakeButton(Transform parent,string name,string label,Vector2 min,Vector2 max)
        { var g=Panel(parent,name,min,max,new Color(.58f,.36f,.12f)); var b=g.AddComponent<Button>(); b.targetGraphic=g.GetComponent<Image>(); Label(g.transform,"Label",label,Vector2.zero,Vector2.one,18,TextAnchor.MiddleCenter,Color.white); return b; }
    }
}






