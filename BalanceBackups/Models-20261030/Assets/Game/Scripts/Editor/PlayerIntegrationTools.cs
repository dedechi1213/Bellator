using System;
using System.IO;
using System.Linq;
using Bellator.AnimationPreview;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Bellator.Editor
{
    public static class PlayerIntegrationTools
    {
        public const string Root="Assets/Game/Animations/SoulslikeGameplay";
        static readonly string[] Classes={"Survivor","Colossus","Warrior","Hunter"};
        [MenuItem("Bellator/Player Stage 2/Connect gameplay visuals and controllers")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before updating assets");
            Directory.CreateDirectory(Root+"/Controllers"); Directory.CreateDirectory(Root+"/Visuals"); Directory.CreateDirectory(Root+"/Evidence"); AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<AnimationPreviewCatalog>(AnimationStageBuilder.Root+"/PreviewCatalog.asset");
            foreach(string name in Classes)
            {
                string path=Root+"/Controllers/"+name+"Gameplay.controller";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if(controller==null)
                {
                    if(!AssetDatabase.CopyAsset(AnimationStageBuilder.Root+"/Controllers/"+name+".controller",path)) throw new InvalidOperationException("Cannot copy "+name+" controller");
                    controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                }
                if(!controller.parameters.Any(p=>p.name=="LocomotionRate")) controller.AddParameter("LocomotionRate",AnimatorControllerParameterType.Float);
                var parameters=controller.parameters; foreach(var p in parameters) if(p.name=="LocomotionRate") p.defaultFloat=1; controller.parameters=parameters;
                foreach(var child in controller.layers[0].stateMachine.states)
                {
                    var state=child.state; state.speed=1;
                    if(state.motion is BlendTree) { state.speedParameter="LocomotionRate"; state.speedParameterActive=true; }
                    else foreach(var transition in state.transitions.ToArray()) state.RemoveTransition(transition);
                }
                var definition=AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+name+".asset");
                var source=catalog.characters.Single(c=>c.prefab.name.StartsWith(name,StringComparison.Ordinal)).prefab;
                var temporary=UnityEngine.Object.Instantiate(source);
                var root=new GameObject(name+"AnimatedVisual");
                try
                {
                    var animator=temporary.GetComponentInChildren<Animator>();
                    animator.transform.SetParent(root.transform,false);
                    foreach(var socket in animator.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="RightHandSocket" || t.name=="LeftHandSocket").ToArray())
                        foreach(Transform weapon in socket.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(weapon.gameObject);
                    animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
                    definition.animatedVisualPrefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Visuals/"+name+"AnimatedVisual.prefab");
                    EditorUtility.SetDirty(definition); EditorUtility.SetDirty(controller);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(temporary); }
            }
            UpgradePlayerPrefab(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); File.Delete(Root+"/Evidence/BuildFailure.txt");
        }
        static void UpgradePlayerPrefab()
        {
            string path="Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab";
            var player=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(player.GetComponents<ThirdPersonMotor>().Length!=1 || player.GetComponent<Rigidbody>()!=null || player.GetComponent<PlayerLockOn>()==null) throw new InvalidOperationException("Unexpected player architecture");
                var classController=player.GetComponent<PlayerClassController>(); var serialized=new SerializedObject(classController);
                var previous=(Transform)serialized.FindProperty("legacyVisual").objectReferenceValue;
                var fallback=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Visuals/WarriorAnimatedVisual.prefab"),player.transform);
                fallback.name="VisualRoot_Replaceable";
                serialized.FindProperty("legacyVisual").objectReferenceValue=fallback.transform; serialized.ApplyModifiedPropertiesWithoutUndo();
                var rig=fallback.GetComponentInChildren<Animator>(); var body=rig.transform.Find("MotionPivot/BodyPivot/TorsoPivot");
                player.GetComponent<CombatPoseAnimator>().Configure(body,body.Find("LeftShoulder"),body.Find("RightShoulder"));
                player.GetComponent<PlayerAnimationBridge>().Bind(rig);
                var equipped=new SerializedObject(player.GetComponent<WeaponEquipment>());
                equipped.FindProperty("rightHandSocket").objectReferenceValue=body.Find("RightShoulder/Elbow/RightHandSocket");
                equipped.FindProperty("leftHandSocket").objectReferenceValue=body.Find("LeftShoulder/Elbow/LeftHandSocket");
                equipped.ApplyModifiedPropertiesWithoutUndo();
                if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                PrefabUtility.SaveAsPrefabAsset(player,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }
        public static void Batch()
        {
            try { Build(); PlayerIntegrationValidation.Begin(); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText(Root+"/Evidence/BuildFailure.txt",e.ToString()); EditorApplication.Exit(1); }
        }
        public static void BatchRegressions() => SoulslikeStageTools.BatchRun();
        public static void ResumeRegressions()
        {
            Directory.CreateDirectory("Temp");
            foreach(string name in new[]{"Soulslike","Corrections"})
                File.Copy(Root+"/Evidence/"+name+"Regression.txt","Temp/Bellator"+name+".test-result.txt",true);
            File.WriteAllText("Temp/BellatorSoulslike.run","2");
        }
    }
}
