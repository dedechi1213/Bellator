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
            deadline=EditorApplication.timeSinceStartup+300; routine=Flatten(Checks()); frame=-1;
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
            Directory.CreateDirectory("Assets/Game/Documentation/BellatorUI");
            // Capture outside Assets so the importer cannot lock an output while it is written.
            AssetDatabase.StartAssetEditing();
            try { foreach(var capture in Directory.GetFiles(evidenceDirectory,"*.png")) File.Copy(capture,Path.Combine("Assets/Game/Documentation/BellatorUI",Path.GetFileName(capture)),true); }
            finally { AssetDatabase.StopAssetEditing(); }
            File.WriteAllText("Assets/Game/Documentation/BellatorUI/Validation.txt",report.ToString());
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
            button.onClick.Invoke(); yield return Frames();
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
        static IEnumerator Checks()
        {
            yield return Frames(10); yield return Resolution(1280,720); yield return Capture("Login1280");
            yield return Resolution(2560,1440); yield return Capture("Login2560"); yield return Resolution(1920,1080);
            Assert(!PlayerSession.HasSession && GameObject.Find("AccessScreen")!=null,"Initial login missing");
            Assert(GameObject.Find("AccountPassword").GetComponent<TMP_InputField>().contentType==TMP_InputField.ContentType.Password,"Password is not masked");
            yield return Capture("Login1920");
            yield return Click("PasswordHelp"); Assert(GameObject.Find("AccountError").GetComponent<TextMeshProUGUI>().text.Contains("não há recuperação"),"Local password help missing");
            yield return Click("LoginButton");
            var remember=GameObject.Find("RememberUsername").GetComponent<Toggle>(); bool beforeToggle=remember.isOn;
            ExecuteEvents.Execute<IPointerClickHandler>(remember.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            Assert(remember.isOn!=beforeToggle,"Remember username toggle did not respond"); remember.isOn=true;
            yield return Click("AccountBackButton"); yield return Click("PlayButton");
            yield return Click("CreateAccountButton");
            Field("AccountUsername","ArtworkTest"); Field("AccountPassword","ArtworkValidation47"); Field("AccountConfirmation","wrong");
            yield return Click("AccountSubmitButton"); yield return WaitSubmit(); Assert(!PlayerSession.HasSession,"Mismatched passwords accepted");
            Field("AccountPassword","ArtworkValidation47"); Field("AccountConfirmation","ArtworkValidation47");
            yield return Capture("Register1920"); yield return Click("AccountSubmitButton"); yield return WaitSubmit();
            Assert(PlayerSession.IsAccount,"Real local signup failed: "+(GameObject.Find("AccountError")!=null?GameObject.Find("AccountError").GetComponent<TextMeshProUGUI>().text:"no error label"));
            Assert(PlayerPrefs.GetString("Bellator.RememberedUsername")=="ArtworkTest","Username preference not saved");
            yield return Click("SwitchAccountButton");
            Field("AccountUsername","ArtworkTest"); Field("AccountPassword","wrong"); yield return Click("AccountSubmitButton"); yield return WaitSubmit();
            Assert(!PlayerSession.HasSession,"Wrong password accepted");
            Field("AccountPassword","ArtworkValidation47"); yield return Click("AccountSubmitButton"); yield return WaitSubmit(); Assert(PlayerSession.IsAccount,"Real login failed");
            Pass("TMP input masking, back, signup mismatch rejection, real signup, logout, invalid and valid login; fixtures isolated in Temp.");
            yield return Capture("MainMenu1920");
            yield return Click("RankingButton"); yield return Click("RankingBackButton");
            yield return Click("SettingsButton"); yield return Click("SettingsBackButton");
            yield return Click("CreditsButton"); yield return Click("CreditsBackButton");
            yield return Click("SelectCharacterButton"); yield return Scene("GladiatorSelection");
            var controller=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
            for(int i=0;i<controller.Catalog.classes.Length;i++)
            {
                yield return Click("CharacterButton_"+i); Assert(controller.SelectedIndex==i,"Selection not updated");
                Assert(SceneManager.GetActiveScene().name=="GladiatorSelection" && UnityEngine.Object.FindAnyObjectByType<GameManager>()==null,"Character click auto-started a match");
                Assert(GameObject.Find("SelectedName").GetComponent<TextMeshProUGUI>().text==controller.Catalog.classes[i].displayName,"Name not updated");
                Assert(GameObject.Find("ClassPreviews").transform.Cast<Transform>().Count(t=>t.gameObject.activeSelf)==1,"Multiple previews visible");
                var preview=GameObject.Find("Live3DPreview").GetComponent<RawImage>().texture as RenderTexture;
                Assert(preview!=null && preview.IsCreated() && Camera.main.targetTexture==preview,"Live model camera disconnected");
                yield return Capture("Gladiator"+i+"1920");
            }
            yield return Click("AbilityDetailsButton");
            yield return Resolution(1280,720); yield return Capture("Selection1280");
            yield return Click("CharacterButton_0"); yield return Click("ContinueButton"); yield return Scene("MainMenu");
            Assert(ClassSelectionState.Selected==AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset").classes[0],"Browse selection not saved");
            Pass("Four real 3D previews, one visible at a time; names, equipment, attributes and abilities use the catalog; browsing confirms back to menu without starting a match.");
            yield return Click("PlayButton"); yield return Capture("Modes1280"); yield return Click("ModeBackButton");
            yield return Capture("MainMenu1280"); yield return Resolution(1920,1080);
            yield return Click("PlayButton"); yield return Capture("Modes1920"); yield return Click("ModeBackButton");
            yield return Click("PlayButton"); yield return Click("StoryModeButton"); yield return Scene("GladiatorSelection");
            yield return Click("SelectionBackButton"); yield return Scene("MainMenu"); Assert(GameObject.Find("ModeSelection")!=null,"Back did not restore mode selection");
            yield return Click("StoryModeButton"); yield return Scene("GladiatorSelection");
            yield return Click("CharacterButton_2"); yield return Click("ContinueButton"); yield return Scene("RomanArena");
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); Assert(!game.IsInfinite && game.Waves.WaveCount==5,"Traditional mode changed");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            yield return Click("PlayButton"); yield return Click("InfiniteModeButton"); yield return Scene("GladiatorSelection");
            yield return Click("ContinueButton"); yield return Scene("RomanArena"); game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
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
            yield return Capture("Selection2560"); yield return Click("ContinueButton"); yield return Scene("RomanArena");
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            typeof(GameManager).GetProperty(nameof(GameManager.Ranking)).SetValue(game,new LocalRankingManager(rankingPath));
            Assert(game.IsInfinite && !game.CanRecordResult && !game.SaveInfiniteResult(),"Guest ranking restriction failed");
            game.Player.GetComponent<CharacterHealth>().Damage(100000); yield return Frames(5);
            Assert(!game.ResultSaved && GameObject.Find("SaveScoreButton")==null,"Guest score saved or submit enabled");
            game.ReturnToMenu(); yield return Scene("MainMenu");
            Pass("Ranking/settings/credits buttons, mode Back, selection Back, 5-wave traditional mode, infinite mode and guest exclusion from ranking; 1280x720, 1920x1080 and 2560x1440; button centers verified by EventSystem raycasts.");
            Assert(errors.Count==0,"Console errors: "+string.Join("; ",errors));
            Pass("No Error/Exception/Assert logged during UI tests.");
            SessionState.SetBool(Active+".Quit",true); yield return Click("QuitButton");
        }
    }
}
