using System;
using System.IO;
using System.Linq;
using Bellator.Arena;
using Bellator.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class PlayerStageBuilder
    {
        private const string Request = "Temp/BellatorPlayer.install";
        static PlayerStageBuilder() { EditorApplication.delayCall += Process; }
        private static void Process()
        {
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            try { Install(); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/BellatorPlayer.error.txt",e.ToString()); }
        }
        [MenuItem("Bellator/Install Stage 2 Player")]
        public static void Install()
        {
            Scene scene = SceneManager.GetSceneByPath(ArenaBuilder.ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ArenaBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            if (scene.GetRootGameObjects().Any(g=>g.GetComponent<ThirdPersonMotor>() != null))
                throw new InvalidOperationException("Player already installed. Existing player was preserved.");
            Directory.CreateDirectory("Assets/Game/Prefabs/Characters");
            Directory.CreateDirectory("Assets/Game/Materials/Characters");
            AssetDatabase.Refresh();
            var skin = Mat("Skin",new Color(.55f,.32f,.19f));
            var cloth = Mat("Linen",new Color(.63f,.54f,.36f));
            var belt = Mat("Leather",new Color(.19f,.09f,.04f));
            var hair = Mat("Hair",new Color(.09f,.065f,.04f));
            var root = new GameObject("Player");
            var spawn = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ArenaSpawnPoint>()).First(p=>p.Kind==ArenaSpawnKind.Player);
            root.transform.SetPositionAndRotation(spawn.transform.position,spawn.transform.rotation);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2.1f; controller.radius = .34f; controller.center = new Vector3(0,1.05f,0);
            controller.skinWidth = .03f; controller.stepOffset = .3f; controller.slopeLimit = 45; controller.minMoveDistance = 0;
            root.AddComponent<CharacterHealth>(); root.AddComponent<CharacterStamina>();
            var input = root.AddComponent<ThirdPersonInput>();
            var motor = root.AddComponent<ThirdPersonMotor>();
            var visuals = Parent("VisualRoot_Replaceable",root.transform);
            var body = Parent("BodyPivot",visuals);
            Shape("Torso",PrimitiveType.Capsule,body,new Vector3(0,1.27f,0),new Vector3(.58f,.39f,.34f),cloth);
            Shape("Belt",PrimitiveType.Cube,body,new Vector3(0,1.02f,0),new Vector3(.6f,.12f,.36f),belt);
            Shape("Neck",PrimitiveType.Cylinder,body,new Vector3(0,1.63f,0),new Vector3(.18f,.09f,.18f),skin);
            Shape("Head",PrimitiveType.Sphere,body,new Vector3(0,1.87f,0),new Vector3(.43f,.48f,.43f),skin);
            Shape("Hair",PrimitiveType.Sphere,body,new Vector3(0,2.015f,-.025f),new Vector3(.44f,.24f,.42f),hair);
            Shape("Nose_Forward",PrimitiveType.Cube,body,new Vector3(0,1.87f,.22f),new Vector3(.075f,.10f,.10f),skin);
            foreach(float x in new[]{-.085f,.085f})
                Shape("Eye",PrimitiveType.Sphere,body,new Vector3(x,1.94f,.19f),new Vector3(.045f,.045f,.04f),hair);
            var leftArm = Limb(body,"LeftShoulder",new Vector3(-.38f,1.55f,0),true,skin,belt);
            var rightArm = Limb(body,"RightShoulder",new Vector3(.38f,1.55f,0),true,skin,belt);
            var leftLeg = Limb(body,"LeftHip",new Vector3(-.17f,.94f,0),false,skin,belt);
            var rightLeg = Limb(body,"RightHip",new Vector3(.17f,.94f,0),false,skin,belt);
            visuals.gameObject.AddComponent<PrimitiveHumanoidAnimator>().Configure(motor,body,leftArm,rightArm,leftLeg,rightLeg);
            var navIgnore = root.AddComponent<Unity.AI.Navigation.NavMeshModifier>(); navIgnore.ignoreFromBuild = true;

            // Preserve the stage 1 overview as an inactive presentation camera.
            var overview = scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="ArenaOverviewCamera");
            if (overview != null) { overview.SetActive(false); overview.tag = "Untagged"; }
            var cameraRoot = new GameObject("PlayerCamera");
            cameraRoot.tag = "MainCamera";
            var camera = cameraRoot.AddComponent<Camera>(); camera.fieldOfView = 60; camera.nearClipPlane = .08f; camera.farClipPlane = 220;
            camera.clearFlags = CameraClearFlags.Skybox; camera.backgroundColor = new Color(.46f,.60f,.72f);
            cameraRoot.AddComponent<AudioListener>();
            var cameraData = cameraRoot.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); cameraData.renderShadows = true;
            var orbit = cameraRoot.AddComponent<ThirdPersonCamera>(); orbit.Configure(root.transform,input);
            motor.Configure(camera.transform);
            orbit.UpdateOrbit(Vector2.zero,0,.016f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,"Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(cameraRoot,"Assets/Game/Prefabs/Characters/ThirdPersonCamera.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Render(camera);
            File.WriteAllText("Temp/BellatorPlayer.installed.txt","Stage 2 installed; original arena prefab preserved.");
            Debug.Log("Bellator: Stage 2 player installed in RomanArena.");
        }
        private static Transform Parent(string name,Transform parent) { var t = new GameObject(name).transform; t.SetParent(parent,false); return t; }
        private static Transform Limb(Transform body,string name,Vector3 position,bool arm,Material skin,Material leather)
        {
            var pivot = Parent(name,body); pivot.localPosition = position;
            Shape(arm?"Arm":"Leg",PrimitiveType.Capsule,pivot,new Vector3(0,arm?-.30f:-.36f,0),new Vector3(arm?.18f:.22f,arm?.30f:.36f,arm?.18f:.22f),skin);
            Shape(arm?"Hand":"Foot",arm?PrimitiveType.Sphere:PrimitiveType.Cube,pivot,new Vector3(0,arm?-.65f:-.82f,arm?0:.09f),arm?new Vector3(.20f,.22f,.19f):new Vector3(.25f,.16f,.43f),arm?skin:leather);
            return pivot;
        }
        private static void Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var g = GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false);
            g.transform.localPosition=position; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        private static Material Mat(string name,Color color)
        {
            string path = "Assets/Game/Materials/Characters/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null) return existing;
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name; m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(m,path); return m;
        }
        private static void Render(Camera camera)
        {
            var rt=new RenderTexture(1280,800,24); var previous=RenderTexture.active; camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1280,800),0,0); texture.Apply();
            File.WriteAllBytes("Assets/Game/Documentation/PlayerPreview.png",texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
        }
    }
}

