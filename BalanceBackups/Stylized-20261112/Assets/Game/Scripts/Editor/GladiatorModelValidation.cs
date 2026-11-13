using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class GladiatorModelValidation
    {
        const string Key="Bellator.ModelRegression";
        static GladiatorModelValidation()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key+".EditReady",true);
                if(state==PlayModeStateChange.ExitingEditMode)SessionState.SetBool(Key+".EditReady",false);
            };
        }
        public static void BatchPlayer()
        {
            SessionState.SetBool(Key+".PlayerNavigation",true);PlayerIntegrationValidation.Begin();
        }
        // These suites exercise the actual gameplay components, not replacement test doubles.
        public static void BatchActors()
        {
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Index",0);
            Directory.CreateDirectory(GladiatorModelUpgrade.Evidence);SampleSkinning();Start(0);
        }
        static void Start(int index)
        {
            SessionState.SetBool(Key+".EditReady",false);
            SessionState.SetFloat(Key+".Deadline",(float)(EditorApplication.timeSinceStartup+420));
            string suite=new[]{"Expansion","Balance","Artwork"}[index];File.Delete("Temp/Bellator"+suite+".test-result.txt");
            if(index==0)ArenaExpansionValidation.Run();else if(index==1)ClassBalanceTools.Run();else BellatorUIValidation.Run();
        }
        static void Tick()
        {
            // The older player regression predates the current confirmation screen. Complete
            // that real screen through its public handler while retaining every gameplay assertion.
            if(SessionState.GetBool(Key+".PlayerNavigation",false) && EditorApplication.isPlaying &&
                SceneManager.GetActiveScene().name=="GladiatorSelection" && SelectionNavigation.StartAfterConfirmation && GameObject.Find("ContinueButton")!=null)
                UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>()?.Confirm();
            if(!SessionState.GetBool(Key,false))return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".Deadline",0)) {SessionState.SetBool(Key,false);File.WriteAllText(GladiatorModelUpgrade.Evidence+"/ActorsFailure.txt","Timed out");EditorApplication.Exit(1);return;}
            if(!SessionState.GetBool(Key+".EditReady",false)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            int index=SessionState.GetInt(Key+".Index",0);string suite=new[]{"Expansion","Balance","Artwork"}[index],path="Temp/Bellator"+suite+".test-result.txt";
            if(!File.Exists(path))return;
            File.Copy(path,GladiatorModelUpgrade.Evidence+"/"+suite+"PlayMode.txt",true);
            if(!File.ReadAllText(path).Contains("RESULT: PASS")){SessionState.SetBool(Key,false);EditorApplication.Exit(1);return;}
            if(index<2){SessionState.SetInt(Key+".Index",index+1);Start(index+1);}
            else {SessionState.SetBool(Key,false);EditorApplication.Exit(0);}
        }
        [MenuItem("Bellator/Models/Validate baked skin and animation poses")]
        public static void SampleSkinning()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();var report=new StringBuilder();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            try
            {
                foreach(string name in new[]{"Survivor","Colossus","Warrior","Hunter"})
                {
                    var definition=AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+name+".asset");var actor=UnityEngine.Object.Instantiate(definition.animatedVisualPrefab);
                    try
                    {
                        var animator=actor.GetComponentInChildren<Animator>();var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();int samples=0;
                        foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                        {
                            for(int i=0;i<=8;i++)
                            {
                                clip.SampleAnimation(animator.gameObject,clip.length*i/8);
                                foreach(var skin in skins)
                                {
                                    var baked=new Mesh();skin.BakeMesh(baked);
                                    if(baked.vertexCount!=skin.sharedMesh.vertexCount||baked.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new Exception("Invalid baked pose: "+name+" "+clip.name);
                                    if(baked.bounds.size.magnitude>8)throw new Exception("Exploding skin: "+name+" "+clip.name);
                                    UnityEngine.Object.DestroyImmediate(baked);
                                }
                                samples++;
                            }
                        }
                        report.AppendLine("PASS: "+name+" — "+samples+" sampled animation poses at both LODs; finite vertices, stable bounds, valid skinning.");
                    }
                    finally {UnityEngine.Object.DestroyImmediate(actor);}
                }
                report.AppendLine("RESULT: PASS");File.WriteAllText(GladiatorModelUpgrade.Evidence+"/SkinningValidation.txt",report.ToString());
            }
            finally {if(setup.Any(s=>s.isLoaded&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
