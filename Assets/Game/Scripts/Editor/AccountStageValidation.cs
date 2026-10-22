using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class AccountStageValidation
    {
        const string Active="Bellator.Accounts.Test",Password="Validation-only-47";
        static IEnumerator routine;static int frame=-1;static double deadline;
        static string root,rankingPath;
        static readonly StringBuilder report=new StringBuilder();static readonly List<string> errors=new List<string>();
        static AccountStageValidation() { EditorApplication.update+=Check;EditorApplication.playModeStateChanged+=State; }
        static void Check()
        {
            if(routine!=null && EditorApplication.isPlaying)
            {
                if(EditorApplication.isPaused) EditorApplication.isPaused=false;
                if(EditorApplication.timeSinceStartup>deadline)
                { var label=GameObject.Find("AccountError");Finish(new TimeoutException("Account validation timeout; UI="+(label!=null?label.GetComponent<Text>().text:"no account form")+"; session="+PlayerSession.HasSession));return; }
            }
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Account validation timeout");if(!routine.MoveNext()) Finish(null); }catch(Exception error) { Finish(error); }
            }
            if(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorAccounts.test")) return;
            File.Delete("Temp/BellatorAccounts.test");Run();
        }
        [MenuItem("Bellator/Test Local Accounts and Guest Mode")]
        public static void Run()
        { if(EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;EditorSceneManager.OpenScene(GameInterface.MainScene);SessionState.SetBool(Active,true);EditorApplication.EnterPlaymode(); }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active+".Reopen",false)) { SessionState.SetBool(Active+".Reopen",false);EditorSceneManager.OpenScene(GameInterface.MainScene); }
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear();errors.Clear();report.AppendLine("Bellator accounts — Unity "+Application.unityVersion+" — real Play Mode, local PBKDF2, native UI and scene transitions");
            root=Path.GetFullPath("Temp/AccountValidation/"+Guid.NewGuid().ToString("N"));rankingPath=Path.Combine(root,"ranking.json");
            typeof(PlayerSession).GetField("accounts",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,new LocalAccountStore(Path.Combine(root,"Accounts")));
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            Application.logMessageReceived+=Log;deadline=EditorApplication.timeSinceStartup+300;routine=Flatten(Checks());frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("MissingReferenceException") || message.Contains("NavMeshAgent"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null;SessionState.SetBool(Active,false);Time.timeScale=1;report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/AccountStageValidation.txt",report.ToString());File.WriteAllText("Temp/BellatorAccounts.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error);else Debug.Log("Bellator local accounts and guest validation passed.");
            SessionState.SetBool(Active+".Reopen",true);EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator rootRoutine)
        { var stack=new Stack<IEnumerator>();stack.Push(rootRoutine);while(stack.Count>0) { var top=stack.Peek();if(!top.MoveNext()) {stack.Pop();continue;}if(top.Current is IEnumerator nested) {stack.Push(nested);continue;}yield return null;} }
        static void Assert(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message);File.WriteAllText("Temp/BellatorAccounts.progress.txt",report.ToString()); }
        static IEnumerator Click(string name)
        {
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            yield return null;yield return null;
            Canvas.ForceUpdateCanvases();var target=GameObject.Find(name);Assert(target!=null,"Missing button "+name);var button=target.GetComponent<Button>();Assert(button.IsInteractable(),"Disabled button "+name);
            var rect=target.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);Assert(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Occluded "+name+" at "+point+"; hits="+string.Join(",",hits.Select(hit=>hit.gameObject.name)));
            button.onClick.Invoke();yield return null;yield return null;
        }
        static void Field(string name,string value) => GameObject.Find(name).GetComponent<InputField>().text=value;
        static IEnumerator Scene(string name) { while(SceneManager.GetActiveScene().name!=name) yield return null;yield return null;yield return null; }
        static IEnumerator WaitSubmit() { while(!GameObject.Find("AccountSubmitButton").GetComponent<Button>().interactable) yield return null;yield return null;yield return null; }
        static IEnumerator WaitAccount()
        {
            while(!PlayerSession.IsAccount)
            {
                var button=GameObject.Find("AccountSubmitButton");var error=GameObject.Find("AccountError");
                if(button!=null && button.GetComponent<Button>().interactable && error!=null && error.GetComponent<Text>().text.Length>0)
                    throw new InvalidOperationException("Account did not sign in: "+error.GetComponent<Text>().text);
                yield return null;
            }
            yield return null;yield return null;
        }
        static IEnumerator Wait(Task task) { while(!task.IsCompleted) yield return null;if(task.IsFaulted) throw task.Exception; }
        static void TestRanking(GameManager game) => typeof(GameManager).GetProperty(nameof(GameManager.Ranking)).SetValue(game,new LocalRankingManager(rankingPath));
        static IEnumerator Capture(string name) { ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Accounts_"+name+".png");yield return null;yield return null; }
        static IEnumerator FirstWave(GameManager game)
        {
            var waves=game.Waves;waves.ManualSimulation=true;game.Player.GetComponent<ThirdPersonMotor>().AcceptInput=false;
            int safety=0;while(game.Statistics.wavesCompleted<1)
            {
                Assert(++safety<30,"First wave did not finish");waves.Tick(10);
                foreach(var enemy in waves.Actors.ToArray())
                {
                    enemy.AutoThink=false;enemy.GetComponent<EnemyNavigation>().Stop();
                    if(enemy.GetComponent<CharacterHealth>().IsAlive) Assert(enemy.GetComponent<DamageReceiver>().Receive(new DamageInfo(100000,enemy.transform.position+Vector3.up,game.Player.transform.position,game.Player.transform)),"Shared enemy kill failed");
                }
                yield return null;
            }
        }
        static IEnumerator Checks()
        {
            yield return null;yield return null;
            Assert(!PlayerSession.HasSession && GameObject.Find("AccessScreen")!=null && GameObject.Find("PlayButton")==null,"Initial access gate bypassed");
            var note=GameObject.Find("GuestNotice").GetComponent<Text>();var guest=GameObject.Find("GuestButton");
            Assert(note.transform.parent==guest.transform.parent && note.fontSize<guest.GetComponentInChildren<Text>().fontSize && note.GetComponent<RectTransform>().anchorMax.y<guest.GetComponent<RectTransform>().anchorMin.y && note.resizeTextMinSize>=14,"Guest notice size/position/readability");
            yield return Capture("Access");
            Pass("Initial access precedes main menu; guest notice is a separate, smaller, readable label below the guest button.");
            yield return Click("CreateAccountButton");
            Assert(GameObject.Find("AccountPassword").GetComponent<InputField>().contentType==InputField.ContentType.Password && GameObject.Find("AccountConfirmation").GetComponent<InputField>().contentType==InputField.ContentType.Password,"Passwords not masked");
            yield return Click("AccountSubmitButton");Assert(!PlayerSession.HasSession && GameObject.Find("AccountError").GetComponent<Text>().text.Length>0,"Empty username accepted");
            Field("AccountUsername","ArenaOne");yield return Click("AccountSubmitButton");Assert(!PlayerSession.HasSession,"Empty password accepted");
            Field("AccountPassword",Password);Field("AccountConfirmation","different");yield return Click("AccountSubmitButton");Assert(GameObject.Find("AccountError").GetComponent<Text>().text.Contains("iguais"),"Password mismatch not reported");
            yield return Capture("Create");Field("AccountPassword",Password);Field("AccountConfirmation",Password);yield return Click("AccountSubmitButton");yield return WaitAccount();
            string firstId=PlayerSession.Profile.Id;
            Assert(GameObject.Find("ConnectedPlayer").GetComponent<Text>().text=="Jogador: ArenaOne" && GameObject.Find("AccessScreen")==null,"Create did not auto-login/show menu");
            var accountScore=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro");accountScore.BeginWave(1);accountScore.Defeated(EnemyRole.Gladiator,1);var firstSnapshot=accountScore.Finish();
            yield return Capture("Menu");
            var store=PlayerSession.Accounts;var missing=new LocalAccountStore(Path.Combine(root,"Missing"));var missingLogin=missing.LoginAsync("Nobody",Password);yield return Wait(missingLogin);Assert(!missingLogin.Result.Success,"Missing accounts file authenticated");
            var duplicate=store.CreateAsync("  arenaONE  ",Password,Password);yield return Wait(duplicate);Assert(!duplicate.Result.Success,"Duplicate case/trim name accepted");
            var empty=store.CreateAsync("EmptyPassword","","");yield return Wait(empty);Assert(!empty.Result.Success,"Empty password accepted in storage");
            Pass("Account creation validates empty names/passwords and confirmation, masks passwords, auto-signs in and rejects normalized duplicate names.");
            yield return Click("SwitchAccountButton");Assert(!PlayerSession.HasSession,"Logout retained identity");
            yield return Click("LoginButton");Field("AccountUsername","ArenaOne");Field("AccountPassword","wrong");yield return Click("AccountSubmitButton");yield return WaitSubmit();
            string error=GameObject.Find("AccountError").GetComponent<Text>().text;
            Field("AccountUsername","UnknownUser");Field("AccountPassword","wrong");yield return Click("AccountSubmitButton");yield return WaitSubmit();
            Assert(error==LocalAccountStore.InvalidCredentials && GameObject.Find("AccountError").GetComponent<Text>().text==error && !PlayerSession.HasSession,"Login disclosed username or accepted wrong password");
            Field("AccountUsername","arenaone");Field("AccountPassword",Password);yield return Click("AccountSubmitButton");yield return WaitAccount();Assert(PlayerSession.Profile.Id==firstId,"Login changed profile ID");
            ClassSelectionState.Confirm(AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset").classes[0]);
            yield return Click("SwitchAccountButton");yield return Click("CreateAccountButton");Field("AccountUsername","ArenaTwo");Field("AccountPassword",Password);Field("AccountConfirmation",Password);yield return Click("AccountSubmitButton");yield return WaitAccount();
            string secondId=PlayerSession.Profile.Id;Assert(firstId!=secondId && ClassSelectionState.Selected==null,"Accounts mixed identity or class selection");
            var ranking=new LocalRankingManager(rankingPath);Assert(!ranking.Submit(firstSnapshot,"ArenaTwo") && !accountScore.SaveResult(ranking),"Session swap allowed another account's result");
            var forged=firstSnapshot.Copy();forged.profileId=secondId;forged.playerName="ArenaTwo";Assert(!ranking.Submit(forged),"Name/profile ID edit bypassed run session binding");
            yield return Click("SwitchAccountButton");yield return Click("LoginButton");Field("AccountUsername","ArenaOne");Field("AccountPassword",Password);yield return Click("AccountSubmitButton");yield return WaitAccount();
            Assert(PlayerSession.Profile.Id==firstId,"First profile data mixed");
            Pass("Correct login restores stable identity; unknown names and wrong passwords share a generic error; logout preserves profiles; two accounts remain isolated; editing result names/IDs or reusing an old session cannot submit a result.");
            yield return Click("PlayButton");yield return Click("InfiniteModeButton");yield return Scene("RomanArena");
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();TestRanking(game);
            Assert(!PlayerSession.Logout() && !PlayerSession.EnterGuest(),"Account switch allowed inside a match");
            yield return FirstWave(game);game.Player.GetComponent<CharacterHealth>().Damage(100000);yield return null;yield return null;
            ranking=new LocalRankingManager(rankingPath);
            Assert(game.ResultSaved && ranking.Entries.Count==1 && ranking.Entries[0].profileId==firstId && ranking.Entries[0].playerName=="ArenaOne" && ranking.Entries[0].score==800,"Account death did not automatically record account identity/statistics");
            Assert(GameObject.Find("PlayerNameInput")==null && game.SaveInfiniteResult("OtherName") && new LocalRankingManager(rankingPath).Entries.Count==1,"Name reentry/duplicate result");
            yield return Capture("AccountResults");yield return Click("ResultsMenuButton");yield return Scene("MainMenu");yield return Click("SwitchAccountButton");yield return Click("GuestButton");
            Assert(!PlayerSession.IsAccount && PlayerSession.HasSession && GameObject.Find("ConnectedPlayer").GetComponent<Text>().text=="Jogando como convidado","Guest session label");
            Pass("Infinite match with an account auto-saves exactly once with stable profile ID and account name, without asking for a name; session changes are rejected during a match.");
            yield return Click("RankingButton");Assert(GameObject.Find("LocalRanking")!=null,"Guest cannot consult ranking");yield return Click("RankingBackButton");
            string before=File.ReadAllText(rankingPath);yield return Click("PlayButton");yield return Click("InfiniteModeButton");yield return Scene("RomanArena");
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>();TestRanking(game);yield return FirstWave(game);game.Player.GetComponent<CharacterHealth>().Damage(100000);yield return null;yield return null;
            Assert(game.Statistics.score==800 && !game.ResultSaved && !game.CanRecordResult && !game.SaveInfiniteResult("GuestFake"),"Guest score registration allowed");
            var guestScore=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro");var guestStats=guestScore.Finish();
            Assert(!guestScore.SaveResult(new LocalRankingManager(rankingPath)) && !new LocalRankingManager(rankingPath).Submit(guestStats,"ArenaOne"),"Backend accepted guest bypass");
            Assert(GameObject.Find("PlayerNameInput")==null && GameObject.Find("SaveScoreButton")==null && GameObject.Find("SaveStatus").GetComponent<Text>().text=="Crie uma conta para registrar seus recordes no ranking.","Guest result UI requests registration/name");
            yield return Capture("GuestResults");yield return Click("ResultsRankingButton");Assert(GameObject.Find("RankingRow0")!=null,"Guest cannot see existing shared records");yield return Click("RankingBackButton");yield return Click("ResultsMenuButton");yield return Scene("MainMenu");
            Assert(File.ReadAllText(rankingPath)==before,"Guest changed ranking file");
            yield return Click("PlayButton");yield return Click("StoryModeButton");yield return Scene("RomanArena");game=UnityEngine.Object.FindAnyObjectByType<GameManager>();game.Waves.ManualSimulation=true;
            Assert(!game.IsInfinite && game.Waves.WaveCount==5 && !game.Gate.IsOpen,"Story unavailable/changed for guest");game.ReturnToMenu();yield return Scene("MainMenu");
            Pass("Guests play infinite with normal scoring/statistics and story with the original five-wave schedule, can read the shared ranking, and cannot save through UI, GameManager, ScoreManager or LocalRankingManager. Ranking JSON remained byte-for-byte unchanged.");
            yield return Click("SwitchAccountButton");
            var credentialFiles=Directory.GetFiles(store.RootPath,"credentials.bin",SearchOption.AllDirectories);Assert(credentialFiles.Length==2,"Incorrect credential file count");
            var salts=new List<string>();
            foreach(string file in credentialFiles)
            {
                Assert(!Encoding.UTF8.GetString(File.ReadAllBytes(file)).Contains(Password),"Plaintext password persisted");
                using(var reader=new BinaryReader(File.OpenRead(file))) {reader.ReadInt32();reader.ReadInt32();reader.ReadString();reader.ReadString();Assert(reader.ReadInt32()==600000,"Incorrect PBKDF2 work factor");salts.Add(Convert.ToBase64String(reader.ReadBytes(16)));}
            }
            Assert(salts.Distinct().Count()==2,"Accounts reused salt");
            foreach(string file in Directory.GetFiles(store.RootPath,"*.json",SearchOption.AllDirectories)) Assert(!File.ReadAllText(file).Contains(Password),"Profile JSON contains password");
            var reloaded=new LocalAccountStore(store.RootPath);var login=reloaded.LoginAsync("ArenaOne",Password);yield return Wait(login);Assert(login.Result.Success && login.Result.Profile.Id==firstId,"Fresh store failed profile persistence");
            string firstFolder=Path.Combine(store.RootPath,firstId);File.WriteAllText(Path.Combine(firstFolder,"profile.json"),"broken");File.WriteAllText(Path.Combine(firstFolder,"credentials.bin"),"broken");
            login=new LocalAccountStore(store.RootPath).LoginAsync("ArenaOne",Password);yield return Wait(login);Assert(login.Result.Success && login.Result.Profile.Id==firstId,"Profile/credential backup recovery failed");
            File.WriteAllText(Path.Combine(firstFolder,"credentials.bin.bak"),"broken");
            var damaged=new LocalAccountStore(store.RootPath);login=damaged.LoginAsync("ArenaOne",Password);yield return Wait(login);Assert(!login.Result.Success,"Corrupt credentials authenticated");
            var blocked=damaged.CreateAsync("NewAccount",Password,Password);yield return Wait(blocked);Assert(!blocked.Result.Success,"Creation overwrote/ignored damaged identity data");
            string fileBlock=Path.Combine(root,"NotADirectory");File.WriteAllText(fileBlock,"file");blocked=new LocalAccountStore(Path.Combine(fileBlock,"Accounts")).CreateAsync("WriteFail",Password,Password);yield return Wait(blocked);Assert(!blocked.Result.Success,"Account write failure not handled");
            Assert(PlayerSession.EnterGuest(),"Corruption prevented guest access");
            Pass("Separate profile JSON and binary PBKDF2-HMAC-SHA256 credentials; 600,000 iterations; distinct 16-byte salts; no plaintext passwords in persisted files; fresh-store login, backup recovery, invalid credentials, failed writes and preservation of damaged profiles verified. Guest access survives account storage errors.");
        }
    }
}
