using System;
using System.IO;
using System.Linq;
using Bellator.Classes;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class CorrectionsBuilder
    {
        static CorrectionsBuilder() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(File.Exists("Temp/BellatorCorrections.install") && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            { EditorApplication.ExitPlaymode();return; }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorCorrections.install")) return;
            File.Delete("Temp/BellatorCorrections.install");
            try { Install(); } catch(Exception e) { Debug.LogException(e);File.WriteAllText("Temp/BellatorCorrections.error.txt",e.ToString()); }
        }
        [MenuItem("Bellator/Apply Fixed Equipment and Selection Corrections")]
        public static void Install()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            catalog.defaultClass=2;EditorUtility.SetDirty(catalog);
            foreach(var definition in catalog.classes)
            {
                definition.equipment=new[]{definition.equipment[0]};
                if(definition.id==GladiatorClassId.Survivor || definition.id==GladiatorClassId.Hunter) definition.shield=null;
                if(definition.id==GladiatorClassId.Hunter) definition.description="Leve e ágil. Elimina ameaças à distância com sua besta.";
                EditorUtility.SetDirty(definition);
            }
            var scene=EditorSceneManager.OpenScene(PlayerClassController.SelectionScene);
            var start=GameObject.Find("StartButton");if(start!=null) UnityEngine.Object.DestroyImmediate(start);
            var confirm=GameObject.Find("ConfirmButton");confirm.GetComponentInChildren<Text>().text="CONFIRMAR SELEÇÃO";
            var rect=confirm.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.58f,.035f);rect.anchorMax=new Vector2(.975f,.11f);
            GameObject.Find("Subtitle").GetComponent<Text>().text="Escolha seu gladiador • Confirme para voltar ao menu principal";
            GameObject.Find("Status").GetComponent<Text>().text="Escolha um gladiador e confirme sua seleção";
            // Update serialized card text as well as the runtime presentation.
            var controller=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();controller.RefreshPresentation();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(GameInterface.MainScene);
            File.WriteAllText("Temp/BellatorCorrections.installed.txt","Fixed class equipment; hunter crossbow only; selection confirms to main menu; no start button. Unity "+Application.unityVersion);
        }
    }
}
