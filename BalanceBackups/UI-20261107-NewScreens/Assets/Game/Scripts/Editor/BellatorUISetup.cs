using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class BellatorUISetup
    {
        static BellatorUISetup() { EditorApplication.update += Check; }
        static void Check()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorArtwork.configure")) return;
            File.Delete("Temp/BellatorArtwork.configure");
            try { Configure(); File.WriteAllText("Temp/BellatorArtwork.configured","OK"); }
            catch(System.Exception error) { Debug.LogException(error); File.WriteAllText("Temp/BellatorArtwork.error",error.ToString()); }
        }
        [MenuItem("Bellator/UI/Configure Original Artwork")]
        public static void Configure()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                foreach(var package in Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories))
                { AssetDatabase.ImportPackage(package,false); break; }
            }
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles("Assets/UI/Bellator/Resources/BellatorUI","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
                importer.wrapMode=TextureWrapMode.Clamp; importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Bellator original UI artwork and TextMeshPro configured.");
        }
    }
}
