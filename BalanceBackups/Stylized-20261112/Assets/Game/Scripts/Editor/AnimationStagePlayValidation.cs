using System;
using System.Collections;
using System.IO;
using Bellator.AnimationPreview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class AnimationStagePlayValidation
    {
        const string Key="Bellator.AnimationStage1.PlayValidation";
        static IEnumerator routine;
        static int frame=-1;
        static string failure;
        static bool editorSearchIssue;
        static AnimationStagePlayValidation()
        {
            EditorApplication.update+=Update;
            Application.logMessageReceived+=(message,stack,type)=>
            {
                if(!SessionState.GetBool(Key,false) || (type!=LogType.Exception && type!=LogType.Error && type!=LogType.Assert)) return;
                // Unity 6000.3 Search can throw while indexing a copied project's editor database.
                // Record this specific editor-only fault separately; preview/runtime errors still fail.
                if(stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Bellator.")) { editorSearchIssue=true; return; }
                failure=message+"\n"+stack;
            };
        }
        [MenuItem("Bellator/Animations Stage 1/Test preview in Play Mode")]
        public static void Begin()
        {
            EditorSceneManager.OpenScene(AnimationStageBuilder.ScenePath);
            failure=null; editorSearchIssue=false; routine=null; frame=-1;
            File.Delete(AnimationStageBuilder.Root+"/Evidence/PlayModeValidation.txt");
            SessionState.SetBool(Key,true); SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+180);
            SessionState.SetBool(Key+"Batch",Application.isBatchMode);
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.position=new Rect(20,20,1440,1000); view.Focus();
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0)) { End("Play Mode timeout"); return; }
            if(!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling) return;
            if(frame==Time.frameCount) return; frame=Time.frameCount;
            try
            {
                if(failure!=null) throw new InvalidOperationException(failure);
                if(routine==null) routine=Run();
                if(!routine.MoveNext()) End(null);
            }
            catch(Exception e) { End(e.ToString()); }
        }
        static IEnumerator Run()
        {
            for(int n=0;n<12;n++) yield return null;
            var viewer=UnityEngine.Object.FindAnyObjectByType<AnimationPreviewPlayer>();
            if(viewer==null || viewer.CurrentAnimator==null) throw new InvalidOperationException("Preview viewer did not start");
            int inspected=0;
            for(int c=0;c<4;c++)
            {
                viewer.SelectClass(c); yield return null; yield return null;
                for(int i=0;i<(c==3?15:13);i++)
                {
                    viewer.SelectClip(i);
                    for(int n=0;n<5;n++) yield return null;
                    if(!viewer.CurrentAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimationStageBuilder.Names[i])) throw new InvalidOperationException("Preview selection failed: "+c+"/"+i);
                    if(viewer.CurrentAnimator.applyRootMotion) throw new InvalidOperationException("Root motion enabled in preview");
                    if(viewer.previewCamera.transform.position.y<.3f) throw new InvalidOperationException("Preview camera is below the floor");
                    var renderers=viewer.CurrentAnimator.GetComponentsInChildren<Renderer>();
                    var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var screen=viewer.previewCamera.WorldToViewportPoint(bounds.center);
                    if(screen.z<=0 || screen.x<.15f || screen.x>.9f || screen.y<.05f || screen.y>.95f) throw new InvalidOperationException("Character outside preview camera: "+screen);
                    inspected++;
                }
                viewer.enabled=false;
                AnimationStageValidation.TestController(viewer.CurrentAnimator,viewer.catalog.characters[c].playbackSpeed,c==0||c==3);
                viewer.enabled=true;
                viewer.SelectClip(0);
                for(int n=0;n<8;n++) yield return null;
                ScreenCapture.CaptureScreenshot(AnimationStageBuilder.Root+"/Evidence/PreviewClass"+c+".png");
                for(int n=0;n<8;n++) yield return null;
            }
            viewer.SelectClass(2); viewer.SelectClip(0);
            for(int n=0;n<8;n++) yield return null;
            ScreenCapture.CaptureScreenshot(AnimationStageBuilder.Root+"/Evidence/PreviewInterface.png");
            for(int n=0;n<8;n++) yield return null;
            File.WriteAllText(AnimationStageBuilder.Root+"/Evidence/PlayModeValidation.txt","Unity "+Application.unityVersion+"\nPreview: 4 classes / "+inspected+" individual selections\nCamera: above floor; visual bounds center inside viewport for every selection; UI screenshots captured for 4 classes\nAnimator: locomotion, lock-on, running, action protection, block, hit, death for all classes\nPreview/runtime errors: 0\n"+(editorSearchIssue?"Editor note: UnityEditor.Search.SearchDatabase startup index exception recorded separately; unrelated to preview scripts.\n":"")+"RESULT: PASS\n");
        }
        static void End(string error)
        {
            bool batch=SessionState.GetBool(Key+"Batch",false); SessionState.SetBool(Key,false); routine=null;
            if(error!=null) { File.WriteAllText(AnimationStageBuilder.Root+"/Evidence/PlayModeValidation.txt","RESULT: FAIL\n"+error); Debug.LogError(error); }
            EditorApplication.ExitPlaymode();
            if(batch) EditorApplication.delayCall+=()=>EditorApplication.Exit(error==null?0:1);
        }
    }
}
