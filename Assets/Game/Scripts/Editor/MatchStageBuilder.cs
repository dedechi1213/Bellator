using System;
using System.IO;
using System.Linq;
using Bellator.Arena;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class MatchStageBuilder
    {
        static MatchStageBuilder() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!File.Exists("Temp/BellatorMatch.install")) return;
            File.Delete("Temp/BellatorMatch.install");
            try { Install(); } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorMatch.error.txt",e.ToString()); }
        }
        [MenuItem("Bellator/Install Stage 6 Survival Match")]
        public static void Install()
        {
            var scene=EditorSceneManager.OpenScene(PlayerClassController.ArenaScene);
            if(UnityEngine.Object.FindAnyObjectByType<GameManager>()!=null) throw new InvalidOperationException("Match already installed; preserved.");
            Directory.CreateDirectory("Assets/Game/ScriptableObjects/Waves"); Directory.CreateDirectory("Assets/Game/UI"); AssetDatabase.Refresh();
            var actor=UnityEngine.Object.FindAnyObjectByType<PlayerClassController>();
            var legacy=UnityEngine.Object.FindAnyObjectByType<EnemyEncounter>(); legacy.gameObject.SetActive(false);
            var training=UnityEngine.Object.FindAnyObjectByType<TrainingArenaControls>(); training.enabled=false;
            foreach(var target in training.GetComponentsInChildren<DamageReceiver>(true)) target.gameObject.SetActive(false);
            var combatHUD=training.GetComponent<CombatHUD>(); combatHUD.SurvivalMode=true; EditorUtility.SetDirty(combatHUD);
            var schedule=ScriptableObject.CreateInstance<WaveSchedule>();
            var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Gladiator.prefab").GetComponent<EnemyBrain>();
            var heavy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Heavy.prefab").GetComponent<EnemyBrain>();
            var archer=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Archer.prefab").GetComponent<EnemyBrain>();
            int[,] counts={{2,0,0},{3,0,0},{2,1,0},{3,0,1},{2,2,2}};
            schedule.waves=new WaveDefinition[5];
            for(int i=0;i<5;i++) schedule.waves[i]=new WaveDefinition {displayName="Onda "+(i+1),groups=new[]{new WaveGroup{prefab=common,count=counts[i,0]},new WaveGroup{prefab=heavy,count=counts[i,1]},new WaveGroup{prefab=archer,count=counts[i,2]}}};
            AssetDatabase.CreateAsset(schedule,"Assets/Game/ScriptableObjects/Waves/ArenaSurvival.asset");
            var systems=new GameObject("MatchSystems"); var game=systems.AddComponent<GameManager>(); var waves=systems.AddComponent<WaveManager>();
            var actorsRoot=new GameObject("WaveEnemies").transform; actorsRoot.SetParent(systems.transform,false);
            var spawns=UnityEngine.Object.FindObjectsByType<ArenaSpawnPoint>(FindObjectsSortMode.None);
            waves.Configure(schedule,spawns.Where(p=>p.Kind==ArenaSpawnKind.Enemy).OrderBy(p=>p.name).ToArray(),actor.GetComponent<DamageReceiver>(),actorsRoot);
            var gate=UnityEngine.Object.FindAnyObjectByType<MainGateController>();
            var zone=new GameObject("MainExitCrossing"); zone.transform.SetParent(systems.transform); zone.transform.SetPositionAndRotation(gate.transform.position,gate.transform.rotation);
            var box=zone.AddComponent<BoxCollider>(); box.isTrigger=true; box.center=new Vector3(0,1.6f,2.5f); box.size=new Vector3(5.8f,3.5f,2);
            var body=zone.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
            var exit=zone.AddComponent<ArenaExitTrigger>(); exit.Configure(actor.transform,gate.transform);
            var beacon=GameObject.CreatePrimitive(PrimitiveType.Sphere); beacon.name="ExitBeacon_Primitive"; beacon.transform.SetParent(zone.transform,false); beacon.transform.localPosition=new Vector3(0,6,-1.45f); beacon.transform.localScale=Vector3.one*.65f; UnityEngine.Object.DestroyImmediate(beacon.GetComponent<Collider>());
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor",new Color(.25f,1,.43f)); AssetDatabase.CreateAsset(material,"Assets/Game/Materials/ExitBeacon.mat"); beacon.GetComponent<Renderer>().sharedMaterial=material; beacon.SetActive(false);
            game.Configure(actor,waves,gate,exit,spawns.First(p=>p.Kind==ArenaSpawnKind.Player).transform,beacon,combatHUD);
            BuildUI(systems.transform,game);
            var events=new GameObject("MatchEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(systems.transform,false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing script: "+t.name);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            File.WriteAllText("Temp/BellatorMatch.installed.txt","Five configurable waves, independent managers, safe spawns, gate crossing, pause, defeat/victory and match UI installed. Unity "+Application.unityVersion);
            EditorSceneManager.OpenScene(PlayerClassController.SelectionScene);
        }
        static void BuildUI(Transform parent,GameManager game)
        {
            var canvas=new GameObject("MatchUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas.transform.SetParent(parent,false); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder=10;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,800); scaler.matchWidthOrHeight=.5f;
            var header=Panel(canvas.transform,"WaveBanner",new Vector2(.34f,.80f),new Vector2(.985f,.98f),new Color(.08f,.065f,.045f,.86f)); header.GetComponent<Image>().raycastTarget=false;
            var wave=Label(header.transform,"WaveNumber",new Vector2(.025f,.72f),new Vector2(.975f,.98f),19,new Color(1,.78f,.36f));
            var message=Label(header.transform,"WaveMessage",new Vector2(.025f,.37f),new Vector2(.975f,.73f),25,Color.white);
            var countdown=Label(header.transform,"Countdown",new Vector2(.025f,.16f),new Vector2(.975f,.40f),17,new Color(.9f,.87f,.77f));
            var alive=Label(header.transform,"RemainingEnemies",new Vector2(.025f,0),new Vector2(.975f,.19f),14,new Color(.85f,.82f,.75f));
            var modal=Panel(canvas.transform,"MatchModal",Vector2.zero,Vector2.one,new Color(0,0,0,.78f));
            var card=Panel(modal.transform,"ModalCard",new Vector2(.25f,.20f),new Vector2(.75f,.80f),new Color(.15f,.115f,.075f,.99f));
            var title=Label(card.transform,"ResultTitle",new Vector2(.06f,.76f),new Vector2(.94f,.96f),40,new Color(1,.75f,.3f));
            var description=Label(card.transform,"ResultDescription",new Vector2(.07f,.55f),new Vector2(.93f,.77f),20,Color.white);
            var resume=Button(card.transform,"ResumeButton","RETOMAR",new Vector2(.15f,.39f),new Vector2(.85f,.52f));
            var restart=Button(card.transform,"RestartButton","REINICIAR PARTIDA",new Vector2(.15f,.22f),new Vector2(.85f,.35f));
            var menu=Button(card.transform,"MenuButton","VOLTAR À SELEÇÃO",new Vector2(.15f,.05f),new Vector2(.85f,.18f));
            modal.SetActive(false);
            var hud=canvas.AddComponent<MatchHUD>();
            hud.Configure(null,wave,message,countdown,alive,modal,title,description,resume,restart,menu);
            // Save the reusable UI before applying the scene-only manager reference.
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvas,"Assets/Game/UI/MatchHUD.prefab",InteractionMode.AutomatedAction);
            hud.Configure(game,wave,message,countdown,alive,modal,title,description,resume,restart,menu);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hud); EditorUtility.SetDirty(hud);
        }
        static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        { var g=new GameObject(name,typeof(RectTransform),typeof(Image)); g.transform.SetParent(parent,false); var r=g.GetComponent<RectTransform>(); r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero; g.GetComponent<Image>().color=color; return g; }
        static Text Label(Transform parent,string name,Vector2 min,Vector2 max,int size,Color color)
        { var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=g.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=color;t.raycastTarget=false;return t; }
        static Button Button(Transform parent,string name,string label,Vector2 min,Vector2 max)
        { var g=Panel(parent,name,min,max,new Color(.57f,.35f,.11f)); var b=g.AddComponent<Button>(); b.targetGraphic=g.GetComponent<Image>(); Label(g.transform,"Label",Vector2.zero,Vector2.one,18,Color.white).text=label; return b; }
        /// <summary>Isolates existing opt-in regression suites from the survival loop; never used in builds.</summary>
        public static void PrepareLegacySandbox()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); if(game==null) return;
            game.gameObject.SetActive(false);
            foreach(var encounter in Resources.FindObjectsOfTypeAll<EnemyEncounter>()) if(encounter.gameObject.scene.IsValid() && encounter.gameObject.scene.isLoaded) encounter.gameObject.SetActive(true);
            foreach(var controls in Resources.FindObjectsOfTypeAll<TrainingArenaControls>())
            {
                if(!controls.gameObject.scene.IsValid() || !controls.gameObject.scene.isLoaded) continue;
                controls.enabled=true; foreach(var target in controls.GetComponentsInChildren<DamageReceiver>(true)) target.gameObject.SetActive(true);
                var hud=controls.GetComponent<CombatHUD>(); hud.enabled=true; hud.SurvivalMode=false;
            }
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerClassController>();
            player.GetComponent<PlayerCombat>().ControlsSuspended=false; player.GetComponent<PlayerAbility>().SimulationSuspended=false;
            player.GetComponent<ThirdPersonMotor>().AcceptInput=true; player.GetComponent<ThirdPersonInput>().enabled=true; player.GetComponent<CombatInput>().enabled=true;
            if(Camera.main!=null) Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=true;
        }
    }
}

