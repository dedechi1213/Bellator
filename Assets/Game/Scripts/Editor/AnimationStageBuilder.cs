using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bellator.AnimationPreview;
using Bellator.Classes;
using Bellator.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    /// <summary>Authors baked transform clips and isolated preview assets. No gameplay upgrade.</summary>
    public static class AnimationStageBuilder
    {
        public const string Root = "Assets/Game/Animations/SoulslikeStage1";
        public const string ScenePath = "Assets/Game/Scenes/AnimationPreview.unity";
        public static readonly string[] Names = { "Idle", "Walk", "Run", "StrafeLeft", "StrafeRight", "WalkBackward", "DodgeRoll", "Quickstep", "LightAttack", "HeavyAttack", "Block", "HitReaction", "Death", "Fire", "Reload" };
        static readonly string[] Classes = { "Survivor", "Colossus", "Warrior", "Hunter" };
        static readonly float[] Speeds = { 1.16f, .8f, 1, 1.1f };
        const string Pivot = "MotionPivot";
        const string Body = "MotionPivot/BodyPivot";
        const string Torso = Body + "/TorsoPivot";
        const string Head = Torso + "/HeadPivot";
        static readonly string[] Bones = { Pivot, Torso, Head, Torso + "/LeftShoulder", Torso + "/LeftShoulder/Elbow", Torso + "/RightShoulder", Torso + "/RightShoulder/Elbow", Body + "/LeftHip", Body + "/LeftHip/Knee", Body + "/LeftHip/Knee/Ankle", Body + "/RightHip", Body + "/RightHip/Knee", Body + "/RightHip/Knee/Ankle" };
        static readonly float[] Durations = { 2.4f, 1.04f, .7f, 1.06f, 1.06f, 1.12f, .86f, .42f, .66f, 1.2f, .4f, .48f, 1.45f, .52f, 1.6f };
        static readonly Vector3[] BasePositions = {
            new Vector3(0,.98f,0), new Vector3(0,1.08f,0), new Vector3(0,.55f,0),
            new Vector3(-.38f,.47f,0), new Vector3(0,-.32f,0), new Vector3(.38f,.47f,0), new Vector3(0,-.32f,0),
            new Vector3(-.17f,.94f,0), new Vector3(0,-.44f,0), new Vector3(0,-.38f,0),
            new Vector3(.17f,.94f,0), new Vector3(0,-.44f,0), new Vector3(0,-.38f,0) };

        [MenuItem("Bellator/Animations Stage 1/Build isolated animation assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building preview assets.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string folder in new[] { Root + "/Clips/Common", Root + "/Clips/Survivor", Root + "/Clips/Colossus", Root + "/Clips/Hunter", Root + "/Controllers", Root + "/PreviewPrefabs", Root + "/Evidence" }) Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                var common = new AnimationClip[Names.Length];
                for (int i = 0; i < 13; i++) common[i] = Bake(i, 2, Root + "/Clips/Common/" + Names[i] + ".anim");
                var catalog = LoadOrCreate<AnimationPreviewCatalog>(Root + "/PreviewCatalog.asset");
                catalog.stateNames = Names;
                catalog.labels = new[] { "Parado / Idle", "Caminhar / Walk", "Correr / Run", "Lateral esquerda", "Lateral direita", "Recuar", "Rolamento completo", "Quickstep", "Ataque leve", "Ataque pesado", "Defesa", "Receber dano", "Morte", "Disparo (Cacador)", "Recarga (Cacador)" };
                catalog.characters = new AnimationPreviewCatalog.CharacterEntry[4];
                for (int c = 0; c < 4; c++)
                {
                    var clips = (AnimationClip[])common.Clone();
                    for (int i = 0; i < Names.Length; i++)
                    {
                        if (c == 3 || (c == 1 && i < 13) || (c == 0 && (i == 8 || i == 9))) clips[i] = Bake(i, c, Root + "/Clips/" + Classes[c] + "/" + Names[i] + ".anim");
                        else if (i >= 13) clips[i] = common[8]; // Direct inspection has no projectile or equipment substitution.
                    }
                    var controller = Controller(c, clips);
                    var prefab = Rig(c, controller);
                    catalog.characters[c] = new AnimationPreviewCatalog.CharacterEntry { label = new[] { "Sobrevivente", "Colosso", "Guerreiro", "Cacador" }[c], prefab = prefab, clips = clips, playbackSpeed = Speeds[c] };
                }
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                CreateScene(catalog);
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                Debug.Log("BELLATOR ANIMATION STAGE 1: baked assets and preview scene created.");
            }
            finally { if (setup.Any(s => s.isLoaded && !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static void BatchBuild()
        {
            try { Build(); AnimationStageValidation.Run(); AnimationStagePlayValidation.Begin(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("AnimationStageFailure.txt", e.ToString()); EditorApplication.Exit(1); }
        }
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        static Transform Node(string name, Transform parent, Vector3 position)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }

        static GameObject Rig(int c, RuntimeAnimatorController controller)
        {
            var definition = AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/" + Classes[c] + ".asset");
            var player = new GameObject(Classes[c] + "PreviewPlayerRoot");
            try
            {
                // Clone the existing primitive artwork. Only these new preview prefabs gain joints.
                var visual = UnityEngine.Object.Instantiate(definition.visualPrefab, player.transform);
                visual.name = "VisualRoot"; visual.transform.localPosition = Vector3.down * (.04f * visual.transform.localScale.y); visual.transform.localRotation = Quaternion.identity;
                foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                var body = visual.transform.Find("BodyPivot");
                var motion = Node("MotionPivot", visual.transform, BasePositions[0]); body.SetParent(motion, true);
                var torso = Node("TorsoPivot", body, BasePositions[1]);
                var head = Node("HeadPivot", torso, BasePositions[2]);
                foreach (Transform t in body.Cast<Transform>().ToArray())
                {
                    if (t == torso || t.name.EndsWith("Hip")) continue;
                    if (t.name == "Head" || t.name == "Hair" || t.name == "Eye" || t.name == "Nose_Forward") t.SetParent(head, true);
                    else t.SetParent(torso, true);
                }
                foreach (string side in new[] { "Left", "Right" })
                {
                    var shoulder = torso.Find(side + "Shoulder");
                    var elbow = Node("Elbow", shoulder, new Vector3(0,-.32f,0));
                    var arm = shoulder.Find("Arm"); var forearm = UnityEngine.Object.Instantiate(arm.gameObject, elbow).transform;
                    arm.localPosition = new Vector3(0,-.155f,0); arm.localScale = new Vector3(.18f,.155f,.18f);
                    forearm.name = "Forearm"; forearm.localPosition = new Vector3(0,-.155f,0); forearm.localScale = new Vector3(.16f,.155f,.16f);
                    shoulder.Find("Hand").SetParent(elbow, true); shoulder.Find(side + "HandSocket").SetParent(elbow, true);
                    var hip = body.Find(side + "Hip"); var knee = Node("Knee", hip, new Vector3(0,-.44f,0)); var ankle = Node("Ankle", knee, new Vector3(0,-.38f,0));
                    var leg = hip.Find("Leg"); var shin = UnityEngine.Object.Instantiate(leg.gameObject, knee).transform;
                    leg.localPosition = new Vector3(0,-.21f,0); leg.localScale = new Vector3(.22f,.21f,.22f);
                    shin.name = "Shin"; shin.localPosition = new Vector3(0,-.19f,0); shin.localScale = new Vector3(.20f,.19f,.20f);
                    hip.Find("Foot").SetParent(ankle, true);
                }
                foreach (var weapon in definition.equipment.Take(1)) Mount(weapon, torso);
                if (definition.shield != null) Mount(definition.shield, torso);
                var animator = visual.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                return PrefabUtility.SaveAsPrefabAsset(player, Root + "/PreviewPrefabs/" + Classes[c] + "AnimationPreview.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }
        static void Mount(WeaponDefinition weapon, Transform torso)
        {
            bool left = weapon.type == EquipmentType.Shield || weapon.leftHandMount;
            var socket = torso.Find((left ? "Left" : "Right") + "Shoulder/Elbow/" + (left ? "Left" : "Right") + "HandSocket");
            var instance = UnityEngine.Object.Instantiate(weapon.visualPrefab, socket);
            instance.name = weapon.visualPrefab.name; instance.transform.localPosition = weapon.mountOffset; instance.transform.localRotation = Quaternion.Euler(weapon.mountEuler);
            foreach (var script in instance.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        }

        sealed class Pose
        {
            public Vector3[] r = new Vector3[Bones.Length];
            public Vector3[] p = (Vector3[])BasePositions.Clone();
        }
        static void Guard(Pose p, int c)
        {
            p.r[3] = new Vector3(-12,0,-8); p.r[4].x = -18;
            p.r[5] = new Vector3(-22,0,8); p.r[6].x = -30;
            if (c == 1) { p.r[5] = new Vector3(-42,0,5); p.r[6].x = -48; }
            if (c == 3) { p.r[3] = new Vector3(-60,45,-12); p.r[4].x = -38; p.r[5] = new Vector3(-68,-20,3); p.r[6].x = -22; }
        }
        static float Ease(float a, float b, float t) { return Mathf.SmoothStep(a,b,t); }
        static float Envelope(float t, float a, float b, float end) { return t < a ? Ease(0,1,t/a) : t < b ? 1 : 1-Ease(0,1,(t-b)/(end-b)); }
        static Pose Sample(int clip, int c, float t)
        {
            var p = new Pose(); Guard(p,c);
            float phase = t * Mathf.PI * 2;
            if (clip == 0)
            {
                p.r[1].x = Mathf.Sin(phase)*.9f; p.r[2].x = -Mathf.Sin(phase)*.6f;
                p.r[3].z -= Mathf.Sin(phase)*1.2f; p.r[5].z += Mathf.Sin(phase)*1.2f;
            }
            else if (clip >= 1 && clip <= 5)
            {
                bool run = clip == 2, side = clip == 3 || clip == 4, backward = clip == 5;
                float a = Mathf.Sin(phase), b = -a, stride = run ? 1.15f : .65f;
                float direction = backward ? -1 : 1;
                if (side)
                {
                    float sign = clip == 3 ? -1 : 1;
                    p.r[7].z = a*18*sign; p.r[10].z = b*18*sign;
                    p.r[7].x = -Mathf.Max(0,a)*9; p.r[10].x = -Mathf.Max(0,b)*9;
                    p.r[8].x = Mathf.Max(0,a)*25; p.r[11].x = Mathf.Max(0,b)*25;
                    p.r[9].z = -p.r[7].z; p.r[12].z = -p.r[10].z;
                    p.r[1].z = Mathf.Sin(phase)*1.4f;
                }
                else
                {
                    p.r[7].x = -a*32*stride*direction; p.r[10].x = -b*32*stride*direction;
                    p.r[8].x = Mathf.Max(0,a)*(run?72:32); p.r[11].x = Mathf.Max(0,b)*(run?72:32);
                    p.r[9].x = -(p.r[7].x+p.r[8].x)*.65f; p.r[12].x = -(p.r[10].x+p.r[11].x)*.65f;
                    p.r[1].x = run?10:3; p.r[1].y += a*(run?5:2.5f);
                    if (c != 3) { p.r[3].x += a*(run?28:12); p.r[5].x += b*(run?28:12); }
                }
                p.p[0].y += run ? .03f*Mathf.Sin(phase*2) : .008f*Mathf.Sin(phase*2);
                // Lift the visual center enough to keep the stance leg/foot above the floor.
                Ground(p);
                if (run) p.p[0].y += .035f * (.5f - .5f * Mathf.Cos(phase * 2));
            }
            else if (clip == 6)
            {
                float tuck = Envelope(t,.18f,.72f,1);
                float spin = t < .18f ? 0 : t > .8f ? 360 : Ease(0,360,(t-.18f)/.62f);
                p.r[0].x = spin;
                p.p[0].y = .98f - .36f*tuck;
                p.r[1].x = 32*tuck;
                p.r[7].x = p.r[10].x = (c==0?-104:-96)*tuck;
                p.r[8].x = p.r[11].x = (c==0?140:132)*tuck;
                p.r[9].x = p.r[12].x = -30*tuck;
                p.r[3] = Vector3.Lerp(p.r[3],new Vector3(-72,0,-8),tuck); p.r[5] = Vector3.Lerp(p.r[5],new Vector3(-87,0,8),tuck);
                p.r[4].x = Mathf.Lerp(p.r[4].x,-100,tuck); p.r[6].x = Mathf.Lerp(p.r[6].x,-100,tuck);
                p.r[2].x = 24*tuck;
                Ground(p);
            }
            else if (clip == 7)
            {
                float e = Envelope(t,.16f,.42f,1);
                p.p[0] += new Vector3(-.14f*e,-.12f*e,-.1f*e);
                p.r[1] = new Vector3(14*e,0,18*e);
                p.r[7].x = -28*e; p.r[10].x = 18*e; p.r[8].x = 48*e; p.r[11].x = 26*e;
                p.r[3].x -= 20*e; p.r[5].x -= 15*e; Ground(p);
            }
            else if (clip == 8 || clip == 9)
            {
                if (c == 3) Shot(p,t,clip==9);
                else
                {
                    bool heavy = clip == 9;
                    float prepareEnd = heavy?.4f:.22f, strikeEnd = heavy?.58f:.44f;
                    float wind = t < prepareEnd ? Ease(0,1,t/prepareEnd) : t < strikeEnd ? 1-Ease(0,1,(t-prepareEnd)/(strikeEnd-prepareEnd)) : 0;
                    float follow = t < prepareEnd ? 0 : t < strikeEnd ? Ease(0,1,(t-prepareEnd)/(strikeEnd-prepareEnd)) : 1-Ease(0,1,(t-strikeEnd)/(1-strikeEnd));
                    float amplitude = c==0?.78f:c==1?1.15f:1;
                    p.r[1] += new Vector3(heavy?12*follow:3*follow,(-28*wind+36*follow)*amplitude,heavy?-4*wind+7*follow:0);
                    p.r[5] += heavy ? new Vector3(-126*wind-45*follow, -20*wind+25*follow,8*wind) : new Vector3(-40*wind-62*follow,-55*wind+85*follow,-10*wind-15*follow);
                    p.r[6].x += heavy?-38*wind+8*follow:-35*wind+15*follow;
                    p.r[3].x-=22*wind; p.r[4].x-=16*wind;
                    p.r[7].x = -12*follow; p.r[8].x = 22*follow; p.r[11].x = 10*wind;
                    Ground(p);
                }
            }
            else if (clip == 10)
            {
                float e = Ease(0,1,Mathf.Clamp01(t/.55f));
                p.r[3] = Vector3.Lerp(p.r[3], new Vector3(-72,15,-16),e); p.r[4].x=Ease(p.r[4].x,-32,e);
                p.r[5].x -= 12*e; p.r[1].x=4*e; p.r[1].y+=8*e;
                if(c==3) { p.r[3].x=Mathf.Lerp(p.r[3].x,-62,e); p.r[4].x=Mathf.Lerp(p.r[4].x,-42,e); p.r[5].x=Mathf.Lerp(p.r[5].x,-62,e); }
            }
            else if (clip == 11)
            {
                float e = t<.22f ? Ease(0,1,t/.22f) : 1-Ease(0,1,(t-.22f)/.78f);
                p.r[1] += new Vector3(-15*e,0,-6*e); p.r[2].x=-8*e;
                p.p[0].z=-.09f*e; p.r[3].x-=15*e; p.r[5].x-=10*e;
            }
            else if (clip == 12)
            {
                float fall = Ease(0,1,Mathf.Clamp01((t-.15f)/.6f));
                p.r[0].x=-88*fall; p.p[0].y=.98f-.73f*fall; p.p[0].z=-.2f*fall;
                p.r[1].z=12*fall; p.r[2].y=-18*fall;
                p.r[3]=Vector3.Lerp(p.r[3],new Vector3(-12,15,-56),fall); p.r[5]=Vector3.Lerp(p.r[5],new Vector3(-22,-10,46),fall);
                p.r[4].x=Mathf.Lerp(p.r[4].x,-18,fall); p.r[6].x=Mathf.Lerp(p.r[6].x,-30,fall);
                p.r[7].x=-8*fall; p.r[10].x=-15*fall; p.r[8].x=28*fall; p.r[11].x=38*fall;
                Ground(p);
            }
            else if (clip == 13) Shot(p,t,false);
            else if (clip == 14)
            {
                float lower = Envelope(t,.18f,.8f,1);
                p.r[5].x += 23*lower; p.r[6].x -= 8*lower; p.r[1].x = 10*lower; p.r[2].x = 15*lower;
                // Left hand reaches the stock, draws the string, seats a bolt, returns underneath.
                float draw = Envelope(t,.3f,.48f,.68f), bolt=Envelope(t,.64f,.75f,.9f);
                p.r[3] += new Vector3(14*lower-16*bolt,40*draw+14*bolt,10*lower);
                p.r[4].x += -48*draw+22*bolt;
            }
            return p;
        }
        static void Shot(Pose p, float t, bool aimed)
        {
            float aim = Envelope(t,aimed?.35f:.16f,aimed?.5f:.25f,1);
            float recoil = Mathf.Max(0,1-Mathf.Abs((t-(aimed?.57f:.34f))/.12f));
            p.r[1].x=-4*aim-7*recoil; p.r[5].x-=10*aim+8*recoil; p.r[3].x-=8*aim;
            p.r[4].x-=4*aim; p.r[2].y=5*aim; p.p[0].z=-.035f*recoil;
        }

        // Geometric contact correction is baked into clips, not a runtime pose writer.
        // Capsule/radius margins keep the roll and fall above the floor for primitive meshes.
        static void Ground(Pose p)
        {
            Quaternion root = Quaternion.Euler(p.r[0]);
            float min = float.MaxValue;
            Action<Vector3,float> test = (v,radius) => { min=Mathf.Min(min,(p.p[0]+root*v).y-radius); };
            for(int side=0;side<2;side++)
            {
                int h=side==0?7:10;
                Vector3 hip=p.p[h]+new Vector3(0,-.98f,0);
                Quaternion thigh=Quaternion.Euler(p.r[h]), shin=thigh*Quaternion.Euler(p.r[h+1]), foot=shin*Quaternion.Euler(p.r[h+2]);
                Vector3 knee=hip+thigh*p.p[h+1], ankle=knee+shin*p.p[h+2];
                test(hip,.12f); test(knee,.11f); test(ankle,.085f);
                foreach(float x in new[]{-.125f,.125f}) foreach(float y in new[]{-.08f,.08f}) foreach(float z in new[]{-.125f,.305f}) test(ankle+foot*new Vector3(x,y,z),0);
            }
            Quaternion tr=Quaternion.Euler(p.r[1]); Vector3 origin=p.p[1]+new Vector3(0,-.98f,0);
            test(origin+tr*new Vector3(0,.19f,0),.24f);
            test(origin+tr*(p.p[2]+Quaternion.Euler(p.r[2])*new Vector3(0,.24f,0)),.25f);
            // Match the original feet's 4 cm rest offset; VisualRoot removes this fixed offset.
            // Correct in both directions so walking does not float above its support foot.
            p.p[0].y += .04f-min;
        }
        public static AnimationClip BakeRoll(int character, string path) => Bake(6, character, path);
        static AnimationClip Bake(int index, int c, string path)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null) { clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,path); }
            clip.ClearCurves(); clip.name=Path.GetFileNameWithoutExtension(path); clip.frameRate=60;
            int frames=Mathf.RoundToInt(Durations[index]*60);
            var curves=new AnimationCurve[Bones.Length,6];
            for(int b=0;b<Bones.Length;b++) for(int k=0;k<6;k++) curves[b,k]=new AnimationCurve();
            for(int f=0;f<=frames;f++)
            {
                Pose pose=Sample(index,c,(float)f/frames); float time=Durations[index]*f/frames;
                for(int b=0;b<Bones.Length;b++) for(int axis=0;axis<3;axis++)
                { curves[b,axis].AddKey(time,pose.p[b][axis]); curves[b,axis+3].AddKey(time,pose.r[b][axis]); }
            }
            for(int b=0;b<Bones.Length;b++) for(int k=0;k<6;k++)
            {
                var curve=curves[b,k];
                var keys=curve.keys;
                if(keys.All(key=>Mathf.Abs(key.value-keys[0].value)<.000001f)) curve=new AnimationCurve(keys[0],keys[keys.Length-1]);
                for(int f=0;f<curve.length;f++)
                { AnimationUtility.SetKeyLeftTangentMode(curve,f,AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve,f,AnimationUtility.TangentMode.Linear); }
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(Bones[b],typeof(Transform),(k<3?"m_LocalPosition.":"localEulerAnglesRaw.")+"xyz"[k%3]),curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=index<=5; settings.loopBlend=false;
            settings.keepOriginalPositionXZ=true; settings.keepOriginalPositionY=true; settings.keepOriginalOrientation=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
            clip.wrapMode=index<=5?WrapMode.Loop:WrapMode.ClampForever;
            EditorUtility.SetDirty(clip); return clip;
        }

        static AnimatorController Controller(int c, AnimationClip[] clips)
        {
            string path=Root+"/Controllers/"+Classes[c]+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            // Rebuild subassets in place, preserving the controller GUID and external references.
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if(asset!=controller) UnityEngine.Object.DestroyImmediate(asset,true);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();
            foreach(string p in new[]{"Speed","MoveX","MoveY"}) controller.AddParameter(p,AnimatorControllerParameterType.Float);
            foreach(string p in new[]{"IsLockedOn","IsRunning","IsBlocking"}) controller.AddParameter(p,AnimatorControllerParameterType.Bool);
            foreach(string p in new[]{"Dodge","LightAttack","HeavyAttack","Hit","Die","Fire","Reload"}) controller.AddParameter(p,AnimatorControllerParameterType.Trigger);
            var sm=new AnimatorStateMachine {name="Base Layer"}; AssetDatabase.AddObjectToAsset(sm,controller);
            controller.layers=new[]{new AnimatorControllerLayer{name="Base Layer",defaultWeight=1,stateMachine=sm}};
            var free=sm.AddState("FreeLocomotion",new Vector3(260,80)); free.motion=FreeTree(controller,clips,false); free.speed=Speeds[c];
            var sprint=sm.AddState("FreeRunning",new Vector3(500,80)); sprint.motion=FreeTree(controller,clips,true); sprint.speed=Speeds[c];
            var locked=sm.AddState("LockedLocomotion",new Vector3(260,200)); locked.motion=LockedTree(controller,clips,false); locked.speed=Speeds[c];
            var lockedRun=sm.AddState("LockedRunning",new Vector3(500,200)); lockedRun.motion=LockedTree(controller,clips,true); lockedRun.speed=Speeds[c];
            sm.defaultState=free;
            var loco=new[]{free,sprint,locked,lockedRun};
            foreach(var from in loco) foreach(var to in loco)
            {
                if(from==to) continue;
                var tr=Transition(from,to,false);
                tr.AddCondition(to==locked||to==lockedRun?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"IsLockedOn");
                tr.AddCondition(to==sprint||to==lockedRun?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"IsRunning");
            }
            var states=new AnimatorState[Names.Length];
            for(int i=0;i<states.Length;i++)
            {
                var s=sm.AddState(Names[i],new Vector3(800+(i%3)*230,60+(i/3)*100)); s.motion=clips[i]; s.speed=Speeds[c]; s.writeDefaultValues=true; states[i]=s;
            }
            foreach(var s in loco)
            {
                Trigger(s,states[c==0||c==3?7:6],"Dodge"); Trigger(s,states[8],"LightAttack"); Trigger(s,states[9],"HeavyAttack");
                var block=Transition(s,states[10],false); block.AddCondition(AnimatorConditionMode.If,0,"IsBlocking");
                if(c==3) { Trigger(s,states[13],"Fire"); Trigger(s,states[14],"Reload"); }
            }
            for(int i=6;i<states.Length;i++)
            {
                if(i==12) continue;
                if(i==10) { var tr=Transition(states[i],free,false); tr.AddCondition(AnimatorConditionMode.IfNot,0,"IsBlocking"); }
                else { var tr=Transition(states[i],free,true); tr.exitTime=.98f; }
            }
            // Hit/death can interrupt live actions. Death has no outgoing transition.
            foreach(var s in loco.Concat(states.Where((s,i)=>i!=12)))
            {
                Trigger(s,states[12],"Die");
                if(s!=states[11]) Trigger(s,states[11],"Hit");
            }
            EditorUtility.SetDirty(controller); return controller;
        }
        static AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,bool exit)
        { var t=from.AddTransition(to); t.hasExitTime=exit; t.hasFixedDuration=true; t.duration=.07f; t.interruptionSource=TransitionInterruptionSource.None; return t; }
        static void Trigger(AnimatorState from,AnimatorState to,string parameter)
        { var t=Transition(from,to,false); t.AddCondition(AnimatorConditionMode.If,0,parameter); }
        static BlendTree FreeTree(AnimatorController controller,AnimationClip[] clips,bool run)
        {
            var tree=new BlendTree{name=run?"Free · Walk to Run":"Free · Idle to Walk",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller); tree.AddChild(clips[0],0); tree.AddChild(clips[1],run?.5f:1); if(run) tree.AddChild(clips[2],1); return tree;
        }
        static BlendTree LockedTree(AnimatorController controller,AnimationClip[] clips,bool run)
        {
            var tree=new BlendTree{name=run?"Locked · Running directions":"Locked · Combat directions",blendType=BlendTreeType.FreeformCartesian2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller); tree.AddChild(clips[0],Vector2.zero); tree.AddChild(clips[run?2:1],Vector2.up); tree.AddChild(clips[5],Vector2.down); tree.AddChild(clips[3],Vector2.left); tree.AddChild(clips[4],Vector2.right); return tree;
        }
        static void CreateScene(AnimationPreviewCatalog catalog)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.52f,.56f,.65f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var light=new GameObject("PreviewKeyLight").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.5f; light.transform.rotation=Quaternion.Euler(40,-30,0); light.shadows=LightShadows.Soft;
            var fill=new GameObject("PreviewFillLight").AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.65f; fill.transform.rotation=Quaternion.Euler(25,140,0);
            var camera=new GameObject("PreviewCamera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.backgroundColor=new Color(.065f,.08f,.115f); camera.clearFlags=CameraClearFlags.SolidColor; camera.fieldOfView=35;
            camera.transform.position=new Vector3(3,2.2f,5); camera.transform.LookAt(new Vector3(0,1,0));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="PreviewFloor"; floor.transform.position=new Vector3(0,-.08f,0); floor.transform.localScale=new Vector3(12,.16f,12); UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/PreviewFloor.mat");
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,Root+"/PreviewFloor.mat"); }
            material.color=new Color(.16f,.19f,.24f); floor.GetComponent<Renderer>().sharedMaterial=material;
            var viewer=new GameObject("AnimationPreviewOnly").AddComponent<AnimationPreviewPlayer>(); viewer.catalog=catalog; viewer.previewCamera=camera;
            // A visible editable reference; runtime viewer replaces it with the selected class.
            var reference=(GameObject)PrefabUtility.InstantiatePrefab(catalog.characters[2].prefab,scene); reference.name="Warrior_EditorReference";
            reference.AddComponent<AnimationPreviewEditorReference>();
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
    }
}
