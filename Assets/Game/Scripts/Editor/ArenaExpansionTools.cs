using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Arena;
using Bellator.Match;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Bellator.Editor
{
    /// <summary>Explicit, editor-only migration of the original arena. No runtime construction.</summary>
    [InitializeOnLoad]
    public static class ArenaExpansionTools
    {
        public const string Root = "Assets/Game/ArenaExpansion";
        public const string Evidence = "Assets/Game/Documentation/ArenaExpansion";
        public const float RadiusX = 32, RadiusZ = 29;
        static Material limestone, stone, dark, sand, gold, red, timber, iron;
        static Mesh cube, cylinder, sphere, crowdHead;
        static ArenaExpansionTools() { EditorApplication.update += Check; }
        static void Check()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (string action in new[] { "audit", "build", "review" })
            {
                string flag = "Temp/BellatorExpansion." + action;
                if (!File.Exists(flag)) continue;
                File.Delete(flag);
                try
                {
                    if (action == "audit") Audit();
                    else if (action == "build") Expand();
                    else Review();
                    File.WriteAllText(flag + ".done", DateTime.UtcNow.ToString("O"));
                }
                catch (Exception e) { Debug.LogException(e); File.WriteAllText(flag + ".error", e.ToString()); }
                return;
            }
        }
        static Scene Open()
        {
            foreach (var s in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                if (s.isDirty && s.path != ArenaBuilder.ScenePath) throw new InvalidOperationException("Save pending scene edits before arena migration: " + s.path);
            var scene = SceneManager.GetSceneByPath(ArenaBuilder.ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ArenaBuilder.ScenePath);
            SceneManager.SetActiveScene(scene);
            return scene;
        }
        static Transform ArenaRoot(Scene scene) => scene.GetRootGameObjects().Single(g => g.name == "Arena").transform;
        [MenuItem("Bellator/Arena Expansion/1 Audit existing arena")]
        public static void Audit()
        {
            var scene = Open(); var arena = ArenaRoot(scene);
            Directory.CreateDirectory(Evidence);
            var text = new StringBuilder("BELLATOR — original scene audit\nUnity " + Application.unityVersion + "\n");
            foreach (string name in new[] { "Ground/SandFloor", "Walls", "Stands", "Gates", "Obstacles", "Lighting", "SpawnPoints" })
            {
                var t = arena.Find(name); if (!t) continue;
                text.AppendLine(name + " | position " + t.position + " | scale " + t.localScale + " | descendants " + t.GetComponentsInChildren<Transform>(true).Length);
                if (name.Contains("Floor")) text.AppendLine("Floor renderer bounds: " + t.GetComponent<Renderer>().bounds);
            }
            foreach (var p in arena.GetComponentsInChildren<ArenaSpawnPoint>()) text.AppendLine(p.name + ": " + p.Kind + " at " + p.transform.position);
            foreach (var g in arena.Find("Gates").Cast<Transform>()) text.AppendLine("Gate " + g.name + " at " + g.position + " yaw " + g.eulerAngles.y);
            foreach (var l in arena.GetComponentsInChildren<Light>()) text.AppendLine("Light " + l.name + ": " + l.type + " intensity " + l.intensity + " color " + l.color + " rotation " + l.transform.eulerAngles);
            text.AppendLine("Render pipeline: " + GraphicsSettings.currentRenderPipeline);
            text.AppendLine("NavMeshSurface: " + arena.GetComponent<NavMeshSurface>().navMeshData + "; PhysicsColliders; voxel .15m");
            text.AppendLine("Renderers: " + arena.GetComponentsInChildren<Renderer>().Length + "; colliders: " + arena.GetComponentsInChildren<Collider>().Length);
            text.AppendLine("Materials: " + string.Join(", ", arena.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m).Select(m => m.name).Distinct()));
            foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true))) text.AppendLine("Camera " + c.name + " at " + c.transform.position + " FOV " + c.fieldOfView + " far " + c.farClipPlane);
            text.AppendLine("Preserve: arena prefab identity; 5 spawn components; 5 gate frames; MovingGate/controller/carving obstacle; WaveManager entry array; GameManager/exit references; all player/camera/combat assets.");
            File.WriteAllText(Evidence + "/Before.txt", text.ToString());
            Render(scene, new Vector3(50,48,-60), new Vector3(0,2,0), "Before.png");
        }
        static Vector3 Ellipse(float angle, float offset = 0, float y = 0)
        { float a = angle * Mathf.Deg2Rad; return new Vector3((RadiusX + offset) * Mathf.Sin(a), y, (RadiusZ + offset) * Mathf.Cos(a)); }
        static float Yaw(float angle, float offset = 0)
        { float a = angle * Mathf.Deg2Rad; return Mathf.Atan2(Mathf.Sin(a)/(RadiusX+offset), Mathf.Cos(a)/(RadiusZ+offset))*Mathf.Rad2Deg; }
        static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        static void Clear(Transform parent)
        { for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject); }
        static Mesh PrimitiveMesh(PrimitiveType type)
        { var g = GameObject.CreatePrimitive(type); var mesh = g.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(g); return mesh; }
        static void SaveMesh(Mesh mesh, string name)
        {
            string path = Root + "/Geometry/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); }
            else AssetDatabase.CreateAsset(mesh, path);
        }
        // Primitive geometry is combined by material and sector before being saved.
        sealed class Batch
        {
            readonly Dictionary<Material,List<CombineInstance>> parts = new Dictionary<Material,List<CombineInstance>>();
            public void Add(Mesh mesh, Vector3 p, Vector3 size, Material m, Quaternion rotation)
            { if (!parts.ContainsKey(m)) parts[m] = new List<CombineInstance>(); parts[m].Add(new CombineInstance { mesh=mesh, transform=Matrix4x4.TRS(p,rotation,size) }); }
            public void Box(Vector3 p, Vector3 size, Material m, float yaw=0) => Add(cube,p,size,m,Quaternion.Euler(0,yaw,0));
            public void Ball(Vector3 p, Vector3 size, Material m) => Add(sphere,p,size,m,Quaternion.identity);
            public void Shaft(Vector3 p, Vector3 size, Material m, float yaw=0) => Add(cylinder,p,size,m,Quaternion.Euler(0,yaw,0));
            public void Ring(float angle,float offset,float y,float height,float depth,Material m,float span=15)
            {
                Vector3 left=Ellipse(angle-span*.5f,offset), right=Ellipse(angle+span*.5f,offset);
                Box(Ellipse(angle,offset,y),new Vector3(Vector3.Distance(left,right)+.1f,height,depth),m,Yaw(angle,offset));
            }
            public Renderer[] Flush(Transform parent,string id,bool shadow=true)
            {
                var renderers=new List<Renderer>();
                foreach(var entry in parts)
                {
                    string name=id+"_"+entry.Key.name;
                    var mesh=new Mesh { name=name, indexFormat=entry.Value.Sum(p=>p.mesh.vertexCount)>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
                    mesh.CombineMeshes(entry.Value.ToArray(),true,true); mesh.RecalculateBounds(); SaveMesh(mesh,name);
                    var go=new GameObject(entry.Key.name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
                    go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Geometry/"+name+".asset");
                    var r=go.GetComponent<MeshRenderer>(); r.sharedMaterial=entry.Key; r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;
                    r.receiveShadows=shadow; GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
                    renderers.Add(r);
                }
                return renderers.ToArray();
            }
        }
        static Texture2D Texture(string name,bool floor)
        {
            int n=floor?512:256; var texture=new Texture2D(n,n,TextureFormat.RGB24,true) { name=name, wrapMode=TextureWrapMode.Repeat, anisoLevel=4 };
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float u=(float)x/n,v=(float)y/n;
                float noise=Mathf.PerlinNoise(u*12+3,v*12+7)*.55f+Mathf.PerlinNoise(u*53+1,v*53+4)*.3f+Mathf.PerlinNoise(u*180,v*180)*.15f;
                Color c;
                if(floor)
                {
                    float tracks=Mathf.SmoothStep(.85f,.98f,Mathf.Sin((u*3+Mathf.PerlinNoise(u*4,v*4)*.4f)*Mathf.PI*2))*.04f;
                    c=Color.Lerp(new Color(.46f,.35f,.22f),new Color(.79f,.67f,.46f),noise)-new Color(tracks,tracks,tracks,0);
                }
                else
                {
                    float row=Mathf.Floor(v*8); float seam=Mathf.Min(Mathf.Repeat(u*4+row%2*.5f,1),Mathf.Repeat(v*8,1));
                    c=Color.white*(.72f+noise*.27f)*(seam<.018f?.72f:1);
                }
                pixels[y*n+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();
            string path=Root+"/Materials/"+name+".png"; File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=4; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material Mat(string name,Color color,Texture texture=null,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.16f);m.SetFloat("_Metallic",metallic);m.enableInstancing=true;
            m.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(m);return m;
        }
        static void Materials()
        {
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Geometry");Directory.CreateDirectory(Evidence);AssetDatabase.Refresh();
            var blocks=Texture("TravertineBlocks",false);
            limestone=Mat("IvoryLimestone",new Color(.83f,.75f,.59f),blocks);
            stone=Mat("WarmTravertine",new Color(.65f,.55f,.40f),blocks);
            dark=Mat("ShadowStone",new Color(.38f,.33f,.26f),blocks);
            sand=Mat("WornArenaSand",Color.white,Texture("SandWear",true));sand.mainTextureScale=new Vector2(9,8);
            gold=Mat("AntiqueBronze",new Color(.61f,.39f,.12f),null,.55f);
            red=Mat("ImperialOxblood",new Color(.34f,.035f,.028f));
            timber=Mat("GateTimber",new Color(.23f,.105f,.04f));iron=Mat("GateIron",new Color(.13f,.15f,.16f),null,.65f);
            cube=PrimitiveMesh(PrimitiveType.Cube);cylinder=PrimitiveMesh(PrimitiveType.Cylinder);sphere=PrimitiveMesh(PrimitiveType.Sphere);
            // Audience heads are tiny in gameplay: an octahedron avoids a full sphere per spectator.
            Vector3[] corners={Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,Vector3.right*.5f,Vector3.forward*.5f,Vector3.back*.5f};
            int[] faces={0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};
            var head=new Mesh { name="AudienceHead" };head.vertices=faces.Select(i=>corners[i]).ToArray();head.triangles=Enumerable.Range(0,24).ToArray();head.uv=new Vector2[24];head.RecalculateNormals();head.RecalculateBounds();SaveMesh(head,"AudienceHead");crowdHead=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Geometry/AudienceHead.asset");
        }
        static void Arch(Batch b,Vector3 center,float yaw,float width,float spring,float depth,Material m)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0);float r=width*.5f;
            for(int i=0;i<13;i++)
            {
                float t=(i+.5f)*Mathf.PI/13;
                Vector3 p=center+q*new Vector3(Mathf.Cos(t)*r,spring+Mathf.Sin(t)*r,0);
                b.Add(cube,p,new Vector3(.52f,r*Mathf.PI/13+.10f,depth),m,q*Quaternion.Euler(0,0,t*Mathf.Rad2Deg));
            }
        }
        static void Column(Batch b,Vector3 p,float yaw,float height)
        {
            b.Box(p+Vector3.up*.25f,new Vector3(1.35f,.5f,1.35f),limestone,yaw);
            b.Shaft(p+Vector3.up*(height*.5f),new Vector3(.72f,(height-.7f)*.5f,.72f),limestone);
            b.Box(p+Vector3.up*(height-.3f),new Vector3(1.35f,.45f,1.35f),limestone,yaw);
            b.Shaft(p+Vector3.up*.6f,new Vector3(.95f,.12f,.95f),stone);
        }
        static void Banner(Batch b,Vector3 p,float yaw,float height=3.5f)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0);
            b.Box(p,new Vector3(1.8f,height,.12f),red,yaw);
            foreach(float x in new[]{-.82f,.82f}) b.Box(p+q*new Vector3(x,0,-.075f),new Vector3(.07f,height,.035f),gold,yaw);
            foreach(float y in new[]{-height*.44f,height*.44f}) b.Box(p+new Vector3(0,y,0)+q*new Vector3(0,0,-.08f),new Vector3(1.8f,.07f,.04f),gold,yaw);
            b.Add(cylinder,p+q*new Vector3(0,.15f,-.12f),new Vector3(.65f,.06f,.65f),gold,q*Quaternion.Euler(90,0,0));
            b.Box(p+Vector3.up*(height*.5f+.1f),new Vector3(2,.10f,.15f),gold,yaw);
        }
        [MenuItem("Bellator/Arena Expansion/2 Expand existing arena")]
        public static void Expand()
        {
            var scene=Open();var arena=ArenaRoot(scene);
            if(!File.Exists(Evidence+"/Before.txt")) Audit();
            Materials();
            var floor=arena.Find("Ground/SandFloor");floor.localScale=new Vector3(65,.3f,59);floor.GetComponent<Renderer>().sharedMaterial=sand;
            var foundation=arena.Find("Ground/ArenaFoundation");foundation.localScale=new Vector3(114,.3f,108);
            var walls=arena.Find("Walls");
            // Keep original podium objects and references, adapting each segment to ellipse tangents.
            foreach(Transform t in walls.Cast<Transform>().ToArray())
            {
                if(t.name.StartsWith("OuterWall_") || t.name.StartsWith("Cornice_")) { Object.DestroyImmediate(t.gameObject);continue; }
                int underscore=t.name.LastIndexOf('_'); if(underscore<0 || !int.TryParse(t.name.Substring(underscore+1),out int index)) continue;
                float a=index*5+2.5f;float offset=t.name.StartsWith("WallFoot")?.3f:.55f;
                t.localPosition=Ellipse(a,offset,t.localPosition.y);t.localRotation=Quaternion.Euler(0,Yaw(a,offset),0);
                var size=t.localScale;size.x=Vector3.Distance(Ellipse(a-2.5f,offset),Ellipse(a+2.5f,offset))+.12f;t.localScale=size;
                t.GetComponent<Renderer>().sharedMaterial=t.name.StartsWith("WallFoot")?dark:t.name.StartsWith("WallCoping")?limestone:stone;
            }
            var stands=arena.Find("Stands");Clear(stands);
            foreach(string n in new[]{"ColiseumStructure","Decorations"}) { var old=arena.Find(n);if(old) Object.DestroyImmediate(old.gameObject); }
            var structure=Group("ColiseumStructure",arena);var arches=Group("Arches",structure);var columns=Group("Columns",structure);var upper=Group("UpperStructures",structure);
            var tiers=new[]{Group("LowerTier",stands),Group("MiddleTier",stands),Group("UpperTier",stands)};
            var decor=Group("Decorations",arena);var banners=Group("Banners",decor);var statues=Group("Statues",decor);var crowd=Group("Audience",decor);
            var random=new System.Random(5713);
            var crowdMats=new[]{Mat("CrowdLinen",new Color(.64f,.56f,.42f)),Mat("CrowdClay",new Color(.40f,.21f,.13f)),Mat("CrowdOlive",new Color(.29f,.32f,.19f)),Mat("CrowdCharcoal",new Color(.22f,.20f,.18f))};
            for(int sector=0;sector<24;sector++)
            {
                float a=sector*15+7.5f;
                for(int tier=0;tier<3;tier++)
                {
                    var b=new Batch();
                    for(int row=0;row<5;row++)
                    {
                        int n=tier*5+row;float offset=2.7f+n*1.16f+tier*.85f;float top=6.3f+n*.73f+tier*.4f;
                        b.Ring(a,offset,top*.5f,top,1.22f,row%3==0?limestone:stone,13.7f);
                        b.Ring(a,offset-.4f,top+.07f,.14f,.32f,limestone,13.7f);
                    }
                    float back=8.6f+tier*6.65f, high=9.7f+tier*4.05f;
                    b.Ring(a,back,high,.22f,1.9f,dark,15);
                    b.Ring(a,back+.6f,high+.65f,1.1f,.32f,limestone,15);
                    // Radial stairs between seating sectors have half-height treads.
                    for(int step=0;step<10;step++)
                    {
                        int n=tier*5;float off=2.1f+n*1.16f+tier*.85f+step*.58f;float top=6.0f+n*.73f+tier*.4f+step*.365f;
                        b.Box(Ellipse(sector*15,off,top*.5f),new Vector3(1.05f,top,.65f),limestone,Yaw(sector*15,off));
                    }
                    b.Flush(Group("Sector_"+sector.ToString("00"),tiers[tier]),"Seats_"+tier+"_"+sector);
                }
                var architecture=new Batch();var col=new Batch();var roof=new Batch();var flags=new Batch();
                float facade=22.3f;
                foreach(float baseY in new[]{3f,10.5f,18f})
                {
                    float yaw=Yaw(a,facade);Vector3 p=Ellipse(a,facade,baseY);float width=Vector3.Distance(Ellipse(a-7.5f,facade),Ellipse(a+7.5f,facade));
                    architecture.Box(Ellipse(a-7.5f,facade,baseY+2.5f),new Vector3(1.55f,5,2.6f),stone,Yaw(a-7.5f,facade));
                    Arch(architecture,p,yaw,width-2,4,2.6f,limestone);
                    architecture.Ring(a,facade,baseY+7.1f,.55f,3.05f,limestone);
                    Column(col,Ellipse(a-7.5f,facade-1.6f,baseY),Yaw(a-7.5f,facade),6.8f);
                }
                roof.Ring(a,facade,26.0f,1.1f,3.4f,stone);
                roof.Ring(a,facade,26.8f,.45f,3.8f,limestone);
                roof.Ring(a,facade,27.7f,1.5f,.65f,limestone);
                if(sector%2==0) Banner(flags,Ellipse(a,facade-2.1f,20.5f),Yaw(a,facade),3.8f);
                architecture.Flush(Group("Sector_"+sector.ToString("00"),arches),"Arcade_"+sector);
                col.Flush(Group("Sector_"+sector.ToString("00"),columns),"Columns_"+sector);
                roof.Flush(Group("Sector_"+sector.ToString("00"),upper),"Cornice_"+sector);
                flags.Flush(banners,"Banner_"+sector);
                var people=new Batch();
                for(int row=0;row<12;row++)
                {
                    int tier=row/4,n=tier*5+row%4;float off=2.7f+n*1.16f+tier*.85f;float top=6.3f+n*.73f+tier*.4f;
                    for(int seat=0;seat<10;seat++)
                    {
                        if(random.NextDouble()<.27) continue;
                        float angle=sector*15+1.5f+seat*1.3f+(float)random.NextDouble()*.3f;
                        Vector3 p=Ellipse(angle,off,top);var cloth=crowdMats[random.Next(4)];float height=.56f+(float)random.NextDouble()*.18f;
                        people.Box(p+Vector3.up*(height*.5f),new Vector3(.38f,height,.29f),cloth,Yaw(angle,off));
                        people.Add(crowdHead,p+Vector3.up*(height+.16f),new Vector3(.29f,.34f,.29f),cloth,Quaternion.identity);
                    }
                }
                var sectorCrowd=Group("Sector_"+sector.ToString("00"),crowd);var renderers=people.Flush(sectorCrowd,"Audience_"+sector,false);
                var lod=sectorCrowd.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,renderers)});lod.RecalculateBounds();
            }
            // Reuse all five gate frames, moving their geometry and original references together.
            foreach(Transform gate in arena.Find("Gates"))
            {
                bool main=gate.GetComponent<MainGateController>()!=null;float a=main?0:float.Parse(gate.name.Substring(gate.name.LastIndexOf('_')+1),System.Globalization.CultureInfo.InvariantCulture);
                gate.localPosition=Ellipse(a,.55f);gate.localRotation=Quaternion.Euler(0,Yaw(a,.55f),0);
                float w=main?6.4f:4.2f;
                // PassageFloor is historically under Ground, matched by angle rather than name.
                var floors=arena.Find("Ground").Cast<Transform>().Where(t=>t.name=="PassageFloor").ToArray();
                var passage=floors.OrderBy(t=>Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.y,a))).First();
                passage.position=gate.TransformPoint(new Vector3(0,-.2f,2.5f));passage.rotation=gate.rotation;passage.GetComponent<Renderer>().sharedMaterial=sand;
                var extra=gate.Find("ArchitecturalDetails");if(extra) Object.DestroyImmediate(extra.gameObject);
                extra=Group("ArchitecturalDetails",gate);var detail=new Batch();
                Arch(detail,new Vector3(0,1.3f,-1.55f),0,w+1,4.1f,.75f,limestone);
                foreach(float side in new[]{-1f,1f})
                {
                    Column(detail,new Vector3(side*(w*.5f+1.15f),0,-.6f),0,8.7f);
                    Banner(detail,new Vector3(side*(w*.5f+2.55f),4,-1.3f),0,2.6f);
                }
                detail.Box(new Vector3(0,9.35f,0),new Vector3(w+5,.5f,3),limestone);
                detail.Flush(extra,gate.name+"_Details");
                // Close the enlarged gap between the retained portal piers and the oval podium.
                foreach(float side in new[]{-1f,1f})
                {
                    var shoulder=GameObject.CreatePrimitive(PrimitiveType.Cube);shoulder.name="PodiumShoulder";shoulder.transform.SetParent(extra,false);
                    shoulder.transform.localPosition=new Vector3(side*(w*.5f+1.65f),3,.3f);shoulder.transform.localScale=new Vector3(2.4f,6,1.7f);shoulder.GetComponent<Renderer>().sharedMaterial=stone;
                }
                foreach(var renderer in gate.GetComponentsInChildren<Renderer>())
                {
                    if(renderer.transform.IsChildOf(extra)) continue;
                    if(renderer.name.Contains("Timber")) renderer.sharedMaterial=timber;
                    else if(renderer.name.Contains("Iron")) renderer.sharedMaterial=iron;
                    else renderer.sharedMaterial=renderer.name.Contains("Back")?dark:stone;
                }
                if(main)
                {
                    var moving=gate.Find("MovingGate");var panels=moving.Find("PlankDetails");if(panels) Object.DestroyImmediate(panels.gameObject);
                    var planks=new Batch();
                    for(int n=0;n<12;n++) planks.Box(new Vector3(-2.93f+n*.53f,0,-.275f),new Vector3(.49f,5.5f,.08f),timber);
                    planks.Flush(Group("PlankDetails",moving),"ExitPlanks");
                    foreach(var r in moving.GetComponentsInChildren<Renderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);
                    foreach(var trigger in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ArenaExitTrigger>(true))) { trigger.transform.SetPositionAndRotation(gate.position,gate.rotation);EditorUtility.SetDirty(trigger); }
                }
            }
            // Preserve the four existing enemy spawn components and their serialized WaveManager references.
            foreach(var p in arena.GetComponentsInChildren<ArenaSpawnPoint>())
            {
                if(p.Kind==ArenaSpawnKind.Player) { p.transform.localPosition=new Vector3(0,.08f,-13);continue; }
                float a=float.Parse(p.name.Substring(p.name.LastIndexOf('_')+1),System.Globalization.CultureInfo.InvariantCulture);
                p.transform.localPosition=Ellipse(a,-3,.08f);p.transform.LookAt(Vector3.up*.08f);
            }
            // Existing obstacle models become peripheral gallery ornaments; central combat stays unobstructed.
            int prop=0;foreach(Transform obstacle in arena.Find("Obstacles"))
            {
                obstacle.localPosition=Ellipse(60+prop++*45,19,18.3f);
                foreach(var collider in obstacle.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            }
            for(int side=-1;side<=1;side+=2)
            {
                var b=new Batch();Vector3 p=new Vector3(side*8.5f,7.1f,34);
                b.Box(p,new Vector3(2.8f,1.2f,2.4f),dark);p+=Vector3.up*.6f;
                b.Shaft(p+Vector3.up*1.2f,new Vector3(1.35f,1.2f,1.35f),limestone);
                b.Box(p+Vector3.up*2.8f,new Vector3(1.6f,1.5f,.8f),limestone);
                b.Ball(p+Vector3.up*4,new Vector3(.8f,.95f,.8f),limestone);
                b.Box(p+new Vector3(side*1.1f,2.8f,0),new Vector3(.5f,1.9f,.55f),limestone);
                b.Add(cylinder,p+new Vector3(-side*1.1f,2.2f,-.35f),new Vector3(1.45f,.18f,1.45f),gold,Quaternion.Euler(90,0,0));
                b.Box(p+new Vector3(side*1.1f,4,0),new Vector3(.12f,4,.12f),gold);
                b.Flush(statues,"Guardian_"+side);
            }
            var sun=arena.Find("Lighting/Sun").GetComponent<Light>();sun.intensity=1.45f;sun.color=new Color(1,.90f,.75f);sun.transform.rotation=Quaternion.Euler(46,-34,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.49f,.58f,.68f);RenderSettings.ambientEquatorColor=new Color(.47f,.41f,.32f);RenderSettings.ambientGroundColor=new Color(.28f,.24f,.19f);RenderSettings.fog=false;
            var overview=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="ArenaOverviewCamera");
            if(overview) { overview.transform.position=new Vector3(80,73,-90);overview.transform.LookAt(new Vector3(0,7,0)); }
            Physics.SyncTransforms();
            var surface=arena.GetComponent<NavMeshSurface>();var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Game/Navigation/RomanArenaNavMesh.asset");
            surface.BuildNavMesh();var generated=surface.navMeshData;surface.RemoveData();
            if(!generated) throw new InvalidOperationException("Expanded navigation bake failed");
            if(previous && previous!=generated) { EditorUtility.CopySerialized(generated,previous);Object.DestroyImmediate(generated);surface.navMeshData=previous;EditorUtility.SetDirty(previous); }
            surface.AddData();
            Validate(scene,arena);
            PrefabUtility.ApplyPrefabInstance(arena.gameObject,InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Review();
        }
        public static void Validate(Scene scene,Transform arena)
        {
            var report=new StringBuilder("Expanded arena — Unity "+Application.unityVersion+"\n");
            var points=arena.GetComponentsInChildren<ArenaSpawnPoint>();if(points.Length!=5) throw new InvalidOperationException("Original spawn component count changed");
            var player=points.Single(p=>p.Kind==ArenaSpawnKind.Player);var path=new NavMeshPath();
            foreach(var point in points)
            {
                if(!NavMesh.SamplePosition(point.transform.position,out var sample,.6f,NavMesh.AllAreas)) throw new InvalidOperationException("Spawn outside navigation: "+point.name);
                if(!NavMesh.CalculatePath(player.transform.position,sample.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete) throw new InvalidOperationException("Disconnected spawn: "+point.name);
                if(Physics.CheckCapsule(point.transform.position+Vector3.up*.6f,point.transform.position+Vector3.up*1.6f,.5f,~0,QueryTriggerInteraction.Ignore)) throw new InvalidOperationException("Spawn blocked: "+point.name);
                report.AppendLine("PASS spawn/clearance/route: "+point.name+" "+point.transform.position);
            }
            int samples=0;
            for(float x=-30;x<=30;x+=2) for(float z=-27;z<=27;z+=2)
            {
                if(x*x/(30*30)+z*z/(27*27)>1) continue;
                Vector3 p=new Vector3(x,.08f,z);
                if(!NavMesh.SamplePosition(p,out var hit,.35f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>.15f) throw new InvalidOperationException("Floor/navigation hole: "+p);
                if(!NavMesh.CalculatePath(player.transform.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete) throw new InvalidOperationException("Unreachable floor: "+p);
                samples++;
            }
            report.AppendLine("PASS: "+samples+" flat connected combat floor samples, 2m grid.");
            for(int angle=0;angle<360;angle+=2)
            {
                Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                var hits=Physics.RaycastAll(Vector3.up*1.2f,direction,48,~0,QueryTriggerInteraction.Ignore);
                if(!hits.Any(h=>h.collider.transform.IsChildOf(arena))) throw new InvalidOperationException("Open physical boundary at angle "+angle);
            }
            report.AppendLine("PASS: 180 radial physical boundary probes; podium, gates and tunnel ends contain the combat field.");
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing script: "+t.name);
            report.AppendLine("PASS: no missing scripts. Original gate controller, exit trigger, waves, camera and actor references retained.");
            report.AppendLine("Renderers "+arena.GetComponentsInChildren<Renderer>().Length+"; objects "+arena.GetComponentsInChildren<Transform>().Length+"; colliders "+arena.GetComponentsInChildren<Collider>().Length);
            report.AppendLine("NavMesh triangles "+NavMesh.CalculateTriangulation().indices.Length/3);
            report.AppendLine("Combined visible mesh vertices "+arena.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)(f.sharedMesh?f.sharedMesh.vertexCount:0)));
            report.AppendLine("Playable ellipse nominal 64 x 58m; podium inner face approximately 63.5 x 57.5m. Original podium inner diameter approximately 48m. Area gain approximately 58.5%.");
            File.WriteAllText(Evidence+"/GeometryValidation.txt",report.ToString());
        }
        [MenuItem("Bellator/Arena Expansion/3 Review and capture expanded arena")]
        public static void Review()
        {
            var scene=Open();var arena=ArenaRoot(scene);Validate(scene,arena);
            Render(scene,new Vector3(80,73,-90),new Vector3(0,7,0),"Overview.png");
            Render(scene,new Vector3(0,2.5f,-20),new Vector3(0,9,30),"ArenaInterior.png");
            Render(scene,new Vector3(20,4,10),new Vector3(-2,10,30),"ExitGate.png");
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,7,0),Quaternion.Euler(36,-34,0),75);
        }
        static void Render(Scene scene,Vector3 p,Vector3 target,string name)
        {
            bool wasDirty=scene.isDirty;
            var go=new GameObject("ArenaEvidenceCamera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.transform.position=p;camera.transform.LookAt(target);camera.fieldOfView=58;camera.farClipPlane=250;camera.nearClipPlane=.1f;camera.clearFlags=CameraClearFlags.Skybox;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+name,image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);if(!wasDirty) EditorSceneManager.SaveScene(scene); }
        }
    }
}
