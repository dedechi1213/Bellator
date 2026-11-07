using System;
using System.IO;
using System.Linq;
using Bellator.Arena;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    /// <summary>Creates persistent primitive assets; never builds the arena at runtime.</summary>
    [InitializeOnLoad]
    public static class ArenaBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/RomanArena.unity";
        private const string Request = "Temp/BellatorArena.request";
        private static Material sand, stone, paleStone, darkStone, wood, metal, red;
        static ArenaBuilder() { EditorApplication.delayCall += ProcessRequest; EditorApplication.delayCall += ProcessPresentationRequest; }

        private static void ProcessRequest()
        {
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            if (File.Exists(ScenePath)) return; // Never replace an existing arena automatically.

            try { Build(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorArena.error.txt", e.ToString()); }
        }

        [MenuItem("Bellator/Create Roman Arena (new scene only)")]
        public static void Build()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Arena already exists. Edit the scene or move it before generating a new version.");
            foreach (string folder in new[] { "Scenes", "Prefabs", "Materials", "UI", "ScriptableObjects", "Navigation", "Documentation" })
                Directory.CreateDirectory("Assets/Game/" + folder);
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            sand = Material("ArenaSand", new Color(.72f,.55f,.32f));
            stone = Material("Travertine", new Color(.57f,.47f,.34f));
            paleStone = Material("CarvedLimestone", new Color(.79f,.69f,.51f));
            darkStone = Material("WeatheredStone", new Color(.34f,.30f,.24f));
            wood = Material("AgedTimber", new Color(.27f,.13f,.055f));
            metal = Material("ForgedIron", new Color(.12f,.14f,.15f), .65f);
            red = Material("RomanOxblood", new Color(.38f,.045f,.035f));

            Transform arena = Parent("Arena", null);
            Transform ground = Parent("Ground", arena), walls = Parent("Walls", arena), stands = Parent("Stands", arena);
            Transform gates = Parent("Gates", arena), obstacles = Parent("Obstacles", arena);
            Transform lighting = Parent("Lighting", arena), spawns = Parent("SpawnPoints", arena);
            Primitive("SandFloor", PrimitiveType.Cylinder, ground, new Vector3(0,-.3f,0), new Vector3(48.5f,.3f,48.5f), sand);
            var foundation = Primitive("ArenaFoundation", PrimitiveType.Cylinder, ground, new Vector3(0,-.9f,0), new Vector3(73,.3f,73), darkStone);
            foundation.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            NotWalkable(stands.gameObject);
            NotWalkable(walls.gameObject);

            for (int i = 0; i < 72; i++)
            {
                float angle = i * 5 + 2.5f;
                bool opening = Opening(angle);
                if (!opening)
                {
                    BlockOnRing("PodiumWall_" + i, walls, angle, 24.8f, 3, 6, 1.6f, stone);
                    BlockOnRing("WallCoping_" + i, walls, angle, 24.8f, 6.1f, .35f, 1.9f, paleStone);
                    BlockOnRing("WallFoot_" + i, walls, angle, 24.55f, .3f, .6f, 1.9f, darkStone);
                    for (int row = 0; row < 7; row++)
                        BlockOnRing($"Seating_{i:00}_{row}", stands, angle, 26.1f + row * 1.25f, 2.6f + row * .68f, 5.2f + row * 1.36f, 1.32f, row % 3 == 0 ? paleStone : stone);
                }
                BlockOnRing("OuterWall_" + i, walls, angle, 35.3f, 5.5f, 12.5f, 1.2f, stone);
                BlockOnRing("Cornice_" + i, walls, angle, 35.3f, 12, .6f, 1.8f, paleStone);
                if (i % 3 == 0)
                {
                    Column(stands, angle, 34.65f, 10.8f);
                    BlockOnRing("Entablature_" + i, stands, angle, 34.65f, 15.5f, .6f, 1.4f, paleStone, 6.15f);
                    if (i % 6 == 0)
                        Primitive("OxbloodBanner_" + i, PrimitiveType.Cube, stands, Ring(angle,34.0f,11.8f), new Vector3(1.1f,2.6f,.1f), red, angle, false);
                }
            }
            Gate(gates, ground, 0, true);
            foreach (float angle in new[] {45f,135f,225f,315f}) Gate(gates, ground, angle, false);

            Spawn(spawns, "PlayerSpawn", new Vector3(0,.08f,-12), ArenaSpawnKind.Player);
            foreach (float angle in new[] {45f,135f,225f,315f})
                Spawn(spawns, "EnemySpawn_" + angle, Ring(angle,22.1f,.08f), ArenaSpawnKind.Enemy);
            foreach (Vector3 p in new[] {new Vector3(-9,0,-4), new Vector3(10,0,4), new Vector3(-7,0,9), new Vector3(8,0,-10)})
            {
                Transform socket = Parent("Obstacle_" + obstacles.childCount, obstacles);
                socket.localPosition = p;
                Transform visual = Parent("Visuals_Replaceable", socket);
                Primitive("StonePlinth", PrimitiveType.Cube, visual, new Vector3(0,.45f,0), new Vector3(2.6f,.9f,2), darkStone);
                Primitive("BrokenColumn", PrimitiveType.Cylinder, visual, new Vector3(0,1.45f,0), new Vector3(1.35f,.7f,1.35f), paleStone);
                Primitive("Capital", PrimitiveType.Cube, visual, new Vector3(0,2.2f,0), new Vector3(1.6f,.2f,1.6f), stone);
            }
            foreach (Vector3 p in new[] {new Vector3(-13,0,4),new Vector3(13,0,-3)})
            {
                Transform socket = Parent("TimberBarricade_" + obstacles.childCount, obstacles); socket.localPosition = p;
                Transform visual = Parent("Visuals_Replaceable", socket);
                Primitive("TimberBody", PrimitiveType.Cube, visual, new Vector3(0,.65f,0), new Vector3(3,1.3f,1.1f), wood);
                for (int j = -1; j <= 1; j += 2)
                    Primitive("IronStrap", PrimitiveType.Cube, visual, new Vector3(j,.65f,0), new Vector3(.14f,1.38f,1.16f), metal, 0, false);
            }

            var sun = new GameObject("Sun").AddComponent<Light>(); sun.transform.SetParent(lighting);
            sun.type = LightType.Directional; sun.color = new Color(1,.88f,.69f); sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .85f; sun.transform.rotation = Quaternion.Euler(48,-32,0);
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.47f,.58f,.7f);
            RenderSettings.ambientEquatorColor = new Color(.42f,.37f,.29f);
            RenderSettings.ambientGroundColor = new Color(.25f,.21f,.16f);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            var camera = new GameObject("ArenaOverviewCamera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(50,48,-60); camera.transform.LookAt(new Vector3(0,2,0));
            camera.fieldOfView = 52; camera.nearClipPlane = .3f; camera.farClipPlane = 220;
            camera.backgroundColor = new Color(.46f,.60f,.72f); camera.clearFlags = CameraClearFlags.Skybox;
            camera.gameObject.AddComponent<AudioListener>();

            var surface = arena.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .15f; Physics.SyncTransforms(); surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh did not build.");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Game/Navigation/RomanArenaNavMesh.asset");
            Validate(arena, surface);
            PrefabUtility.SaveAsPrefabAssetAndConnect(arena.gameObject, "Assets/Game/Prefabs/RomanArena.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            RenderPreview(camera);
            // Preserve the user's existing scenes and their unsaved edits. Arena stays loaded additively.
            if (previous.IsValid() && previous.isDirty) SceneManager.SetActiveScene(previous);
            File.WriteAllText("Temp/BellatorArena.complete.txt", "Created " + ScenePath + "\n" + DateTime.Now);
            Debug.Log("Bellator: arena created, NavMesh and spawn routes validated. Open " + ScenePath);
        }

        private static void ProcessPresentationRequest()
        {
            const string request = "Temp/BellatorArena.presentation";
            if (!File.Exists(request)) return;
            File.Delete(request);
            try { ReviewPresentation(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorArena.presentation-error.txt",e.ToString()); }
        }

        [MenuItem("Bellator/Validate and Preview Open Arena")]
        public static void ReviewPresentation()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            // Close the unchanged template scene to prevent its sun/camera affecting the arena.
            Scene template = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (template.isLoaded && !template.isDirty) EditorSceneManager.CloseScene(template,true);
            var arena = scene.GetRootGameObjects().First(g=>g.name == "Arena");
            var sun = arena.transform.Find("Lighting/Sun").GetComponent<Light>();
            sun.intensity = 1.25f;
            if (sun.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>() == null)
                sun.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            var camera = scene.GetRootGameObjects().First(g=>g.name == "ArenaOverviewCamera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.20f,.25f,.29f);
            var data = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null) data = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderShadows = true;
            Physics.SyncTransforms();
            Validate(arena.transform,arena.GetComponent<NavMeshSurface>());
            PrefabUtility.ApplyPrefabInstance(arena,InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            RenderPreview(camera);
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(new Vector3(0,2,0),Quaternion.Euler(48,-36,0),55);
            File.WriteAllText("Temp/BellatorArena.reviewed.txt","Presentation and saved-scene navigation verified.");
        }
        private static Material Material(string name, Color color, float metallic = 0)
        {
            string path = "Assets/Game/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .12f); m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m,path); return m;
        }
        private static Transform Parent(string name, Transform parent)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
        private static Vector3 Ring(float angle,float radius,float y) => Quaternion.Euler(0,angle,0) * new Vector3(0,y,radius);
        private static bool Opening(float angle) => Mathf.Abs(Mathf.DeltaAngle(angle,0)) < 7.6f || new[] {45f,135f,225f,315f}.Any(a => Mathf.Abs(Mathf.DeltaAngle(angle,a)) < 5.1f);
        private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material m,float yaw = 0,bool collision = true)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent,false);
            g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0,yaw,0); g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (type == PrimitiveType.Cylinder)
            {
                UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                if (collision) g.AddComponent<MeshCollider>().sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
            }
            else if (!collision) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        private static void BlockOnRing(string name,Transform parent,float a,float r,float y,float h,float depth,Material m,float width = 0)
            => Primitive(name,PrimitiveType.Cube,parent,Ring(a,r,y),new Vector3(width > 0 ? width : 2*r*Mathf.Tan(2.5f*Mathf.Deg2Rad)+.09f,h,depth),m,a);
        private static void NotWalkable(GameObject g)
        {
            var modifier = g.AddComponent<NavMeshModifier>(); modifier.overrideArea = true; modifier.area = 1;
        }
        private static void Column(Transform parent,float a,float r,float y)
        {
            var t = Parent("RomanColumn_" + a,parent); t.localPosition = Ring(a,r,y);
            Primitive("Base",PrimitiveType.Cube,t,Vector3.zero,new Vector3(1.5f,.4f,1.5f),paleStone);
            Primitive("Shaft",PrimitiveType.Cylinder,t,new Vector3(0,2,0),new Vector3(.85f,1.9f,.85f),paleStone);
            Primitive("Capital",PrimitiveType.Cube,t,new Vector3(0,4,0),new Vector3(1.5f,.45f,1.5f),paleStone);
        }
        private static void Spawn(Transform parent,string name,Vector3 p,ArenaSpawnKind kind)
        {
            var t = Parent(name,parent); t.localPosition = p; t.LookAt(new Vector3(0,p.y,0));
            t.gameObject.AddComponent<ArenaSpawnPoint>().Configure(kind);
        }
        private static void Gate(Transform parent,Transform ground,float a,bool main)
        {
            var t = Parent(main ? "MainExitGate" : "EnemyEntrance_" + a,parent);
            t.localPosition = Ring(a,24.8f,0); t.localRotation = Quaternion.Euler(0,a,0);
            float width = main ? 6.4f : 4.2f;
            Primitive("PassageFloor",PrimitiveType.Cube,ground,Ring(a,27.3f,-.2f),new Vector3(width,.4f,6.3f),sand,a);
            for(int side = -1;side <= 1;side += 2)
            {
                Primitive("PortalPier",PrimitiveType.Cube,t,new Vector3(side*(width/2+.48f),3.4f,0),new Vector3(.95f,6.8f,2.4f),paleStone);
                Primitive("TunnelSide",PrimitiveType.Cube,t,new Vector3(side*(width/2+.48f),2.5f,3.6f),new Vector3(.95f,5,6),darkStone);
                Primitive("PierCapital",PrimitiveType.Cube,t,new Vector3(side*(width/2+.48f),6.7f,0),new Vector3(1.25f,.4f,2.7f),paleStone);
            }
            Primitive("PortalLintel",PrimitiveType.Cube,t,new Vector3(0,6.35f,0),new Vector3(width,1,2.4f),stone);
            // A semicircular arch of cuboid voussoirs, entirely made from Unity cubes.
            for(int i = 0;i < 11;i++)
            {
                float theta = i*Mathf.PI/10;
                var block = Primitive("ArchStone_" + i,PrimitiveType.Cube,t,new Vector3(Mathf.Cos(theta)*width*.48f,4.5f+Mathf.Sin(theta)*width*.24f,-1.25f),new Vector3(.65f,.65f,.4f),paleStone,0,false);
                block.transform.localRotation = Quaternion.Euler(0,0,theta*Mathf.Rad2Deg);
            }
            Primitive("TunnelBackBoundary",PrimitiveType.Cube,t,new Vector3(0,2.5f,6.2f),new Vector3(width,5,.65f),darkStone);
            Primitive("TunnelRoof",PrimitiveType.Cube,t,new Vector3(0,5.3f,3.6f),new Vector3(width,.6f,6),stone);
            if(main)
            {
                var moving = Parent("MovingGate",t); moving.localPosition = new Vector3(0,2.8f,0);
                Primitive("SolidTimberGate",PrimitiveType.Cube,moving,Vector3.zero,new Vector3(width,5.6f,.5f),wood);
                for(int i = -3;i <= 3;i++) Primitive("IronUpright_" + i,PrimitiveType.Cube,moving,new Vector3(i*.87f,0,-.3f),new Vector3(.13f,5.65f,.17f),metal,0,false);
                foreach(float y in new[]{-1.9f,0,1.9f}) Primitive("IronCrossbar",PrimitiveType.Cube,moving,new Vector3(0,y,-.32f),new Vector3(width,.18f,.2f),metal,0,false);
                var mod = moving.gameObject.AddComponent<NavMeshModifier>(); mod.ignoreFromBuild = true;
                var carve = moving.gameObject.AddComponent<NavMeshObstacle>(); carve.shape = NavMeshObstacleShape.Box;
                carve.size = new Vector3(width,5.6f,.65f); carve.carving = true; carve.carveOnlyStationary = false;
                t.gameObject.AddComponent<MainGateController>().Configure(moving);
                Primitive("GateAttic",PrimitiveType.Cube,t,new Vector3(0,8.6f,0),new Vector3(width+2.7f,3.3f,2.4f),stone);
                Primitive("ExitCrest",PrimitiveType.Sphere,t,new Vector3(0,8.5f,-1.3f),new Vector3(1.4f,1.4f,.25f),metal,0,false);
            }
        }
        private static void Validate(Transform arena,NavMeshSurface surface)
        {
            var points = arena.GetComponentsInChildren<ArenaSpawnPoint>();
            if(points.Length != 5) throw new InvalidOperationException("Expected five spawn points.");
            if(!NavMesh.SamplePosition(points.First(p=>p.Kind==ArenaSpawnKind.Player).transform.position,out var player,1,NavMesh.AllAreas))
                throw new InvalidOperationException("Player spawn outside NavMesh.");
            foreach(var point in points)
            {
                if(!NavMesh.SamplePosition(point.transform.position,out var hit,1,NavMesh.AllAreas)) throw new InvalidOperationException(point.name + " outside NavMesh.");
                var path = new NavMeshPath();
                if(!NavMesh.CalculatePath(player.position,hit.position,NavMesh.AllAreas,path) || path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Disconnected spawn: " + point.name);
                if(Physics.OverlapCapsule(point.transform.position+Vector3.up*.65f,point.transform.position+Vector3.up*1.5f,.45f).Any(c => c.transform.IsChildOf(arena)))
                    throw new InvalidOperationException("Spawn intersects collider: " + point.name);
            }
            File.WriteAllText("Assets/Game/Documentation/Validation.txt", "Unity " + Application.unityVersion + "\nPrimitive renderers: " + arena.GetComponentsInChildren<Renderer>().Length + "\nSpawns: 5; all sampled on NavMesh; all routes complete; clearance checked.\nNavMesh triangles: " + NavMesh.CalculateTriangulation().indices.Length/3 + "\nMain gate: closed, solid collider; carving obstacle; Open/Close available.\n");
        }
        private static void RenderPreview(Camera camera)
        {
            var rt = new RenderTexture(1600,1000,24); camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(1600,1000,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/ArenaPreview.png",image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.Refresh();
        }
    }
}




