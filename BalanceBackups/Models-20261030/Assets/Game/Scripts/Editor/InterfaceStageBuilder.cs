using System;
using System.IO;
using System.Linq;
using Bellator.Classes;
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
    public static class InterfaceStageBuilder
    {
        static InterfaceStageBuilder() { EditorApplication.update += Check; }
        static void Check()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorInterface.install")) return;
            File.Delete("Temp/BellatorInterface.install");
            try { Install(); } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorInterface.error.txt", e.ToString()); }
        }
        [MenuItem("Bellator/Install Stage 7 Complete Interface")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach(string path in new[]{PlayerClassController.SelectionScene, PlayerClassController.ArenaScene})
            {
                var scene = EditorSceneManager.OpenScene(path);
                if(UnityEngine.Object.FindAnyObjectByType<GameInterface>() == null) new GameObject("GameInterface").AddComponent<GameInterface>();
                if(path == PlayerClassController.SelectionScene)
                {
                    // More space for attributes, equipment and ability descriptions.
                    foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.name=="Details"))
                    { text.resizeTextForBestFit=true; text.resizeTextMinSize=12; text.resizeTextMaxSize=14; }
                    var subtitle=GameObject.Find("Subtitle").GetComponent<RectTransform>(); subtitle.anchorMin=new Vector2(.23f,.885f);
                    foreach(var card in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("ClassCard_")))
                    { var r=card.GetComponent<RectTransform>(); r.anchorMin=new Vector2(r.anchorMin.x,.14f); r.anchorMax=new Vector2(r.anchorMax.x,.49f); }
                    var camera=Camera.main; camera.rect=new Rect(0,.49f,1,.39f); camera.orthographicSize=2.35f;
                    // The preview camera renders only a strip. Clear the complete screen first,
                    // including gaps between cards, so the previous scene never bleeds through.
                    if(GameObject.Find("SelectionBackdropCamera")==null)
                    {
                        var backdrop=new GameObject("SelectionBackdropCamera").AddComponent<Camera>();
                        backdrop.depth=-10;backdrop.cullingMask=0;backdrop.clearFlags=CameraClearFlags.SolidColor;
                        backdrop.backgroundColor=new Color(.075f,.065f,.055f);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            var main = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); cam.tag="MainCamera"; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.13f,.105f,.085f);
            new GameObject("GameInterface").AddComponent<GameInterface>();
            new GameObject("MenuEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(main, GameInterface.MainScene);
            var ordered=new[]{GameInterface.MainScene,PlayerClassController.SelectionScene,PlayerClassController.ArenaScene};
            EditorBuildSettings.scenes=ordered.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!ordered.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BellatorInterface.installed.txt", "Stage 7 native interface installed. MainMenu first; selection and arena preserved. Unity " + Application.unityVersion);
        }
    }
}
