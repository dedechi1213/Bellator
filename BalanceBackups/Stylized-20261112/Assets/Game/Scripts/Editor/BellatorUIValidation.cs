using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class BellatorUIValidation
    {
        const string Active="Bellator.Artwork.Validation";
        static IEnumerator routine;
        static int frame=-1, originalSize, customSizes;
        static double deadline;
        static object sizeGroup;
        static EditorWindow gameView;
        static string rankingPath, rememberedName, evidenceDirectory;
        static bool hadRemember, hadRememberedName;
        static int rememberValue;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static BellatorUIValidation() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=State; }
        [MenuItem("Bellator/UI/Test Original Screens in Play Mode")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GameInterface.MainScene);
            SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(routine!=null && EditorApplication.isPlaying)
            {
                if(EditorApplication.isPaused) EditorApplication.isPaused=false;
                EditorApplication.QueuePlayerLoopUpdate(); gameView?.Repaint();
                if(EditorApplication.timeSinceStartup>deadline) { Finish(new TimeoutException("UI validation timed out")); return; }
            }
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;
                try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("UI validation timed out"); if(!routine.MoveNext()) Finish(null); }
                catch(Exception e) { Finish(e); }
            }
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists("Temp/BellatorArtwork.test"))
            { File.Delete("Temp/BellatorArtwork.test"); Run(); }
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists("Temp/BellatorSelection.test"))
            { File.Delete("Temp/BellatorSelection.test"); SessionState.SetBool(Active+".Selection",true); Run(); }
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active+".Quit",false))
            { SessionState.SetBool(Active+".Quit",false); Pass("Quit button exits Play Mode through the game's real Quit handler."); Finish(null); }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active+".Reopen",false))
            { SessionState.SetBool(Active+".Reopen",false); EditorSceneManager.OpenScene(GameInterface.MainScene); }
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear(); errors.Clear(); report.AppendLine("Bellator original artwork — Unity "+Application.unityVersion+" — real Play Mode");
            Application.runInBackground=true;
            var root=Path.GetFullPath("Temp/ArtworkValidation/"+Guid.NewGuid().ToString("N"));
            rankingPath=Path.Combine(root,"ranking.json");
            evidenceDirectory=Path.Combine(root,"Evidence"); Directory.CreateDirectory(evidenceDirectory);
            hadRemember=PlayerPrefs.HasKey("Bellator.RememberUsername"); rememberValue=PlayerPrefs.GetInt("Bellator.RememberUsername",0);
            hadRememberedName=PlayerPrefs.HasKey("Bellator.RememberedUsername"); rememberedName=PlayerPrefs.GetString("Bellator.RememberedUsername","");
            typeof(PlayerSession).GetField("accounts",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new LocalAccountStore(root));
            Application.logMessageReceived+=Log;
            gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); gameView.Focus();
            originalSize=(int)gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(gameView);
            deadline=EditorApplication.timeSinceStartup+300; routine=Flatten(SessionState.GetBool(Active+".Selection",false)?SelectionFramingChecks():Checks()); frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log; routine=null;
            if(error==null && errors.Count>0) error=new Exception(string.Join("\n",errors));
            try
            {
                gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(gameView,originalSize);
                for(int i=0;i<customSizes;i++) sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1});
            }
            catch(Exception e) { report.AppendLine("GameView restore: "+e.Message); }
            customSizes=0;
            if(hadRemember) PlayerPrefs.SetInt("Bellator.RememberUsername",rememberValue); else PlayerPrefs.DeleteKey("Bellator.RememberUsername");
            if(hadRememberedName) PlayerPrefs.SetString("Bellator.RememberedUsername",rememberedName); else PlayerPrefs.DeleteKey("Bellator.RememberedUsername");
            PlayerPrefs.Save();
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            bool selectionOnly=SessionState.GetBool(Active+".Selection",false);
            string output="Assets/Game/Documentation/BellatorUI"+(selectionOnly?"/SelectionFraming":"");
            Directory.CreateDirectory(output);
            // Capture outside Assets so the importer cannot lock an output while it is written.
            AssetDatabase.StartAssetEditing();
            try { foreach(var capture in Directory.GetFiles(evidenceDirectory,"*.png")) File.Copy(capture,Path.Combine(output,Path.GetFileName(capture)),true); }
            finally { AssetDatabase.StopAssetEditing(); }
            File.WriteAllText(Path.Combine(output,"Validation.txt"),report.ToString());
            SessionState.SetBool(Active+".Selection",false);
            File.WriteAllText("Temp/BellatorArtwork.test-result.txt",report.ToString());
            SessionState.SetBool(Active,false); SessionState.SetBool(Active+".Reopen",true);
            if(error!=null) Debug.LogException(error); else Debug.Log("Bellator original artwork validation passed.");
            if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator source)
        { var stack=new Stack<IEnumerator>(); stack.Push(source); while(stack.Count>0) { var top=stack.Peek(); if(!top.MoveNext()) {stack.Pop();continue;} if(top.Current is IEnumerator nested) stack.Push(nested); else yield return null; } }
        static void Assert(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        static void Pass(string value) { report.AppendLine("PASS: "+value); File.WriteAllText("Temp/BellatorArtwork.progress.txt",report.ToString()); }
        static IEnumerator Frames(int n=3) { for(int i=0;i<n;i++) yield return null; }
        static IEnumerator Click(string name)
        {
            var g=GameObject.Find(name); Assert(g!=null,"Missing active button: "+name);
            var button=g.GetComponent<Button>(); Assert(button!=null && button.IsInteractable(),"Disabled button: "+name);
            Canvas.ForceUpdateCanvases();
            var rect=g.GetComponent<RectTransform>(); var center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);
            Assert(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Button center is obstructed: "+name+"; hits="+string.Join(", ",hits.Select(h=>h.gameObject.name+" ["+h.gameObject.transform.root.name+"]")));
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){position=center,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler); yield return Frames();
        }
        static void Field(string name,string value) => GameObject.Find(name).GetComponent<TMP_InputField>().text=value;
        static IEnumerator WaitSubmit()
        { while(!PlayerSession.HasSession && !GameObject.Find("AccountSubmitButton").GetComponent<Button>().interactable) yield return null; yield return Frames(); }
        static IEnumerator Scene(string name)
        { while(SceneManager.GetActiveScene().name!=name) yield return null; yield return Frames(8); }
        static IEnumerator Resolution(int width,int height)
        {
            var assembly=typeof(EditorWindow).Assembly; var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            sizeGroup=sizesType.GetMethod("GetGroup").Invoke(singleton,new object[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeGroupType"),0)});
            var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
                new object[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),width,height,"Bellator UI "+width+"x"+height},null);
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size}); customSizes++;
            var index=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1;
            gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(gameView,index);
            yield return Frames(8); Assert(Screen.width==width && Screen.height==height,"Resolution did not apply: "+Screen.width+"x"+Screen.height);
        }
        static IEnumerator Capture(string name)
        {
            yield return Frames(); string path=Path.Combine(evidenceDirectory,name+".png"); ScreenCapture.CaptureScreenshot(path);
            while(!File.Exists(path) || new FileInfo(path).Length==0) yield return null;
            yield return Frames(2);
        }
        static IEnumerator VerifyHover(string name)
        {
            var root=GameObject.Find(name); var frame=root.GetComponent<BellatorFrame>();
            Assert(frame!=null && root.GetComponent<BellatorButtonHover>()!=null,"Shared button style missing: "+name);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(root,pointer,ExecuteEvents.pointerEnterHandler); yield return Frames(2);
            Assert(frame.color==BellatorButtonHover.Hover,"Hover is not red: "+name);
            if(name=="PlayButton") yield return Capture("MainMenuHover1920");
            ExecuteEvents.Execute(root,pointer,ExecuteEvents.pointerDownHandler); yield return Frames(2);
            Assert(frame.color==BellatorButtonHover.Pressed,"Pressed color missing: "+name);
            ExecuteEvents.Execute(root,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(root,pointer,ExecuteEvents.pointerExitHandler);
            EventSystem.current.SetSelectedGameObject(root); yield return Frames(2);
            Assert(frame.color==BellatorButtonHover.Normal,"Keyboard selection leaves a red button: "+name);
        }
        static void CheckContainedControls()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var control in UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                if(!control.gameObject.activeInHierarchy) continue;
                var corners=new Vector3[4]; control.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach(var corner in corners) Assert(corner.x>=-1 && corner.y>=-1 && corner.x<=Screen.width+1 && corner.y<=Screen.height+1,"Clipped control: "+control.name);
            }
        }
        static IEnumerator CheckAllButtons()
        {
            foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if(!button.gameObject.activeInHierarchy) continue;
                Assert(button.GetComponent<BellatorFrame>()!=null && button.GetComponent<BellatorButtonHover>()!=null,"Unstyled menu button: "+button.name);
                if(button.IsInteractable()) yield return VerifyHover(button.name);
            }
        }
        static IEnumerator CheckLegacyAccounts()
        {
            var store=PlayerSession.Accounts;
            var legacy=store.CreateAsync("LegacyFixture","LegacyValidation47","LegacyValidation47"); while(!legacy.IsCompleted) yield return null;
            Assert(legacy.Result.Success,"Existing username-only API regressed: "+store.LastError);
            var login=store.LoginAsync("LegacyFixture","LegacyValidation47"); while(!login.IsCompleted) yield return null;
            Assert(login.Result.Success,"Existing username-only login regressed");
            var duplicate=store.CreateAsync("DuplicateFixture","Validation47","Validation47","ARTWORK@example.test"); while(!duplicate.IsCompleted) yield return null;
            Assert(!duplicate.Result.Success,"Duplicate email accepted");
            var invalid=store.CreateAsync("InvalidEmailFixture","Validation47","Validation47","bad-email"); while(!invalid.IsCompleted) yield return null;
            Assert(!invalid.Result.Success,"Invalid email accepted");
            for(int i=0;i<8;i++)
            {
                var save=store.CreateAsync("PersistenceProbe"+i,"Validation47","Validation47","probe"+i+"@example.test"); while(!save.IsCompleted) yield return null;
                Assert(save.Result.Success,"Repeated account save failed: "+store.LastError);
                var reloaded=new LocalAccountStore(store.RootPath); var check=reloaded.LoginAsync("probe"+i+"@example.test","Validation47"); while(!check.IsCompleted) yield return null;
                Assert(check.Result.Success,"Repeated account reload failed: "+reloaded.LastError);
            }
            Pass("Legacy username-only accounts remain readable/loginable; duplicate emails (case-insensitive) and invalid email rejected by the existing local store.");
            Pass("Eight additional isolated accounts created, reopened and authenticated by email to investigate an intermittent file-write failure.");
        }
        static IEnumerator SelectionFramingChecks()
        {
            yield return Frames(10);
            SelectionNavigation.Browse(); SceneManager.LoadScene(PlayerClassController.SelectionScene);
            yield return Scene("GladiatorSelection");
            var controller=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2560,1440),new Vector2Int(1024,768),new Vector2Int(2560,1080)})
            {
                yield return Resolution(size.x,size.y);
                for(int i=0;i<controller.Catalog.classes.Length;i++)
                {
                    yield return Click("CharacterButton_"+i);
                    var root=GameObject.Find("Preview_"+controller.Catalog.classes[i].id).transform;
                    CheckPreviewGeometry(root);
                    var camera=Camera.main; var position=camera.transform.position; float zoom=camera.orthographicSize;
                    yield return Frames(15);
                    Assert(camera.transform.position==position && camera.orthographicSize==zoom,"Preview framing drifted while idle");
                    yield return VerifyHover("CharacterButton_"+i);
                    Assert(UnityEngine.Object.FindAnyObjectByType<GameManager>()==null,"Selection started a match");
                    yield return Capture("Centered_"+i+"_"+size.x+"x"+size.y);
                    Pass(controller.Catalog.classes[i].displayName+" at "+size+": centered geometry, full body, stable camera, hover only red.");
                }
            }
            // Off-center pivots and extreme proportions exercise future model framing without editing assets.
            var sample=GameObject.Find("Preview_"+controller.Catalog.classes[0].id);
            controller.Select(0); yield return Frames();
            sample=GameObject.Find("Preview_"+controller.Catalog.classes[0].id);
            var originalPosition=sample.transform.position; var originalScale=sample.transform.localScale;
            foreach(var scale in new[]{new Vector3(.3f,.3f,.3f),new Vector3(3,4,2),new Vector3(4,.5f,1)})
            {
                sample.transform.position=originalPosition+new Vector3(13,7,-5); sample.transform.localScale=scale;
                controller.Select(0); yield return Frames(); CheckPreviewGeometry(sample.transform);
            }
            sample.transform.position=originalPosition; sample.transform.localScale=originalScale;
            controller.Select(0); yield return Frames();
            Pass("Offset pivot, small, tall and wide model transforms fit without editing original prefabs.");
            yield return Click("NextCharacterButton"); Assert(controller.SelectedIndex==1,"Next arrow failed");
            yield return Click("PreviousCharacterButton"); Assert(controller.SelectedIndex==0,"Previous arrow failed");
            SelectionNavigation.Play();
            var expectedClass=controller.Catalog.classes[0];
            yield return Click("ContinueButton"); yield return Scene("MainMenu");
            Assert(ClassSelectionState.Selected==expectedClass,"Selection was not saved");
            Assert(UnityEngine.Object.FindAnyObjectByType<GameManager>()==null,"Confirm launched arena");
            Pass("Confirmation from play intent returns to the menu and preserves the selected class.");
            Assert(PlayerSession.EnterGuest(),"Guest session unavailable");
            ClassSelectionState.Confirm(expectedClass);
            SelectionNavigation.Browse();
            SceneManager.LoadScene(GameInterface.MainScene); yield return Scene("MainMenu");
            yield return Click("PlayButton"); yield return Click("StoryModeButton"); yield return Scene("RomanArena");
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Assert(game!=null && !game.IsInfinite,"Story mode no longer starts from the menu");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            yield return Click("PlayButton"); yield return Click("InfiniteModeButton"); yield return Scene("RomanArena");
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(game!=null && game.IsInfinite,"Infinite mode no longer starts from the menu");
            Pass("Story and infinite modes remain playable from the menu after selection is saved.");
            game.ReturnToMenu(); yield return Scene("MainMenu");
        }
        static void CheckPreviewGeometry(Transform root)
        {
            var camera=Camera.main; var min=Vector2.one*float.PositiveInfinity; var max=Vector2.one*float.NegativeInfinity;
            var baked=new Mesh();
            try
            {
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    if(!renderer.enabled) continue;
                    Mesh mesh=null;
                    var geometryToWorld=renderer.localToWorldMatrix;
                    Vector3[] vertices=null;
                    if(renderer is SkinnedMeshRenderer skin)
                    {
                        // Independently skin the source vertices to verify the baked framing,
                        // including inherited/nonuniform scales, without repeating its calculation.
                        mesh=skin.sharedMesh; vertices=mesh.vertices;
                        var weights=mesh.boneWeights; var bind=mesh.bindposes;
                        var matrices=skin.bones.Select((bone,index)=>bone.localToWorldMatrix*bind[index]).ToArray();
                        for(int index=0;index<vertices.Length;index++)
                        {
                            var vertex=vertices[index]; var weight=weights[index];
                            vertices[index]=matrices[weight.boneIndex0].MultiplyPoint3x4(vertex)*weight.weight0+
                                matrices[weight.boneIndex1].MultiplyPoint3x4(vertex)*weight.weight1+
                                matrices[weight.boneIndex2].MultiplyPoint3x4(vertex)*weight.weight2+
                                matrices[weight.boneIndex3].MultiplyPoint3x4(vertex)*weight.weight3;
                        }
                        geometryToWorld=Matrix4x4.identity;
                    }
                    else if(renderer is MeshRenderer) mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if(mesh==null) continue;
                    foreach(var vertex in vertices??mesh.vertices)
                    {
                        var point=camera.WorldToViewportPoint(geometryToWorld.MultiplyPoint3x4(vertex));
                        Assert(point.z>camera.nearClipPlane && point.z<camera.farClipPlane,"Geometry clipped by camera depth");
                        min=Vector2.Min(min,point); max=Vector2.Max(max,point);
                    }
                }
                Assert(min.x>.04f && min.y>.04f && max.x<.96f && max.y<.96f,"Body or equipment clipped: "+min+" / "+max);
                Assert(((min+max)*.5f-Vector2.one*.5f).sqrMagnitude<.000001f,"Visible geometry is not centered");
                Assert(Mathf.Max(max.x-min.x,max.y-min.y)>.88f,"Model appears too distant");
                Assert(Quaternion.Angle(camera.transform.rotation,Quaternion.identity)<.01f,"Preview camera is tilted");
            }
            finally { UnityEngine.Object.Destroy(baked); }
        }
        static IEnumerator Checks()
        {
            yield return Frames(10); yield return Resolution(1024,768); CheckContainedControls(); yield return Capture("Login4x3");
            yield return Resolution(2560,1080); CheckContainedControls(); yield return Capture("LoginUltrawide");
            yield return Resolution(1280,720); yield return Capture("Login1280");
            yield return Resolution(2560,1440); yield return Capture("Login2560"); yield return Resolution(1920,1080);
            Assert(!PlayerSession.HasSession && GameObject.Find("AccessScreen")!=null,"Initial login missing");
            Assert(GameObject.Find("AccountPassword").GetComponent<TMP_InputField>().contentType==TMP_InputField.ContentType.Password,"Password is not masked");
            yield return Capture("Login1920");
            yield return CheckAllButtons();
            yield return VerifyHover("AccountSubmitButton");
            yield return Click("RevealPassword");
            Assert(GameObject.Find("AccountPassword").GetComponent<TMP_InputField>().contentType==TMP_InputField.ContentType.Standard,"Password visibility toggle failed");
            yield return Click("RevealPassword");
            yield return Click("PasswordHelp"); Assert(GameObject.Find("AccountError").GetComponent<TextMeshProUGUI>().text.Contains("não há recuperação"),"Local password help missing");
            yield return Click("AccountBackButton"); yield return Click("PlayButton");
            var remember=GameObject.Find("RememberUsername").GetComponent<Toggle>(); bool beforeToggle=remember.isOn;
            ExecuteEvents.Execute<IPointerClickHandler>(remember.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            Assert(remember.isOn!=beforeToggle,"Remember username toggle did not respond"); remember.isOn=true;
            yield return Click("AccountBackButton"); yield return Click("PlayButton");
            yield return Click("CreateAccountButton");
            Field("AccountUsername","ArtworkTest"); Field("AccountEmail","artwork@example.test"); Field("AccountPassword","ArtworkValidation47"); Field("AccountConfirmation","wrong");
            yield return Click("AccountSubmitButton"); yield return WaitSubmit(); Assert(!PlayerSession.HasSession,"Mismatched passwords accepted");
            Field("AccountPassword","ArtworkValidation47"); Field("AccountConfirmation","ArtworkValidation47");
            yield return Capture("Register1920"); yield return Click("AccountSubmitButton"); yield return WaitSubmit();
            Assert(PlayerSession.IsAccount,"Real local signup failed: "+(GameObject.Find("AccountError")!=null?GameObject.Find("AccountError").GetComponent<TextMeshProUGUI>().text:"no error label")+"; "+PlayerSession.Accounts.LastError);
            Assert(PlayerPrefs.GetString("Bellator.RememberedUsername")=="ArtworkTest","Username preference not saved");
            yield return Click("SwitchAccountButton");
            Field("AccountUsername","artwork@example.test"); Field("AccountPassword","wrong"); yield return Click("AccountSubmitButton"); yield return WaitSubmit();
            Assert(!PlayerSession.HasSession,"Wrong password accepted");
            Field("AccountPassword","ArtworkValidation47"); yield return Click("AccountSubmitButton"); yield return WaitSubmit(); Assert(PlayerSession.IsAccount,"Real login failed");
            Pass("TMP input masking, back, signup mismatch rejection, real signup, logout, invalid and valid login; fixtures isolated in Temp.");
            yield return CheckLegacyAccounts();
            yield return Capture("MainMenu1920"); yield return VerifyHover("PlayButton");
            Assert(GameObject.Find("MainMenu").GetComponentsInChildren<BellatorMenuIcon>().All(icon=>icon.canvasRenderer!=null),"Menu icon renderer missing");
            yield return CheckAllButtons();
            yield return Click("RankingButton"); yield return Capture("MenuRanking1920"); yield return Click("RankingBackButton");
            yield return Click("SettingsButton"); yield return Capture("Settings1920"); yield return Click("SettingsBackButton");
            yield return Click("CreditsButton"); yield return Click("CreditsBackButton");
            yield return Click("SelectCharacterButton"); yield return Scene("GladiatorSelection");
            var controller=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
            for(int i=0;i<controller.Catalog.classes.Length;i++)
            {
                yield return Click("CharacterButton_"+i); Assert(controller.SelectedIndex==i,"Selection not updated");
                yield return VerifyHover("CharacterButton_"+i);
                Assert(SceneManager.GetActiveScene().name=="GladiatorSelection" && UnityEngine.Object.FindAnyObjectByType<GameManager>()==null,"Character click auto-started a match");
                Assert(GameObject.Find("SelectedName").GetComponent<TextMeshProUGUI>().text==controller.Catalog.classes[i].displayName,"Name not updated");
                Assert(GameObject.Find("ClassPreviews").transform.Cast<Transform>().Count(t=>t.gameObject.activeSelf)==1,"Multiple previews visible");
                var preview=GameObject.Find("Live3DPreview").GetComponent<RawImage>().texture as RenderTexture;
                Assert(preview!=null && preview.IsCreated() && Camera.main.targetTexture==preview,"Live model camera disconnected");
                yield return Capture("Gladiator"+i+"1920");
            }
            Assert(GameObject.Find("StatTrack_0")==null && GameObject.Find("AbilityDetailsButton")==null,"Removed stats or abilities reintroduced");
            yield return Click("NextCharacterButton"); Assert(controller.SelectedIndex==0,"Next arrow failed to wrap");
            yield return Click("PreviousCharacterButton"); Assert(controller.SelectedIndex==3,"Previous arrow failed to wrap");
            yield return Resolution(1280,720); yield return Capture("Selection1280");
            yield return Click("CharacterButton_0"); yield return Click("ContinueButton"); yield return Scene("MainMenu");
            Assert(ClassSelectionState.Selected==AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset").classes[0],"Browse selection not saved");
            Pass("Four real 3D previews, one visible at a time; names use the real catalog; removed attributes stay absent; browsing confirms back to menu without starting a match.");
            yield return Click("PlayButton"); yield return Capture("Modes1280"); yield return Click("ModeBackButton");
            yield return Capture("MainMenu1280"); yield return Resolution(1920,1080);
            yield return Click("PlayButton"); Assert(!GameObject.Find("MultiplayerButton").GetComponent<Button>().interactable,"Unfinished multiplayer enabled"); yield return Capture("Modes1920"); yield return Click("ModeBackButton");
            ClassSelectionState.Confirm(null);
            yield return Click("PlayButton"); yield return Click("StoryModeButton"); yield return Scene("GladiatorSelection");
            yield return Click("SelectionBackButton"); yield return Scene("MainMenu"); Assert(GameObject.Find("ModeSelection")!=null,"Back did not restore mode selection");
            yield return Click("StoryModeButton"); yield return Scene("GladiatorSelection");
            yield return Click("CharacterButton_2"); yield return Click("ContinueButton"); yield return Scene("MainMenu");
            Assert(UnityEngine.Object.FindAnyObjectByType<GameManager>()==null,"Confirm must return to menu");
            yield return Click("StoryModeButton"); yield return Scene("RomanArena");
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(!game.IsInfinite && game.Waves.WaveCount==5,"Traditional mode changed");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            yield return Click("PlayButton"); yield return Click("InfiniteModeButton"); yield return Scene("RomanArena"); game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            typeof(GameManager).GetProperty(nameof(GameManager.Ranking)).SetValue(game,new LocalRankingManager(rankingPath));
            game.Waves.ManualSimulation=true; game.Player.GetComponent<ThirdPersonMotor>().AcceptInput=false;
            int safety=0;
            while(game.Statistics.wavesCompleted<1)
            {
                Assert(++safety<30,"First infinite wave stalled"); game.Waves.Tick(10);
                foreach(var enemy in game.Waves.Actors.ToArray())
                {
                    enemy.AutoThink=false; enemy.GetComponent<EnemyNavigation>().Stop();
                    if(enemy.GetComponent<CharacterHealth>().IsAlive) enemy.GetComponent<DamageReceiver>().Receive(new DamageInfo(100000,enemy.transform.position+Vector3.up,game.Player.transform.position,game.Player.transform));
                }
                yield return null;
            }
            Assert(game.Statistics.score>0,"Infinite score did not advance");
            game.Player.GetComponent<CharacterHealth>().Damage(100000); yield return Frames(5);
            var ranking=new LocalRankingManager(rankingPath);
            Assert(game.ResultSaved && ranking.Entries.Count==1 && ranking.Entries[0].profileId==PlayerSession.Profile.Id,"Authenticated score did not persist");
            Assert(game.SaveInfiniteResult() && new LocalRankingManager(rankingPath).Entries.Count==1,"Score saved twice");
            Pass("Authenticated infinite run: actual first-wave enemies, progressive score, death and automatic ranking persistence exactly once; ranking fixture isolated in Temp.");
            yield return Capture("AccountInfiniteResults"); yield return Click("ResultsRankingButton"); yield return Capture("AccountRanking"); yield return Click("RankingBackButton");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            yield return Click("SwitchAccountButton"); yield return Click("GuestButton"); Assert(!PlayerSession.IsAccount && PlayerSession.HasSession,"Guest failed");
            yield return Resolution(2560,1440); yield return Capture("Menu2560");
            yield return Click("PlayButton"); yield return Capture("Modes2560");
            yield return Click("InfiniteModeButton"); yield return Scene("GladiatorSelection");
            yield return Capture("Selection2560"); yield return Click("ContinueButton"); yield return Scene("MainMenu");
            yield return Click("InfiniteModeButton"); yield return Scene("RomanArena");
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            typeof(GameManager).GetProperty(nameof(GameManager.Ranking)).SetValue(game,new LocalRankingManager(rankingPath));
            Assert(game.IsInfinite && !game.CanRecordResult && !game.SaveInfiniteResult(),"Guest ranking restriction failed");
            game.Player.GetComponent<CharacterHealth>().Damage(100000); yield return Frames(5);
            Assert(!game.ResultSaved && GameObject.Find("SaveScoreButton")==null,"Guest score saved or submit enabled");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            Pass("Ranking/settings/credits buttons, mode Back, selection Back, 5-wave traditional mode, infinite mode and guest exclusion from ranking; 1280x720, 1920x1080 and 2560x1440; button centers verified by EventSystem raycasts.");
            Pass("Dark shared buttons turn red on pointer enter, darker on press and return dark on exit even while selected. Password reveal, catalog arrows and disabled multiplayer verified. Controls contained at 4:3 and ultrawide.");
            Assert(errors.Count==0,"Console errors: "+string.Join("; ",errors));
            Pass("No Error/Exception/Assert logged during UI tests.");
            SessionState.SetBool(Active+".Quit",true); yield return Click("QuitButton");
        }
    }
}

