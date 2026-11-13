using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Bellator.Editor
{
    public static class ArenaMovementTools
    {
        const string EnemyAnimations = PlayerIntegrationTools.Root + "/Enemies";
        static readonly string[] Players = { "Survivor", "Colossus", "Warrior", "Hunter" };
        static readonly string[] Enemies = { "Gladiator", "Heavy", "Archer" };
        static readonly string[] Donors = { "Warrior", "Colossus", "Hunter" };

        public static void BuildPilot()
        {
            AnimationStageBuilder.BakeMovementPilot();
            RefreshPlayerMesh("Warrior",2);
            AssetDatabase.SaveAssets();
        }
        public static void RunPilot()
        {
            BuildPilot();
            VerifyGait("Warrior");
            GladiatorModelValidation.BatchPlayer();
        }
        public static void RunFullPlayerValidation()
        {
            Build();
            GladiatorModelValidation.BatchPlayer();
        }

        [MenuItem("Bellator/Movement/Rebuild gait and enemy rigs")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before rebuilding animation assets.");
            BuildPilot();
            AnimationStageBuilder.RebuildLocomotion("Colossus",1);
            AnimationStageBuilder.RebuildLocomotion("Hunter",3);
            for (int i=0;i<Players.Length;i++)
                if (i != 2) RefreshPlayerMesh(Players[i],i);
            Directory.CreateDirectory(EnemyAnimations);
            AssetDatabase.Refresh();
            for (int i=0;i<Enemies.Length;i++) UpgradeEnemy(Enemies[i],Donors[i],i+4);
            AssetDatabase.SaveAssets();
            foreach (string name in Players) VerifyGait(name);
        }

        static void VerifyGait(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerIntegrationTools.Root+"/Visuals/"+name+"AnimatedVisual.prefab");
            var copy = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var animator = copy.GetComponentInChildren<Animator>();
                var ankle = animator.transform.Find("MotionPivot/BodyPivot/LeftHip/Knee/Ankle");
                int verified = 0;
                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    Vector3 stride;
                    switch (clip.name)
                    {
                        case "Walk": stride=Vector3.forward*PlayerAnimationBridge.WalkStride; break;
                        case "Run": stride=Vector3.forward*PlayerAnimationBridge.RunStride; break;
                        case "WalkBackward": stride=Vector3.back*PlayerAnimationBridge.BackStride; break;
                        case "StrafeLeft": stride=Vector3.left*PlayerAnimationBridge.SideStride; break;
                        case "StrafeRight": stride=Vector3.right*PlayerAnimationBridge.SideStride; break;
                        default: continue;
                    }
                    Vector3 AnkleAt(float time)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*time);
                        return animator.transform.InverseTransformPoint(ankle.position);
                    }
                    Vector3 start=AnkleAt(.12f), end=AnkleAt(.26f), airborne=AnkleAt(.81f);
                    if ((end-start + stride*.14f).magnitude > .02f ||
                        Mathf.Abs(start.y-.12f) > .02f || airborne.y-start.y < .07f)
                        throw new InvalidOperationException(name+"/"+clip.name+" invalid foot trajectory. Support: "+start+" -> "+end+", swing: "+airborne);
                    verified++;
                }
                if (verified != 5) throw new InvalidOperationException(name+" is missing locomotion clips.");
                Debug.Log("Bellator gait PASS: "+name+" has planted and swing trajectories in the five locomotion clips.");
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        static void RefreshPlayerMesh(string name, int profile)
        {
            var animated = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerIntegrationTools.Root+"/Visuals/"+name+"AnimatedVisual.prefab");
            var selection = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Classes/"+name+"Visual.prefab");
            GladiatorModelUpgrade.RefreshMovementMeshes(animated,profile);
            GladiatorModelUpgrade.RefreshMovementMeshes(selection,profile);
        }

        static Transform Joint(Transform parent, string name, Vector3 position)
        {
            var node = parent.Find(name);
            if (node != null) return node;
            node = new GameObject(name).transform;
            node.SetParent(parent,false);
            node.localPosition=position;
            return node;
        }

        static void UpgradeEnemy(string name, string donor, int profile)
        {
            string path = "Assets/Game/Prefabs/Enemies/"+name+".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visual = root.transform.Find("VisualRoot_Replaceable");
                var motion = Joint(visual,"MotionPivot",Vector3.up*.98f);
                var body = motion.Find("BodyPivot");
                if (body == null)
                {
                    body = visual.Find("BodyPivot");
                    body.SetParent(motion,true);
                    var torso = Joint(body,"TorsoPivot",Vector3.up*1.08f);
                    var head = body.Find("ArtHeadBone");
                    head.SetParent(torso,true);
                    head.name="HeadPivot";
                    foreach (string side in new[] { "Left", "Right" })
                    {
                        var shoulder = body.Find(side+"Shoulder");
                        shoulder.SetParent(torso,true);
                        var elbow = Joint(shoulder,"Elbow",Vector3.down*.32f);
                        shoulder.Find(side+"HandSocket").SetParent(elbow,true);
                        var hip = body.Find(side+"Hip");
                        var knee = Joint(hip,"Knee",Vector3.down*.44f);
                        var ankle = Joint(knee,"Ankle",Vector3.down*.38f);
                        hip.Find("Foot").SetParent(ankle,true);
                    }
                    visual.localPosition -= Vector3.up * (.04f * visual.localScale.y);
                }
                GladiatorModelUpgrade.RebuildMovementVisual(body,profile);
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController=BuildEnemyController(name,donor);
                animator.applyRootMotion=false;
                animator.cullingMode = name=="Archer" ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        if (visual.Find(binding.path) == null)
                            throw new InvalidOperationException(name+" is missing animation bone "+binding.path);
                var pose = new SerializedObject(root.GetComponent<EnemyPoseAnimator>());
                pose.FindProperty("animator").objectReferenceValue=animator;
                pose.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
                VerifyEnemySkin(animator);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void VerifyEnemySkin(Animator animator)
        {
            var skins = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skins.Length != 2) throw new InvalidOperationException("Expected two skin LODs on "+animator.transform.root.name);
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            if (clips.Length < 10) throw new InvalidOperationException("Incomplete enemy controller on "+animator.transform.root.name);
            var baked = new Mesh();
            int samples = 0;
            try
            {
                foreach (var clip in clips)
                    for (int frame=0; frame<=8; frame++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/8);
                        foreach (var skin in skins)
                        {
                            skin.BakeMesh(baked);
                            if (baked.vertexCount != skin.sharedMesh.vertexCount ||
                                baked.bounds.size.magnitude > 8 ||
                                baked.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))
                                throw new InvalidOperationException("Invalid baked skin in "+animator.transform.root.name+"/"+clip.name);
                        }
                        samples++;
                    }
                Debug.Log("Bellator enemy skin PASS: "+animator.transform.root.name+", "+samples+" poses in both LODs.");
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        static AnimatorController BuildEnemyController(string name, string donor)
        {
            string path = EnemyAnimations+"/"+name+".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            foreach (string parameter in new[] { "Speed", "MoveX", "MoveY", "LocomotionRate" })
                if (!controller.parameters.Any(existing=>existing.name==parameter))
                    controller.AddParameter(new AnimatorControllerParameter { name=parameter,
                        type=AnimatorControllerParameterType.Float,defaultFloat=parameter=="LocomotionRate"?1:0 });
            var source = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerIntegrationTools.Root+"/Controllers/"+donor+"Gameplay.controller");
            var sourceStates = source.layers[0].stateMachine.states;
            var machine = controller.layers[0].stateMachine;
            string attack = name=="Archer" ? "Fire" : name=="Heavy" ? "HeavyAttack" : "LightAttack";
            foreach (string key in new[] { "FreeRunning", "LockedLocomotion", attack, "Block", "HitReaction", "Death" })
            {
                var state = machine.states.FirstOrDefault(item=>item.state.name==key).state;
                if (state == null) state=machine.AddState(key);
                state.motion = key=="Fire" ? AnimationStageBuilder.BakeBowRelease(EnemyAnimations+"/BowRelease.anim")
                    : sourceStates.Single(item=>item.state.name==key).state.motion;
                if (state.motion is BlendTree)
                {
                    state.speedParameter="LocomotionRate";
                    state.speedParameterActive=true;
                }
                if (key=="FreeRunning") machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
