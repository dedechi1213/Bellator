using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class GladiatorModelUpgrade
    {
        public const string Root="Assets/Game/Art/Gladiators";
        public const string Evidence="Assets/Game/Documentation/GladiatorModels";
        static readonly string[] Classes={"Survivor","Colossus","Warrior","Hunter"};
        static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        static Material[] shared;
        static readonly StringBuilder report=new StringBuilder();
        static GladiatorModelUpgrade() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/BellatorModels.build"))
            {
                File.Delete("Temp/BellatorModels.build");
                try { Build(); }catch(Exception e) {Debug.LogException(e);File.WriteAllText("Temp/BellatorModels.error.txt",e.ToString());}
            }
            if(File.Exists("Temp/BellatorModels.integration"))
            {
                File.Delete("Temp/BellatorModels.integration");PlayerIntegrationValidation.Begin();
            }
        }
        [MenuItem("Bellator/Models/Rebuild gladiator artwork")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before authoring models.");
            // Only explicit invocation modifies content; import and domain reload are read-only.
            foreach(var folder in new[]{Root+"/Meshes",Root+"/Materials",Root+"/Textures",Evidence})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();meshes.Clear();report.Clear();
            report.AppendLine("BELLATOR GLADIATOR ART — Unity "+Application.unityVersion+" — "+DateTime.Now.ToString("yyyy-MM-dd"));
            if(Application.isBatchMode || !SceneManager.GetActiveScene().IsValid())EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                if(!File.Exists(Evidence+"/Before.png"))RenderLineup(false);
                CreateMaterials();
                foreach(string weapon in new[]{"ShortSword","HeavySword","Shield","Bow","Crossbow"})UpgradeWeaponPrefab("Assets/Game/Prefabs/Weapons/"+weapon+".prefab",weapon);
                for(int c=0;c<4;c++)
                {
                    UpgradePrefab("Assets/Game/Prefabs/Classes/"+Classes[c]+"Visual.prefab",c);
                    UpgradePrefab("Assets/Game/Animations/SoulslikeGameplay/Visuals/"+Classes[c]+"AnimatedVisual.prefab",c);
                    UpgradePrefab("Assets/Game/Animations/SoulslikeStage1/PreviewPrefabs/"+Classes[c]+"AnimationPreview.prefab",c);
                }
                UpgradePrefab("Assets/Game/Prefabs/Enemies/Gladiator.prefab",4);
                UpgradePrefab("Assets/Game/Prefabs/Enemies/Heavy.prefab",5);
                UpgradePrefab("Assets/Game/Prefabs/Enemies/Archer.prefab",6);
                // The legacy player already inherits WarriorAnimatedVisual as a nested prefab.
                // Preserve its gameplay serialization and let that source update propagate.
                report.AppendLine("Legacy player: preserved; artwork inherited from WarriorAnimatedVisual.");
                // Some old scenes have embedded copies, rather than linked prefab instances.
                foreach(string path in Directory.GetFiles("Assets/Game/Scenes","*.unity"))
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);bool changed=false;
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        foreach(var body in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BodyPivot").ToArray())
                        {
                            if(body.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.name.StartsWith("BellatorArt_LOD")))continue;
                            if(body.Find("LeftHip")==null)continue;
                            string key=body.root.name+"/"+body.parent.name;int p=Profile(key);
                            // Selection previews share a ClassPreviews root; inspect ancestors for their class.
                            for(var t=body;t!=null;t=t.parent) {int candidate=Profile(t.name);if(candidate!=2) {p=candidate;break;}}
                            UpgradeVisual(body,p);changed=true;
                        }
                        foreach(var socket in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("HandSocket")).ToArray())
                            foreach(Transform child in socket.Cast<Transform>().ToArray())
                            {string weapon=WeaponName(child.name);if(weapon!=null && child.Find("BellatorWeaponArt")==null){UpgradeWeapon(child.gameObject,weapon);changed=true;}}
                    }
                    if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.AppendLine("Scene visual copies refreshed: "+path);}
                }
                AssetDatabase.SaveAssets();RenderLineup(true);ValidateAssets();
                File.WriteAllText(Evidence+"/BuildValidation.txt",report.ToString());
                File.WriteAllText("Temp/BellatorModels.built.txt",report.ToString());
            }
            catch(Exception e) {File.WriteAllText("Temp/BellatorModels.error.txt",e.ToString());throw;}
            finally {if(setup.Any(s=>s.isLoaded&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);AssetDatabase.SaveAssets();}
        }
        static int Profile(string name)
        {
            if(name.Contains("Survivor")||name.Contains("Sobrevivente"))return 0;
            if(name.Contains("Colossus")||name.Contains("Colosso"))return 1;
            if(name.Contains("Hunter")||name.Contains("Caçador"))return 3;
            if(name.Contains("Heavy")||name.Contains("pesado"))return 5;
            if(name.Contains("Archer")||name.Contains("Arqueiro"))return 6;
            if(name.Contains("Gladiator")||name.Contains("comum"))return 4;
            return 2;
        }
        static Transform Find(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        static void UpgradePrefab(string path,int profile)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body=Find(root.transform,"BodyPivot");if(body==null)throw new Exception("Missing skeleton: "+path);
                UpgradeVisual(body,profile);
                foreach(var socket in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("HandSocket")).ToArray())
                    foreach(Transform weapon in socket.Cast<Transform>().ToArray()) {string name=WeaponName(weapon.name);if(name!=null)UpgradeWeapon(weapon.gameObject,name);}
                PrefabUtility.SaveAsPrefabAsset(root,path);report.AppendLine("Upgraded: "+path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        static void UpgradeVisual(Transform body,int profile)
        {
            foreach(var old in body.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("BellatorArt_LOD")).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var lodGroup=body.GetComponent<LODGroup>();if(lodGroup!=null)UnityEngine.Object.DestroyImmediate(lodGroup);
            foreach(var renderer in body.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool weapon=false;for(var t=renderer.transform;t!=body&&t!=null;t=t.parent)if(t.name.EndsWith("HandSocket")){weapon=true;break;}
                if(weapon)continue;
                var filter=renderer.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(renderer);if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);
            }
            Transform torso=body.Find("TorsoPivot")??body,head=Find(body,"HeadPivot");
            if(head==null) {head=body.Find("ArtHeadBone");if(head==null){head=new GameObject("ArtHeadBone").transform;head.SetParent(body,false);head.localPosition=new Vector3(0,1.63f,0);}}
            var bones=new List<Transform>{body,torso,head};
            foreach(string side in new[]{"Left","Right"}) {var arm=torso.Find(side+"Shoulder");bones.Add(arm);bones.Add(arm.Find("Elbow")??arm);}
            foreach(string side in new[]{"Left","Right"}) {var hip=body.Find(side+"Hip");var knee=hip.Find("Knee")??hip;bones.Add(hip);bones.Add(knee);bones.Add(knee.Find("Ankle")??knee);}
            if(bones.Any(b=>b==null))throw new Exception("Missing required art bone");
            bool articulated=torso!=body;var bind=bones.Select(t=>t.worldToLocalMatrix*body.localToWorldMatrix).ToArray();
            var lods=new LOD[2];
            for(int lod=0;lod<2;lod++)
            {
                string key="Profile"+profile+(articulated?"_Articulated":"_Legacy")+"_LOD"+lod;
                if(!meshes.TryGetValue(key,out var mesh))
                {
                    var builder=new GladiatorMeshWorkshop(lod);GladiatorArtGeometry.Character(builder,profile,Enumerable.Range(0,bones.Count).ToArray());
                    mesh=SaveMesh(builder.Finish(key,bind),key);meshes.Add(key,mesh);
                    report.AppendLine(key+": "+mesh.vertexCount+" vertices, "+mesh.triangles.Length/3+" triangles");
                }
                var art=new GameObject("BellatorArt_LOD"+lod);art.transform.SetParent(body,false);
                var skin=art.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones.ToArray();skin.rootBone=body;skin.sharedMaterials=Palette(profile);
                skin.quality=SkinQuality.Bone2;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;skin.updateWhenOffscreen=false;
                // Roll and death clips move the whole skeleton; bounds must include those poses.
                skin.localBounds=articulated?new Bounds(new Vector3(0,1.05f,0),new Vector3(3.8f,4.5f,4.5f)):mesh.bounds;
                lods[lod]=new LOD(lod==0?.20f:.018f,new Renderer[]{skin});
            }
            lodGroup=body.gameObject.AddComponent<LODGroup>();lodGroup.SetLODs(lods);lodGroup.fadeMode=LODFadeMode.None;lodGroup.localReferencePoint=new Vector3(0,1.1f,0);lodGroup.size=2.4f;
        }
        internal static Mesh SaveMesh(Mesh mesh,string key)
        {
            string path=Root+"/Meshes/"+key+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            // Mesh owns native skin buffers: CopySerialized can retain stale bone streams
            // when a rebuilt mesh has a different vertex count. Replace every stream explicitly.
            existing.Clear(false);existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;
            existing.normals=mesh.normals;existing.tangents=mesh.tangents;existing.uv=mesh.uv;
            existing.bindposes=mesh.bindposes;if(mesh.boneWeights.Length>0)existing.boneWeights=mesh.boneWeights;
            existing.subMeshCount=mesh.subMeshCount;
            for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);
            existing.bounds=mesh.bounds;EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;
        }
        static void CreateMaterials()
        {
            shared=new Material[6];
            shared[2]=Material("OiledHide",new Color(.22f,.105f,.047f),0,.22f,1);
            shared[3]=Material("HammeredBronze",new Color(.47f,.30f,.12f),.78f,.33f,2);
            shared[4]=Material("CharcoalHair",new Color(.055f,.037f,.026f),0,.18f,3);
            shared[5]=Material("UnbleachedLinen",new Color(.68f,.59f,.43f),0,.12f,0);
            Color[] skin={new Color(.57f,.39f,.27f),new Color(.62f,.40f,.27f),new Color(.60f,.405f,.285f),new Color(.48f,.325f,.22f),new Color(.53f,.35f,.24f),new Color(.48f,.31f,.22f),new Color(.61f,.43f,.30f)};
            Color[] cloth={new Color(.40f,.30f,.17f),new Color(.40f,.062f,.043f),new Color(.095f,.22f,.31f),new Color(.16f,.25f,.105f),new Color(.39f,.10f,.065f),new Color(.20f,.15f,.115f),new Color(.31f,.30f,.13f)};
            for(int i=0;i<7;i++) {Material("Skin"+i,skin[i],0,.24f,4);Material("Linen"+i,cloth[i],0,.15f,0);}
            Material("ForgedIron",new Color(.28f,.31f,.32f),.82f,.37f,2);
            Material("AshWood",new Color(.27f,.13f,.051f),0,.20f,5);
        }
        static Material Material(string name,Color tint,float metal,float smooth,int surface)
        {
            string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.name=name;
            string texture=Root+"/Textures/Surface"+surface;
            if(!File.Exists(texture+"_Albedo.png"))CreateSurface(texture,surface);
            mat.SetColor("_BaseColor",tint);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture+"_Albedo.png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture+"_Normal.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",surface==2?.25f:surface==4?.18f:.38f);
            mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_SpecularHighlights",1);EditorUtility.SetDirty(mat);return mat;
        }
        static void CreateSurface(string path,int type)
        {
            const int n=256;float[] h=new float[n*n];var albedo=new Texture2D(n,n,TextureFormat.RGBA32,false);var normal=new Texture2D(n,n,TextureFormat.RGBA32,false,true);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float noise=Mathf.PerlinNoise(x*.19f+type*4,y*.19f),fine=Mathf.PerlinNoise(x*.71f,y*.71f+type);
                float weave=type==0?(Mathf.Sin(x*Mathf.PI*.5f)*Mathf.Sin(y*Mathf.PI*.5f))*.09f:0;
                float grain=type==5?Mathf.Sin(x*.22f+Mathf.PerlinNoise(x*.025f,y*.025f)*4)*.12f:0;
                float scratch=type==2&&Mathf.Sin(y*.79f+x*.12f)>.99f?.12f:0;
                h[y*n+x]=noise*.12f+fine*.06f+weave+grain-scratch;
                float c=.86f+noise*.10f+fine*.035f+weave*.35f+grain*.45f-scratch*.4f;
                albedo.SetPixel(x,y,new Color(c,c,c,1));
            }
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=h[y*n+(x+1)%n]-h[y*n+(x+n-1)%n],dy=h[((y+1)%n)*n+x]-h[((y+n-1)%n)*n+x];
                var v=new Vector3(-dx,-dy,1).normalized;normal.SetPixel(x,y,new Color(v.x*.5f+.5f,v.y*.5f+.5f,v.z*.5f+.5f,1));
            }
            File.WriteAllBytes(path+"_Albedo.png",albedo.EncodeToPNG());File.WriteAllBytes(path+"_Normal.png",normal.EncodeToPNG());UnityEngine.Object.DestroyImmediate(albedo);UnityEngine.Object.DestroyImmediate(normal);
            foreach(var suffix in new[]{"_Albedo.png","_Normal.png"})
            {
                AssetDatabase.ImportAsset(path+suffix);var importer=(TextureImporter)AssetImporter.GetAtPath(path+suffix);importer.textureType=suffix.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=256;importer.SaveAndReimport();
            }
        }
        static Material[] Palette(int p) => new[]{AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Skin"+p+".mat"),AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Linen"+p+".mat"),shared[2],p==5?AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForgedIron.mat"):shared[3],shared[4],shared[5]};
        static string WeaponName(string name)
        {foreach(var n in new[]{"HeavySword","ShortSword","Crossbow","Shield","Bow"})if(name.Contains(n))return n;if(name=="SelectionShield")return "Shield";return null;}
        static void UpgradeWeaponPrefab(string path,string name)
        {var root=PrefabUtility.LoadPrefabContents(path);try{UpgradeWeapon(root,name);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        static void UpgradeWeapon(GameObject root,string name)
        {
            foreach(var old in root.GetComponentsInChildren<MeshRenderer>(true)) {var filter=old.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(old);if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);}
            var prior=root.transform.Find("BellatorWeaponArt");if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            string key="Weapon_"+name;if(!meshes.TryGetValue(key,out var mesh)) {var m=new GladiatorMeshWorkshop(0);GladiatorWeaponGeometry.Build(m,name);mesh=SaveMesh(m.Finish(key),key);meshes.Add(key,mesh);}
            var art=new GameObject("BellatorWeaponArt",typeof(MeshFilter),typeof(MeshRenderer));art.transform.SetParent(root.transform,false);art.GetComponent<MeshFilter>().sharedMesh=mesh;
            var palette=Palette(2);palette[0]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForgedIron.mat");palette[1]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/AshWood.mat");
            art.GetComponent<MeshRenderer>().sharedMaterials=palette;
        }
        static void ValidateAssets()
        {
            foreach(var path in Directory.GetFiles(Root+"/Meshes","*.asset"))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null||mesh.vertexCount==0)throw new Exception("Empty mesh "+path);
                if(mesh.bindposes.Length>0 && mesh.boneWeights.Length!=mesh.vertexCount)throw new Exception("Stale bone stream "+path);
                if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)))throw new Exception("Invalid vertices");
                foreach(var w in mesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new Exception("Unnormalized weights");
            }
            foreach(string c in Classes)
            {
                var d=AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+c+".asset");
                if(d.visualPrefab==null||d.animatedVisualPrefab==null||d.equipment.Any(w=>w==null||w.visualPrefab==null))throw new Exception("Broken class references "+c);
                var animator=d.animatedVisualPrefab.GetComponentInChildren<Animator>();
                if(animator==null||animator.runtimeAnimatorController==null||animator.applyRootMotion)throw new Exception("Invalid animator "+c);
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(!string.IsNullOrEmpty(binding.path)&&animator.transform.Find(binding.path)==null)throw new Exception("Lost animation path "+binding.path);
                var skins=d.animatedVisualPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=2)throw new Exception("Missing LODs "+c);
                if(skins.Any(s=>s.bones.Any(b=>b==null)))throw new Exception("Missing skin bones");
            }
            report.AppendLine("PASS: weighted meshes, original animation curve paths, equipment references, root motion setting and two LODs per class.");
        }
        static void RenderLineup(bool after)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);Scene previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
            try
            {
                var camera=new GameObject("ArtEvidenceCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.052f,.057f,.062f);camera.orthographic=true;camera.orthographicSize=1.70f;camera.cullingMask=1<<30;
                camera.transform.position=new Vector3(0,1.8f,-9);camera.transform.LookAt(new Vector3(0,1.35f,0));camera.farClipPlane=30;
                var key=new GameObject("ArtEvidenceKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.7f;key.color=new Color(1,.87f,.73f);key.transform.rotation=Quaternion.Euler(32,-35,0);key.cullingMask=1<<30;
                var fill=new GameObject("ArtEvidenceFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.75f;fill.color=new Color(.64f,.77f,1);fill.transform.rotation=Quaternion.Euler(20,145,0);fill.cullingMask=1<<30;
                for(int i=0;i<4;i++)
                {
                    var d=AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+Classes[i]+".asset");
                    var visual=UnityEngine.Object.Instantiate(d.animatedVisualPrefab);visual.transform.position=new Vector3((i-1.5f)*1.95f,0,0);visual.transform.rotation=Quaternion.Euler(0,165,0);
                    var a=visual.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);a.Play("Idle",0,.3f);a.Update(0);
                    foreach(var w in new[]{d.equipment[0],d.shield})if(w!=null)
                    {
                        var socket=Find(visual.transform,w.leftHandMount||w.type==Bellator.Combat.EquipmentType.Shield?"LeftHandSocket":"RightHandSocket");
                        var item=UnityEngine.Object.Instantiate(w.visualPrefab,socket,false);item.transform.localPosition=w.mountOffset;item.transform.localRotation=Quaternion.Euler(w.mountEuler);
                    }
                    foreach(var lod in visual.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                    foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                }
                Capture(camera,Evidence+(after?"/After.png":"/Before.png"),1920,900);
            }
            finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        internal static void Capture(Camera camera,string path,int width,int height)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
