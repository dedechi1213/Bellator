using System;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class GladiatorToonReview
    {
        const string Active="Bellator.Toon.PlayerReview";
        const string Result="Assets/Game/Animations/SoulslikeGameplay/Evidence/IntegrationValidation.txt";
        static GladiatorToonReview(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(File.Exists("Temp/BellatorToon.stop"))
            {
                File.Delete("Temp/BellatorToon.stop");SessionState.SetBool(Active,false);
                SessionState.SetBool("Bellator.ModelRegression.PlayerNavigation",false);
                SessionState.SetBool("Bellator.PlayerStage2.Test",false);
                if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();
                return;
            }
            if(SessionState.GetBool(Active,false))
            {
                Application.runInBackground=true;
                if(EditorApplication.isPaused)EditorApplication.isPaused=false;
                EditorApplication.QueuePlayerLoopUpdate();
                if(EditorApplication.isPlaying)
                {
                    var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Repaint();
                    // Current selection confirms back to the mode menu. Complete that
                    // public navigation before the older arena regression resumes.
                    if(SceneManager.GetActiveScene().name=="MainMenu"&&ClassSelectionState.Selected!=null&&GameObject.Find("StoryModeButton")!=null)
                        GameObject.Find("StoryModeButton").GetComponent<Button>().onClick.Invoke();
                }
                if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists(Result))
                {
                    Directory.CreateDirectory(GladiatorModelUpgrade.Evidence);
                    File.Copy(Result,GladiatorModelUpgrade.Evidence+"/"+SessionState.GetString(Active+".Label","Player")+"PlayMode.txt",true);
                    SessionState.SetBool(Active,false);SessionState.SetBool("Bellator.ModelRegression.PlayerNavigation",false);
                    EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity");
                }
            }
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Temp/BellatorToon.player"))
            {
                string label=File.ReadAllText("Temp/BellatorToon.player").Trim();File.Delete("Temp/BellatorToon.player");
                SessionState.SetString(Active+".Label",label);SessionState.SetBool(Active,true);
                Application.runInBackground=true;
                GladiatorModelValidation.BatchPlayer();
            }
            if(File.Exists("Temp/BellatorToon.gallery"))
            {
                File.Delete("Temp/BellatorToon.gallery");
                try{Gallery();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/BellatorToon.error.txt",e.ToString());}
            }
        }
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        [MenuItem("Bellator/Models/Render stylized model turnarounds")]
        public static void Gallery()
        {
            string folder=GladiatorModelUpgrade.Evidence;Directory.CreateDirectory(folder);
            var setup=EditorSceneManager.GetSceneManagerSetup();var report=new StringBuilder();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            try
            {
                var shader=Shader.Find("Bellator/Gladiator Toon");
                var messages=ShaderUtil.GetShaderMessages(shader);
                if(!shader.isSupported||messages.Any(m=>m.severity.ToString()=="Error"))throw new Exception("Toon shader unsupported or failed compilation: "+string.Join("; ",messages.Select(m=>m.message)));
                var camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.65f,.66f,.67f);camera.nearClipPlane=.01f;camera.farClipPlane=50;
                var key=new GameObject("ReviewKey").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(35,-35,0);
                var fill=new GameObject("ReviewFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.7f,.8f,1);fill.transform.rotation=Quaternion.Euler(25,135,0);
                foreach(string name in new[]{"Survivor","Colossus","Warrior","Hunter","Gladiator","Heavy","Archer"})
                {
                    bool enemy=name=="Gladiator"||name=="Heavy"||name=="Archer";
                    var definition=enemy?null:AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+name+".asset");
                    var prefab=enemy?AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/"+name+".prefab"):definition.animatedVisualPrefab;
                    var actor=UnityEngine.Object.Instantiate(prefab);actor.transform.position=Vector3.zero;
                    try
                    {
                        var animator=actor.GetComponentInChildren<Animator>();
                        if(animator!=null){animator.Rebind();animator.Update(0);var idle=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.Contains("Idle"));idle?.SampleAnimation(animator.gameObject,.3f);}
                        var enemyDefinition=enemy?actor.GetComponent<EnemyBrain>().Definition:null;
                        var equipment=definition!=null?new[]{definition.equipment[0],definition.shield}:new[]{enemyDefinition.weapon,enemyDefinition.shield};
                        foreach(var item in equipment)if(item!=null)
                        {
                            var socket=Find(actor.transform,item.leftHandMount||item.type==EquipmentType.Shield?"LeftHandSocket":"RightHandSocket");
                            var weapon=UnityEngine.Object.Instantiate(item.visualPrefab,socket,false);weapon.transform.localPosition=item.mountOffset;weapon.transform.localRotation=Quaternion.Euler(item.mountEuler);
                        }
                        foreach(var lod in actor.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            if(skin.sharedMaterials.Any(mat=>mat==null||mat.shader!=shader))throw new Exception("Inconsistent material: "+name);
                            var points=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;var triangles=skin.sharedMesh.GetTriangles(0);
                            for(int t=0;t<triangles.Length;t+=3)
                            {
                                bool arm=false,leg=false;float top=0;
                                for(int v=0;v<3;v++)
                                {
                                    int index=triangles[t+v];var w=weights[index];top=Mathf.Max(top,points[index].y);
                                    foreach(var pair in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1)})if(pair.Item2>.1f)
                                    {arm|=pair.Item1>=3&&pair.Item1<=6;leg|=pair.Item1>=7;}
                                }
                                if(arm&&leg&&top<1.15f)throw new Exception("Skin triangle connects an arm to a leg: "+name+" / "+skin.name+" / "+string.Join("; ",Enumerable.Range(0,3).Select(v=>points[triangles[t+v]].ToString("F4"))));
                            }
                            report.AppendLine(name+" / "+skin.name+": "+skin.sharedMesh.vertexCount+" vertices; "+skin.sharedMesh.triangles.Length/3+" triangles; "+skin.bones.Length+" bones; "+skin.sharedMaterials.Length+" material slots.");
                        }
                        foreach(var view in new[]{("Front",180f),("ThreeQuarter",140f),("Side",90f),("Back",0f)})
                        {
                            actor.transform.rotation=Quaternion.Euler(0,view.Item2,0);
                            var all=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var bounds=all[0].bounds;
                            // Bake the current pose: animation culling bounds are deliberately larger.
                            bool first=true;
                            foreach(var r in all)
                            {
                                Mesh mesh=(r as SkinnedMeshRenderer)?.sharedMesh??r.GetComponent<MeshFilter>()?.sharedMesh;
                                if(mesh==null)continue;
                                var points=mesh.vertices;
                                if(r is SkinnedMeshRenderer posed)
                                {
                                    var matrices=posed.bones.Select((bone,index)=>bone.localToWorldMatrix*mesh.bindposes[index]).ToArray();var weights=mesh.boneWeights;
                                    for(int v=0;v<points.Length;v++)
                                    {
                                        var p=points[v];var w=weights[v];
                                        points[v]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                                    }
                                }
                                else for(int v=0;v<points.Length;v++)points[v]=r.transform.TransformPoint(points[v]);
                                foreach(var p in points){if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                            }
                            camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.14f;camera.transform.position=bounds.center+Vector3.back*12;camera.transform.rotation=Quaternion.identity;
                            GladiatorModelUpgrade.Capture(camera,folder+"/"+name+"_"+view.Item1+".png",1000,1000);
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(actor);}
                }
                report.AppendLine("PASS: 7 profiles, 28 equipped views; supported toon shader; shared materials; no arm-to-leg skin bridges at either LOD.");
                File.WriteAllText(folder+"/GeometryAndShaderValidation.txt",report.ToString());
            }
            finally{if(setup.Any(s=>s.isLoaded&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
