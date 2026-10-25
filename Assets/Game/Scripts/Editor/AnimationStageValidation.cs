using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.AnimationPreview;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellator.Editor
{
    public static class AnimationStageValidation
    {
        static int checks;
        static void Require(bool value,string message)
        { if(!value) throw new InvalidOperationException(message); checks++; }

        [MenuItem("Bellator/Animations Stage 1/Validate and render animation sheets")]
        public static void Run()
        {
            checks=0;
            var report=new StringBuilder("BELLATOR - ANIMATION STAGE 1\nUnity "+Application.unityVersion+"\n");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(AnimationStageBuilder.ScenePath);
                var catalog=AssetDatabase.LoadAssetAtPath<AnimationPreviewCatalog>(AnimationStageBuilder.Root+"/PreviewCatalog.asset");
                Require(catalog!=null && catalog.characters.Length==4,"Four preview classes required");
                var camera=UnityEngine.Object.FindAnyObjectByType<Camera>();
                foreach(var reference in UnityEngine.Object.FindObjectsByType<AnimationPreviewEditorReference>(FindObjectsSortMode.None)) reference.gameObject.SetActive(false);
                foreach(var entry in catalog.characters)
                {
                    var go=UnityEngine.Object.Instantiate(entry.prefab);
                    try
                    {
                        var animator=go.GetComponentInChildren<Animator>(); var visual=animator.gameObject;
                        Require(!animator.applyRootMotion,"Root motion must be disabled");
                        Require(go.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"No procedural/gameplay scripts on preview rig");
                        Require(go.GetComponentsInChildren<Collider>(true).Length==0 && go.GetComponentsInChildren<Rigidbody>(true).Length==0,"No physics components on preview rig");
                        Vector3 position=go.transform.position; Quaternion rotation=go.transform.rotation;
                        var controller=(AnimatorController)animator.runtimeAnimatorController;
                        var sm=controller.layers[0].stateMachine;
                        Require(sm.states.Length==19,"19 states per controller");
                        Require(sm.states.Single(s=>s.state.name=="Death").state.transitions.Length==0,"Death cannot be interrupted");
                        Require(sm.states.Count(s=>s.state.motion is BlendTree)==4,"Four locomotion blend trees");
                        foreach(string name in AnimationStageBuilder.Names)
                            Require(sm.states.Any(s=>s.state.name==name && s.state.motion!=null),"Missing state motion: "+name);
                        for(int i=0;i<entry.clips.Length;i++)
                        {
                            var clip=entry.clips[i]; Require(clip!=null && !clip.legacy && clip.length>0,"Invalid clip");
                            var bindings=AnimationUtility.GetCurveBindings(clip); Require(bindings.Length>=60,"Baked articulated curves required");
                            foreach(var binding in bindings)
                            {
                                Require(binding.type==typeof(Transform) && binding.path.StartsWith("MotionPivot/",StringComparison.Ordinal) || binding.type==typeof(Transform) && binding.path=="MotionPivot","Clip may only animate visual descendants");
                                Require(visual.transform.Find(binding.path)!=null,"Unbound animation path: "+binding.path);
                                Require(binding.propertyName.StartsWith("m_LocalPosition") || binding.propertyName.StartsWith("localEulerAnglesRaw"),"Unexpected animated property");
                            }
                            for(int frame=0;frame<=60;frame++)
                            {
                                clip.SampleAnimation(visual,clip.length*frame/60f);
                                Require(go.transform.position==position && go.transform.rotation==rotation,"Clip moved PlayerRoot");
                                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
                                    Require(!float.IsNaN(renderer.bounds.center.y),"Invalid visual bounds");
                            }
                            if(i<=5)
                            {
                                var transforms=bindings.Select(b=>visual.transform.Find(b.path)).Distinct().ToArray();
                                clip.SampleAnimation(visual,0); var first=transforms.Select(t=>(t.localPosition,t.localRotation)).ToArray();
                                clip.SampleAnimation(visual,clip.length);
                                for(int b=0;b<transforms.Length;b++)
                                    Require(Vector3.Distance(first[b].localPosition,transforms[b].localPosition)<.0001f && Quaternion.Angle(first[b].localRotation,transforms[b].localRotation)<.05f,"Locomotion loop seam");
                            }
                        }
                        var roll=entry.clips[6];
                        var spin=AnimationUtility.GetEditorCurve(roll,EditorCurveBinding.FloatCurve("MotionPivot",typeof(Transform),"localEulerAnglesRaw.x"));
                        Require(spin!=null && Mathf.Abs(spin.Evaluate(roll.length)-spin.Evaluate(0)-360)<.01f,"Roll must contain a complete 360-degree rotation");
                        Require(!AnimationUtility.GetAnimationClipSettings(entry.clips[12]).loopTime,"Death must not loop");
                        TestController(animator,entry.playbackSpeed,entry.label=="Sobrevivente" || entry.label=="Cacador");
                        RenderSheet(camera,go,visual,entry,0,7,"Locomotion");
                        RenderSheet(camera,go,visual,entry,7,entry.label=="Cacador"?15:13,"Actions");
                        report.AppendLine(entry.label+": PASS - clips, hierarchy, loops, controller transitions, root isolation; rendered sheets.");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                report.AppendLine("AnimationClip assets: "+AssetDatabase.FindAssets("t:AnimationClip",new[]{AnimationStageBuilder.Root}).Length);
                report.AppendLine("Animator Controllers: "+AssetDatabase.FindAssets("t:AnimatorController",new[]{AnimationStageBuilder.Root}).Length);
                report.AppendLine("Checks: "+checks+"\nRESULT: PASS");
                File.WriteAllText(AnimationStageBuilder.Root+"/Evidence/Validation.txt",report.ToString());
                Debug.Log(report.ToString()); AssetDatabase.Refresh();
            }
            catch(Exception e)
            { report.AppendLine("RESULT: FAIL\n"+e); File.WriteAllText(AnimationStageBuilder.Root+"/Evidence/Validation.txt",report.ToString()); throw; }
            finally { if(setup.Any(s=>s.isLoaded && !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void Step(Animator a,float seconds)
        { for(int i=0;i<Mathf.CeilToInt(seconds*60);i++) a.Update(1/60f); }
        static bool State(Animator a,string name) { return a.GetCurrentAnimatorStateInfo(0).IsName(name); }
        internal static void TestController(Animator a,float speed,bool quick)
        {
            a.Rebind(); a.Update(0); Step(a,.15f); Require(State(a,"FreeLocomotion"),"Default locomotion");
            a.SetBool("IsLockedOn",true); a.SetFloat("MoveX",-1); Step(a,.2f); Require(State(a,"LockedLocomotion"),"Lock-on strafe tree");
            a.SetBool("IsRunning",true); Step(a,.2f); Require(State(a,"LockedRunning"),"Lock-on run tree");
            a.SetBool("IsRunning",false); Step(a,.2f);
            foreach(string trigger in new[]{"LightAttack","HeavyAttack","Dodge"})
            {
                a.SetTrigger(trigger); Step(a,.12f);
                string state=trigger=="Dodge"?(quick?"Quickstep":"DodgeRoll"):trigger;
                Require(State(a,state),"Trigger did not enter "+state);
                a.SetFloat("Speed",1); a.SetBool("IsLockedOn",false); a.SetBool("IsRunning",true);
                Step(a,.08f); Require(State(a,state),"Walking interrupted action "+state);
                Step(a,2.2f/speed); Require(State(a,"FreeRunning"),"Action did not recover to locomotion");
            }
            a.SetBool("IsBlocking",true); Step(a,.2f); Require(State(a,"Block"),"Block entry"); Step(a,1); Require(State(a,"Block"),"Block not held");
            a.SetBool("IsBlocking",false); Step(a,.25f); Require(State(a,"FreeRunning"),"Block release");
            a.SetTrigger("Hit"); Step(a,.15f); Require(State(a,"HitReaction"),"Hit trigger"); Step(a,1/speed);
            a.SetTrigger("Die"); Step(a,.15f); Require(State(a,"Death"),"Death trigger");
            a.SetTrigger("Hit"); a.SetTrigger("LightAttack"); a.SetTrigger("Dodge"); a.SetBool("IsLockedOn",true); Step(a,3/speed); Require(State(a,"Death"),"Death interrupted");
            a.Rebind(); a.Update(0);
        }
        static void RenderSheet(Camera camera,GameObject root,GameObject visual,AnimationPreviewCatalog.CharacterEntry entry,int start,int end,string group)
        {
            const int width=256,height=256,columns=5;
            var atlas=new Texture2D(width*columns,height*(end-start),TextureFormat.RGB24,false);
            var target=new RenderTexture(width,height,24); var tile=new Texture2D(width,height,TextureFormat.RGB24,false);
            var label=new GameObject("FrameLabel").AddComponent<TextMesh>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=36; label.characterSize=.032f; label.color=Color.white; label.anchor=TextAnchor.UpperLeft;
            label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            label.transform.SetParent(camera.transform,false); label.transform.localPosition=new Vector3(-1.43f,1.4f,4.8f); label.transform.localRotation=Quaternion.identity;
            try
            {
                camera.transform.position=entry.label=="Colosso"?new Vector3(4.5f,2.9f,8.5f):new Vector3(3.8f,2.6f,6.8f); camera.transform.LookAt(new Vector3(0,entry.label=="Colosso"?1.4f:1.2f,0)); camera.fieldOfView=34;
                camera.targetTexture=target;
                var animator=visual.GetComponent<Animator>(); animator.enabled=false;
                for(int row=start;row<end;row++) for(int col=0;col<columns;col++)
                {
                    float normalized=col/(float)(columns-1);
                    entry.clips[row].SampleAnimation(visual,entry.clips[row].length*normalized);
                    var renderers=visual.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds;
                    foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    AnimationPreviewPlayer.FrameBounds(camera,bounds,new Vector3(0,entry.label=="Colosso"?1.4f:1.2f,0),entry.label=="Colosso"?9.5f:7.5f);
                    label.text=AnimationStageBuilder.Names[row]+"  "+normalized.ToString("0.00");
                    camera.Render(); var prior=RenderTexture.active; RenderTexture.active=target;
                    tile.ReadPixels(new Rect(0,0,width,height),0,0); tile.Apply(); RenderTexture.active=prior;
                    atlas.SetPixels(col*width,(end-start-1-(row-start))*height,width,height,tile.GetPixels());
                }
                atlas.Apply(); File.WriteAllBytes(AnimationStageBuilder.Root+"/Evidence/"+entry.prefab.name.Replace("AnimationPreview","")+"_"+group+".png",atlas.EncodeToPNG());
            }
            finally
            { camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(label.gameObject); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(tile); UnityEngine.Object.DestroyImmediate(atlas); }
        }
    }
}
