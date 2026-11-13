using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using Bellator.Multiplayer;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class MultiplayerSetup
    {
        const string Request = "Temp/BellatorOnline.request";
        static MultiplayerSetup() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            string action=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try
            {
                if(action=="prepare") Prepare();
                else if(action=="build") Build();
                else if(action=="validate") Validate();
                else if(action=="offline") { if(EditorSceneManager.GetActiveScene().isDirty) throw new Exception("Cena atual com mudanças não salvas."); PlayerIntegrationValidation.Begin(); }
                File.WriteAllText("Logs/Online-"+action+".txt","PASS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex) { Debug.LogException(ex); File.WriteAllText("Logs/Online-"+action+".txt","FAIL\n"+ex); }
        }
        [MenuItem("Bellator/Multiplayer/Preparar assets online")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play Mode antes de preparar.");
            Directory.CreateDirectory("Assets/Game/Resources"); Directory.CreateDirectory("Assets/Game/Prefabs/Multiplayer");
            const string networkPrefab="Assets/Game/Prefabs/Multiplayer/OnlineGladiator.prefab";
            var root=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab");
            try
            {
                root.name="OnlineGladiator";
                root.AddComponent<NetworkObject>(); root.AddComponent<NetworkGladiator>();
                root.GetComponent<ThirdPersonInput>().enabled=false; root.GetComponent<CombatInput>().enabled=false;
                root.GetComponent<ThirdPersonMotor>().enabled=false; root.GetComponent<PlayerClassController>().enabled=false;
                root.GetComponent<PlayerCombat>().enabled=false; root.GetComponent<PlayerAbility>().enabled=false;
                root.GetComponent<PlayerLockOn>().enabled=false;
                PrefabUtility.SaveAsPrefabAsset(root,networkPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var config=AssetDatabase.LoadAssetAtPath<OnlineConfiguration>("Assets/Game/Resources/BellatorOnline.asset");
            if(config==null) { config=ScriptableObject.CreateInstance<OnlineConfiguration>(); AssetDatabase.CreateAsset(config,"Assets/Game/Resources/BellatorOnline.asset"); }
            config.playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(networkPrefab);
            config.cameraPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/ThirdPersonCamera.prefab");
            config.catalog=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            // Save a separate scene; never edit or resave the source arena.
            if(!File.Exists(OnlineConfiguration.Arena))
            {
                AssetDatabase.CopyAsset(PlayerClassController.ArenaScene,OnlineConfiguration.Arena);
                var scene=EditorSceneManager.OpenScene(OnlineConfiguration.Arena,OpenSceneMode.Additive);
                try
                {
                    foreach(var game in scene.GetRootGameObjects())
                    {
                        foreach(var m in game.GetComponentsInChildren<GameManager>(true)) UnityEngine.Object.DestroyImmediate(m.gameObject);
                        if(game==null) continue;
                        foreach(var player in game.GetComponentsInChildren<PlayerClassController>(true)) UnityEngine.Object.DestroyImmediate(player.gameObject);
                        if(game==null) continue;
                        foreach(var ui in game.GetComponentsInChildren<GameInterface>(true)) UnityEngine.Object.DestroyImmediate(ui.gameObject);
                        if(game==null) continue;
                        foreach(var enemy in game.GetComponentsInChildren<EnemyBrain>(true)) UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                        if(game==null) continue;
                        foreach(var camera in game.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
                        if(game==null) continue;
                        if(game.name=="Training" || game.name=="ExitBeacon_Primitive" || game.name=="MatchEventSystem") UnityEngine.Object.DestroyImmediate(game);
                    }
                    for(int i=0;i<2;i++)
                    {
                        var spawn=new GameObject("OnlineSpawn"+i); SceneManager.MoveGameObjectToScene(spawn,scene);
                        spawn.transform.position=new Vector3(i==0?-5:5,.15f,-5);
                        spawn.transform.rotation=Quaternion.Euler(0,i==0?90:270,0);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
                finally { EditorSceneManager.CloseScene(scene,true); }
            }
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==OnlineConfiguration.Arena)) scenes.Add(new EditorBuildSettingsScene(OnlineConfiguration.Arena,true));
            EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Bellator/Multiplayer/Validar configuracao")]
        public static void Validate()
        {
            var config=Resources.Load<OnlineConfiguration>("BellatorOnline");
            if(config==null || config.playerPrefab==null || config.catalog==null || config.cameraPrefab==null) throw new Exception("Referências online incompletas.");
            if(config.playerPrefab.GetComponent<NetworkObject>()==null || config.playerPrefab.GetComponent<NetworkGladiator>()==null) throw new Exception("Prefab online inválido.");
            if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab").GetComponent<NetworkObject>()!=null) throw new Exception("O prefab offline foi alterado.");
            foreach(var c in config.catalog.classes) if(c==null || c.ability==null || c.animatedVisualPrefab==null) throw new Exception("Gladiador incompleto.");
            if(!EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==OnlineConfiguration.Arena)) throw new Exception("Arena fora da build.");
            Debug.Log("Bellator online: configuração validada; originais offline preservados.");
        }
        [MenuItem("Bellator/Multiplayer/Gerar build de teste Windows")]
        public static void Build()
        {
            Validate(); Directory.CreateDirectory("Builds/OnlineTest");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName="Builds/OnlineTest/Bellator.exe", target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build falhou: "+report.summary.result);
        }
    }
}

