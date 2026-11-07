using System.IO;
using Bellator.Match;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class BellatorUISetup
    {
        const string Elements="Assets/UI/Bellator/Resources/BellatorUI/Elements/";
        static BellatorUISetup() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/BellatorArtwork.configure")) return;
            File.Delete("Temp/BellatorArtwork.configure");
            try { Configure(); File.WriteAllText("Temp/BellatorArtwork.configured","OK"); }
            catch(System.Exception error) { Debug.LogException(error); File.WriteAllText("Temp/BellatorArtwork.error",error.ToString()); }
        }
        [MenuItem("Bellator/UI/Configure Original Artwork")]
        public static void Configure()
        {
            Directory.CreateDirectory(Elements);
            ExtractOriginalLogo(); ExtractModeArt(); AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Elements,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false; importer.alphaIsTransparency=Path.GetFileName(path)=="OriginalLogo.png";
                importer.wrapMode=TextureWrapMode.Clamp; importer.maxTextureSize=2048;
                // Lossless logo; high-quality BC7 for photographic RGB backgrounds/cards.
                importer.textureCompression=importer.alphaIsTransparency?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;
                if(!importer.alphaIsTransparency) { var desktop=importer.GetPlatformTextureSettings("Standalone"); desktop.name="Standalone"; desktop.overridden=true; desktop.maxTextureSize=2048; desktop.format=TextureImporterFormat.BC7; importer.SetPlatformTextureSettings(desktop); }
                importer.SaveAndReimport();
            }
            CreateFont();
            var root=new GameObject("BellatorButton",typeof(RectTransform)); root.GetComponent<RectTransform>().sizeDelta=new Vector2(540,80);
            BellatorUI.DecorateButton(root,"BOTÃO"); PrefabUtility.SaveAsPrefabAsset(root,"Assets/UI/Bellator/Prefabs/BellatorButton.prefab"); Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets(); Debug.Log("Bellator UI: separate background, exact original logo, TMP, reusable button configured.");
        }
        static Texture2D Read(string path)
        { var t=new Texture2D(2,2,TextureFormat.RGBA32,false); t.LoadImage(File.ReadAllBytes(path)); return t; }
        static void ExtractOriginalLogo()
        {
            var source=Read("Assets/UI/Bellator/Source/WhatsApp Image 2026-11-07 at 11.06.55.jpeg");
            // Fixed crop removes only empty surrounding white. Foreground RGB pixels are copied verbatim.
            var target=new Texture2D(478,340,TextureFormat.RGBA32,false);
            var pixels=source.GetPixels32(); var result=new Color32[478*340];
            for(int y=0;y<340;y++) for(int x=0;x<478;x++)
            {
                var pixel=pixels[(source.height-390+y)*source.width+121+x];
                int minimum=System.Math.Min(pixel.r,System.Math.Min(pixel.g,pixel.b));
                int maximum=System.Math.Max(pixel.r,System.Math.Max(pixel.g,pixel.b));
                if(minimum>200 && maximum-minimum<35) pixel.a=(byte)(255*Mathf.Clamp01((240-minimum)/40f));
                result[y*478+x]=pixel;
            }
            target.SetPixels32(result); target.Apply(); File.WriteAllBytes(Elements+"OriginalLogo.png",target.EncodeToPNG());
            Object.DestroyImmediate(source); Object.DestroyImmediate(target);
        }
        static void ExtractModeArt()
        {
            var source=Read("Assets/UI/Bellator/Source/Menu de Modos na Arena Romana.png");
            int[] left={307,685,1062};
            for(int i=0;i<3;i++)
            {
                var t=new Texture2D(308,180,TextureFormat.RGB24,false); t.SetPixels(source.GetPixels(left[i],source.height-522,308,180)); t.Apply();
                File.WriteAllBytes(Elements+"Mode"+i+".png",t.EncodeToPNG()); Object.DestroyImmediate(t);
            }
            Object.DestroyImmediate(source);
        }
        static void CreateFont()
        {
            string path=Elements+"Cinzel SDF.asset";
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null) return;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Bellator/Fonts/Cinzel.ttf");
            var font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
            string characters=""; for(int c=32;c<384;c++) characters+=(char)c;
            font.TryAddCharacters(characters); font.atlasPopulationMode=AtlasPopulationMode.Static; font.name="Cinzel SDF";
            AssetDatabase.CreateAsset(font,path); AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,font);
            EditorUtility.SetDirty(font);
        }
    }
}
