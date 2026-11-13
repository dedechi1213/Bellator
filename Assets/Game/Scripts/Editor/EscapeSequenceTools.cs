using System;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Arena;
using Bellator.Match;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class EscapeSequenceTools
    {
        public const string Root="Assets/Game/StoryEscape",Evidence="Assets/Game/Documentation/Escape";
        static EscapeSequenceTools() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/EscapeSequence.request")) return;
            string action=File.ReadAllText("Temp/EscapeSequence.request").Trim(); File.Delete("Temp/EscapeSequence.request");
            try
            {
                if(action=="audit") Audit(); else if(action=="build") Build(); else if(action=="review") Review();
                else throw new Exception("Unknown escape request "+action);
                File.WriteAllText("Logs/Escape-"+action+".txt","PASS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex) { File.WriteAllText("Logs/Escape-"+action+".txt","FAIL\n"+ex); Debug.LogException(ex); }
        }
        static Scene Open()
        {
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save scene edits before installing the physical exit: "+SceneManager.GetSceneAt(i).path);
            var scene=SceneManager.GetSceneByPath(PlayerClassController.ArenaScene);
            if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(PlayerClassController.ArenaScene);
            SceneManager.SetActiveScene(scene); return scene;
        }
        static Transform Gate(Scene scene) => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MainGateController>(true)).Single().transform;
        public static void Audit()
        {
            var scene=Open(); var gate=Gate(scene); Directory.CreateDirectory(Evidence);
            Physics.SyncTransforms(); var report=new StringBuilder("PHYSICAL ESCAPE / before\n");
            report.AppendLine("Gate "+Path(gate)+" at "+gate.position+" rotation "+gate.eulerAngles);
            foreach(var hit in Physics.SphereCastAll(gate.TransformPoint(new Vector3(0,1.4f,-2)),.4f,gate.forward,55,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                report.AppendLine("Center route hit: "+Path(hit.collider.transform)+" at "+hit.point+" distance "+hit.distance);
            foreach(var t in gate.GetComponentsInChildren<Transform>()) if(t.name.Contains("Tunnel") || t.name.Contains("Gate")) report.AppendLine("Gate part "+Path(t)+" local "+t.localPosition+" scale "+t.localScale);
            File.WriteAllText(Evidence+"/BeforeAudit.txt",report.ToString());
            Render(scene,gate.TransformPoint(new Vector3(11,5,-18)),gate.TransformPoint(new Vector3(0,4,2)),"BeforeGate.png");
        }
        static string Path(Transform t) => t.parent!=null?Path(t.parent)+"/"+t.name:t.name;
        static Transform Group(Transform parent,string name,Vector3 position)
        { var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=position; return t; }
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/ArenaExpansion/Materials/"+name+".mat");
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=p; g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material; if(!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        static void Column(Transform parent,float x,float z,Material stone,Material trim,float height=5.7f)
        {
            Box(parent,"ColumnFoot",new Vector3(x,.2f,z),new Vector3(.95f,.4f,.95f),stone);
            var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder); shaft.name="RomanColumn"; shaft.transform.SetParent(parent,false);
            shaft.transform.localPosition=new Vector3(x,height*.5f,z); shaft.transform.localScale=new Vector3(.65f,(height-.5f)*.5f,.65f); shaft.GetComponent<Renderer>().sharedMaterial=trim;
            Box(parent,"ColumnCapital",new Vector3(x,height-.2f,z),new Vector3(1,.4f,1),trim);
        }
        static void Arch(Transform parent,float z,Material stone)
        {
            for(int i=0;i<15;i++)
            {
                float angle=(i+.5f)*Mathf.PI/15;
                var g=Box(parent,"VaultVoussoir",new Vector3(Mathf.Cos(angle)*3.5f,3.3f+Mathf.Sin(angle)*2.4f,z),new Vector3(.62f,.8f,.7f),stone);
                g.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            }
        }
        static AudioSource Audio(Transform parent,string name,AudioClip clip,bool spatial)
        {
            var t=Group(parent,name,Vector3.zero); var source=t.gameObject.AddComponent<AudioSource>(); source.clip=clip; source.playOnAwake=false; source.loop=true;
            source.spatialBlend=spatial?.8f:0; source.minDistance=5; source.maxDistance=85; source.volume=.2f; return source;
        }
        static AudioClip Sound(string name,float duration,int kind)
        {
            string path=Root+"/Audio/"+name+".wav"; if(File.Exists(path)) return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            int rate=22050,count=(int)(duration*rate); var random=new System.Random(123+kind); float low=0;
            using(var file=new BinaryWriter(File.Create(path)))
            {
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(36+count*2); file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16); file.Write((short)1); file.Write((short)1); file.Write(rate); file.Write(rate*2); file.Write((short)2); file.Write((short)16);
                file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    float t=(float)i/rate,noise=(float)random.NextDouble()*2-1; low=low*.97f+noise*.03f; float value;
                    if(kind==0)
                    {
                        float envelope=Mathf.Sin(Mathf.PI*t/duration);
                        value=(low*1.4f+Mathf.Sin(2*Mathf.PI*(95*t+20*t*t))*.12f+noise*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*29)),12)*.17f)*envelope;
                    }
                    else if(kind==1) value=low*.65f+Mathf.Sin(t*43)*Mathf.Sin(t*.9f)*.008f;
                    else
                    {
                        float birdTime=Mathf.Repeat(t,3.8f); float chirp=birdTime<.4f?Mathf.Sin(Mathf.PI*birdTime/.4f)*Mathf.Sin(2*Mathf.PI*(1650*birdTime+900*birdTime*birdTime))*.14f:0;
                        value=low*.5f+chirp;
                    }
                    file.Write((short)(Mathf.Clamp(value,-1,1)*22000));
                }
            }
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("Bellator/Escape/Install physical exit and victory sequence")]
        public static void Build()
        {
            var scene=Open(); var gate=Gate(scene);
            if(scene.GetRootGameObjects().Any(g=>g.name=="StoryEscapePassage")) throw new Exception("Escape passage already exists; use review to preserve authored changes.");
            Directory.CreateDirectory(Root+"/Geometry"); Directory.CreateDirectory(Root+"/Audio"); Directory.CreateDirectory(Evidence);
            var arena=scene.GetRootGameObjects().Single(g=>g.name=="Arena").transform;
            // Scene-only unpacking prevents modifying the shared arena used by OnlineArena.
            if(PrefabUtility.IsPartOfPrefabInstance(arena)) PrefabUtility.UnpackPrefabInstance(arena.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var volume=new Bounds(new Vector3(0,3.35f,16.75f),new Vector3(9.2f,7.9f,34.5f));
            var worldBounds=new Bounds(gate.TransformPoint(volume.center),volume.size);
            var report=new StringBuilder("Physical exit migration\n"); int cutIndex=0;
            foreach(var filter in arena.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh==null || filter.transform.IsChildOf(gate) || Path(filter.transform).Contains("/Ground/")) continue;
                var renderer=filter.GetComponent<Renderer>(); if(renderer==null || !renderer.bounds.Intersects(worldBounds)) continue;
                var mesh=EscapeMeshCut.Subtract(filter.sharedMesh,filter.transform,gate,volume);
                if(mesh.vertexCount==0) { report.AppendLine("Removed geometry inside passage: "+Path(filter.transform)); Object.DestroyImmediate(filter.gameObject); Object.DestroyImmediate(mesh); continue; }
                string path=Root+"/Geometry/PassageCut_"+(cutIndex++).ToString("000")+".asset";
                AssetDatabase.CreateAsset(mesh,path); filter.sharedMesh=mesh;
                bool collision=filter.GetComponents<Collider>().Length>0;
                foreach(var collider in filter.GetComponents<Collider>()) Object.DestroyImmediate(collider);
                if(collision) filter.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
                report.AppendLine("Cut actual mesh "+Path(filter.transform)+" / collider rebuilt="+collision);
            }
            var back=gate.Find("TunnelBackBoundary"); if(back!=null) { report.AppendLine("Removed closed tunnel end "+Path(back)); Object.DestroyImmediate(back.gameObject); }
            var stone=Mat("WarmTravertine"); var trim=Mat("IvoryLimestone"); var dark=Mat("ShadowStone"); var wood=Mat("GateTimber"); var iron=Mat("GateIron"); var gold=Mat("AntiqueBronze");
            var moving=gate.Find("MovingGate");
            for(int i=moving.childCount-1;i>=0;i--) Object.DestroyImmediate(moving.GetChild(i).gameObject);
            for(int i=0;i<12;i++)
            {
                var plank=Box(moving,"ReinforcedOakPlank",new Vector3(-2.93f+i*.533f,0,0),new Vector3(.525f,5.6f,.48f),wood);
                for(int band=0;band<3;band++)
                {
                    var bolt=GameObject.CreatePrimitive(PrimitiveType.Sphere); bolt.name="IronRivet"; bolt.transform.SetParent(moving,false);
                    bolt.transform.localPosition=new Vector3(-2.93f+i*.533f,-1.9f+band*1.9f,-.34f); bolt.transform.localScale=new Vector3(.12f,.12f,.07f); bolt.GetComponent<Renderer>().sharedMaterial=iron; Object.DestroyImmediate(bolt.GetComponent<Collider>());
                }
            }
            foreach(float y in new[]{-1.9f,0,1.9f}) Box(moving,"ForgedIronBand",new Vector3(0,y,-.29f),new Vector3(6.4f,.22f,.16f),iron,false);
            Box(moving,"BronzeLockPlate",new Vector3(0,-.35f,-.4f),new Vector3(.55f,.75f,.16f),gold,false);
            var passage=new GameObject("StoryEscapePassage").transform; SceneManager.MoveGameObjectToScene(passage.gameObject,scene); passage.SetPositionAndRotation(gate.position,gate.rotation);
            Box(passage,"ContinuousStoneFloor",new Vector3(0,-.2f,19),new Vector3(8.4f,.4f,38),stone);
            foreach(float x in new[]{-4.2f,4.2f}) Box(passage,"StoneCorridorWall",new Vector3(x,3.15f,18),new Vector3(.8f,6.3f,23.5f),stone);
            Box(passage,"VaultRoof",new Vector3(0,6.35f,18),new Vector3(9.2f,.6f,24),dark);
            for(float z=8;z<=29;z+=7)
            {
                foreach(float x in new[]{-3.45f,3.45f}) Column(passage,x,z,stone,trim);
                Arch(passage,z,trim);
                foreach(float x in new[]{-3.72f,3.72f})
                {
                    Box(passage,"TorchSconce",new Vector3(x,2.7f,z+.6f),new Vector3(.18f,.65f,.28f),iron,false);
                    var flame=GameObject.CreatePrimitive(PrimitiveType.Sphere); flame.name="TorchFlame"; flame.transform.SetParent(passage,false); flame.transform.localPosition=new Vector3(x,3.2f,z+.6f); flame.transform.localScale=new Vector3(.18f,.38f,.18f); Object.DestroyImmediate(flame.GetComponent<Collider>());
                    flame.GetComponent<Renderer>().sharedMaterial=gold;
                    var light=flame.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.55f,.23f); light.intensity=1.4f; light.range=7; light.shadows=LightShadows.None;
                }
            }
            // Simple open-air terrace beyond the original outer facade, no city loading or backdrop teleport.
            Box(passage,"ExteriorTerrace",new Vector3(0,-.23f,44),new Vector3(34,.46f,30),stone);
            for(int z=32;z<=55;z+=5) for(int x=-12;x<=12;x+=4) Box(passage,"ExteriorPavingJoint",new Vector3(x,.005f,z),new Vector3(3.92f,.015f,4.92f),trim,false);
            foreach(float x in new[]{-12f,12f})
            {
                for(int z=33;z<=54;z+=7) Column(passage,x,z,stone,trim,6.5f);
                Box(passage,"TerraceBalustrade",new Vector3(x,1,46),new Vector3(.6f,2,25),stone);
                Box(passage,"ExteriorEntablature",new Vector3(x,6.6f,44),new Vector3(1.4f,.6f,25),trim);
            }
            foreach(float x in new[]{-8f,8f}) { Column(passage,x,32,stone,trim,7); Box(passage,"ExteriorBanner",new Vector3(x,4,32),new Vector3(1.2f,2.4f,.08f),Mat("ImperialOxblood"),false); }
            var insideLight=Group(passage,"WarmTunnelBounce",new Vector3(0,3,16)).gameObject.AddComponent<Light>(); insideLight.type=LightType.Point; insideLight.color=new Color(1,.72f,.43f); insideLight.intensity=.6f; insideLight.range=19;
            var outsideLight=Group(passage,"DaylightAtExit",new Vector3(0,6,35)).gameObject.AddComponent<Light>(); outsideLight.type=LightType.Point; outsideLight.color=new Color(.76f,.88f,1); outsideLight.intensity=1.7f; outsideLight.range=30;
            var gateAudio=Audio(gate,"GateMechanismAudio",Sound("ReinforcedGateLift",3.2f,0),true); gateAudio.transform.localPosition=new Vector3(0,3,0); gateAudio.loop=false; gateAudio.volume=.8f;
            gate.GetComponent<MainGateController>().ConfigureSound(gateAudio,gateAudio.clip);
            var crowd=Audio(passage,"ArenaMurmur",Sound("ArenaMurmur",12,1),false);
            var outside=Audio(passage,"OutsideWindAndBirds",Sound("OutsideWindAndBirds",15.2f,2),false);
            var game=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameManager>(true)).Single();
            var gameProperties=new SerializedObject(game);
            var beacon=gameProperties.FindProperty("exitBeacon").objectReferenceValue as GameObject;
            if(beacon!=null)
            {
                beacon.transform.SetPositionAndRotation(gate.TransformPoint(new Vector3(0,8.5f,-1.4f)),gate.rotation);
                var green=beacon.GetComponent<Light>(); if(green==null) green=beacon.AddComponent<Light>();
                green.type=LightType.Point; green.color=new Color(.2f,1,.45f); green.intensity=2; green.range=7; green.shadows=LightShadows.None;
            }
            var sequence=game.gameObject.AddComponent<EscapeVictorySequence>(); sequence.Configure(gate,crowd,outside,insideLight,outsideLight);
            var exit=game.Exit; var serialized=new SerializedObject(exit); serialized.FindProperty("crossingDepth").floatValue=4.2f; serialized.ApplyModifiedPropertiesWithoutUndo();
            var trigger=exit.GetComponent<BoxCollider>(); if(trigger!=null) { trigger.center=new Vector3(0,1.5f,3.5f); trigger.size=new Vector3(6,3.5f,6); }
            EditorUtility.SetDirty(game); EditorUtility.SetDirty(gate.GetComponent<MainGateController>());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/GeometryChanges.txt",report.ToString()); Review();
        }
        [MenuItem("Bellator/Escape/Review physical exit")]
        public static void Review()
        {
            var scene=Open(); var gate=Gate(scene); Physics.SyncTransforms();
            Render(scene,gate.TransformPoint(new Vector3(10,5,-18)),gate.TransformPoint(new Vector3(0,4,1)),"ClosedGate.png");
            Render(scene,gate.TransformPoint(new Vector3(1.2f,2.2f,9)),gate.TransformPoint(new Vector3(0,2.1f,34)),"PhysicalCorridor.png");
            Render(scene,gate.TransformPoint(new Vector3(9,7.4f,53)),gate.TransformPoint(new Vector3(0,4,27)),"Exterior.png");
        }
        static void Render(Scene scene,Vector3 p,Vector3 target,string name)
        {
            Directory.CreateDirectory(Evidence); bool wasDirty=scene.isDirty;
            var go=new GameObject("EscapeEvidenceCamera"); SceneManager.MoveGameObjectToScene(go,scene); var camera=go.AddComponent<Camera>(); camera.transform.position=p; camera.transform.LookAt(target); camera.fieldOfView=62; camera.farClipPlane=250; camera.nearClipPlane=.1f; camera.clearFlags=CameraClearFlags.Skybox;
            var rt=new RenderTexture(1280,800,24); var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                var image=new Texture2D(1280,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
                File.WriteAllBytes(Evidence+"/"+name,image.EncodeToPNG()); Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture=null; RenderTexture.active=previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(go); if(!wasDirty) EditorSceneManager.SaveScene(scene); }
        }
    }
}
