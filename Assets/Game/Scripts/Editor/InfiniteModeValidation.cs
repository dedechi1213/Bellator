using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Enemies;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    /// <summary>Opt-in real scene tests. Ranking fixtures stay in Temp, never in the player's leaderboard.</summary>
    [InitializeOnLoad]
    public static class InfiniteModeValidation
    {
        const string Active="Bellator.Infinite.Test";
        static IEnumerator routine;
        static int frame=-1;
        static double deadline;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static InputSettings.EditorInputBehaviorInPlayMode previousInput;
        static string rankingPath;
        static InfiniteModeValidation() { EditorApplication.update+=Check; EditorApplication.playModeStateChanged+=State; }
        static void Check()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            {
                frame=Time.frameCount;
                try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Infinite validation timed out"); if(!routine.MoveNext()) Finish(null); }
                catch(Exception e) { Finish(e); }
            }
            if(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/BellatorInfinite.test")) return;
            File.Delete("Temp/BellatorInfinite.test"); Run();
        }
        [MenuItem("Bellator/Test Infinite Mode and Local Ranking")]
        public static void Run()
        {
            if(EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(GameInterface.MainScene); SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active+".Reopen",false))
            { SessionState.SetBool(Active+".Reopen",false); EditorSceneManager.OpenScene(GameInterface.MainScene); }
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear(); errors.Clear(); report.AppendLine("Bellator infinite mode — Unity "+Application.unityVersion+" — real Play Mode, NavMesh, shared damage and native UI");
            previousInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            rankingPath=Path.GetFullPath("Temp/InfiniteRankingTests/"+Guid.NewGuid().ToString("N")+"/ranking.json");
            Application.logMessageReceived+=Log; deadline=EditorApplication.timeSinceStartup+300; routine=Flatten(Checks()); frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Error || type==LogType.Exception || type==LogType.Assert) && (trace.Contains("Bellator.") || message.Contains("NavMeshAgent") || message.Contains("MissingReferenceException"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;
            if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null; SessionState.SetBool(Active,false); Time.timeScale=1;
            InputSystem.settings.editorInputBehaviorInPlayMode=previousInput;
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/InfiniteModeValidation.txt",report.ToString());
            File.WriteAllText("Temp/BellatorInfinite.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error); else Debug.Log("Bellator infinite-mode validation passed.");
            SessionState.SetBool(Active+".Reopen",true); EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>(); stack.Push(root);
            while(stack.Count>0) { var top=stack.Peek(); if(!top.MoveNext()) { stack.Pop();continue; } if(top.Current is IEnumerator nested) { stack.Push(nested);continue; } yield return null; }
        }
        static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message); File.WriteAllText("Temp/BellatorInfinite.progress.txt",report.ToString()); }
        static IEnumerator Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var go=GameObject.Find(name); Assert(go!=null,"Missing button "+name); var button=go.GetComponent<Button>(); Assert(button.IsInteractable(),"Disabled button "+name);
            var rect=go.GetComponent<RectTransform>(); var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Occluded button "+name+" at "+point+"; hits="+string.Join(",",hits.Select(h=>h.gameObject.name))); button.onClick.Invoke(); yield return null; yield return null;
        }
        static IEnumerator Scene(string name)
        { while(SceneManager.GetActiveScene().name!=name) yield return null; yield return null; yield return null; }
        static void SetTestRanking(GameManager game)
        { typeof(GameManager).GetProperty(nameof(GameManager.Ranking)).SetValue(game,new LocalRankingManager(rankingPath)); }
        static MatchStatistics Fixture(long points,int completed,double time)
        {
            var fixture=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro").Finish();
            fixture.score=points;fixture.highestWave=completed+1;fixture.wavesCompleted=completed;fixture.enemiesDefeated=completed*3;fixture.survivalSeconds=time;
            return fixture;
        }
        static void DataChecks()
        {
            var score=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro");
            for(int wave=1;wave<=31;wave++)
            {
                long before=score.Current.score; score.BeginWave(wave);
                foreach(EnemyRole role in Enum.GetValues(typeof(EnemyRole))) score.Defeated(role,wave);
                long expected=(long)Math.Round(450d*ScoreManager.Multiplier(wave));
                Assert(score.Current.score-before==expected,"Wrong enemy multiplier "+wave);
                before=score.Current.score; score.CompleteWave(wave); score.CompleteWave(wave);
                Assert(score.Current.score-before==500L+wave*100L,"Duplicate/incorrect wave bonus "+wave);
            }
            score.Tick(3); var snapshot=score.Finish(); score.Defeated(EnemyRole.Heavy,31); score.Tick(30);
            Assert(snapshot.score==score.Current.score && snapshot.survivalSeconds==3,"Finished score changed");
            var difficulty=new DifficultyScaler(); float hp=0,damage=0;
            foreach(int wave in new[]{1,5,6,10,11,20,21,30,31,100,1000,int.MaxValue})
            {
                Assert(difficulty.HealthMultiplier(wave)>=hp && difficulty.DamageMultiplier(wave)>=damage,"Non-monotonic difficulty");
                hp=difficulty.HealthMultiplier(wave); damage=difficulty.DamageMultiplier(wave);
                Assert(!float.IsInfinity(hp) && !float.IsNaN(hp) && difficulty.EnemyCount(wave)<=90 && difficulty.AttackRate(wave)<=1.8f,"Unsafe advanced scaling");
            }
            var repository=new LocalRankingManager(rankingPath); Assert(repository.Entries.Count==0,"Missing file not empty");
            var sample=Fixture(1000,3,60); Assert(repository.Submit(sample,"  <Arena>  "),"Ranking save failed"); Assert(repository.Submit(sample,"Duplicate"),"Idempotent save failed");
            var reloaded=new LocalRankingManager(rankingPath); Assert(reloaded.Entries.Count==1 && reloaded.Entries[0].playerName==PlayerSession.Username,"JSON round-trip/account identity/duplicate");
            File.WriteAllText(rankingPath,"{corrupted"); reloaded.Reload(); Assert(reloaded.Entries.Count==1 && reloaded.LastError!=null,"Backup recovery failed");
            Assert(reloaded.Submit(Fixture(1100,4,70),""),"Recovery write failed");
            File.WriteAllText(rankingPath,"{}"); File.WriteAllText(rankingPath+".bak","[]"); reloaded.Reload(); Assert(reloaded.Entries.Count==0 && reloaded.LastError!=null,"Invalid file accepted");
            for(int i=0;i<14;i++) Assert(reloaded.Submit(Fixture(i*100,2,i),""),"Top ten write failed");
            Assert(reloaded.Entries.Count==10 && reloaded.BestScore==1300 && reloaded.Entries[0].playerName==PlayerSession.Username,"Top ten/account name/order");
            var a=Fixture(3000,3,90); var b=Fixture(3000,4,10); var c=Fixture(3000,4,30);
            reloaded.Submit(a,"A");reloaded.Submit(b,"B");reloaded.Submit(c,"C");
            Assert(reloaded.Entries[0].matchId==c.matchId && reloaded.Entries[1].matchId==b.matchId,"Tie-break rules failed");
            var tie=c.Copy(); tie.matchId=Guid.NewGuid().ToString("N"); Assert(LocalRankingManager.Compare(c,tie)==-LocalRankingManager.Compare(tie,c),"Tie not deterministic");
            var story=Fixture(99999,5,300); story.mode=GameMode.Story; Assert(!reloaded.Submit(story,"Story"),"Story leaked into ranking");
            var invalid=Fixture(1,1,double.NaN); Assert(!reloaded.Submit(invalid,"Invalid"),"Invalid time accepted");
            string blocked=Path.Combine(Path.GetDirectoryName(rankingPath),"blocked"); File.WriteAllText(blocked,"file");
            var unavailable=new LocalRankingManager(Path.Combine(blocked,"ranking.json")); Assert(!unavailable.Submit(sample,"Write fail") && unavailable.LastError!=null,"Write failure not handled");
            Pass("Score tiers 1–31, separate wave bonus, no duplicate wave rewards, frozen final snapshot; monotonic finite difficulty through int.MaxValue; JSON reload, duplicate IDs, backup recovery, invalid/corrupt data, write failure, top ten, account-bound names, deterministic ties and story exclusion.");
        }
        static void Freeze(WaveManager waves)
        { foreach(var actor in waves.Actors) if(actor!=null) { actor.AutoThink=false; actor.GetComponent<EnemyNavigation>().Stop(); } }
        static IEnumerator ClearWave(GameManager game,int expectedWave)
        {
            var waves=game.Waves; var settings=waves.InfiniteDifficulty;
            Assert(waves.WaveNumber==expectedWave,"Unexpected next wave");
            int expected=settings.EnemyCount(expectedWave),kills=0,peak=0; long points=0,scoreBefore=game.Statistics.score;
            // Never skip the interval: a tick that leaves 0.1 seconds must not spawn.
            if(waves.Countdown>.11f) { waves.Tick(waves.Countdown-.1f); Assert(waves.AliveCount==0 && waves.PendingCount==0,"Spawned during countdown"); }
            waves.Tick(.11f);
            int safety=0;
            while(waves.State==WaveState.Spawning || waves.State==WaveState.Fighting)
            {
                Assert(++safety<expected*8+30,"Spawn loop stuck, wave "+expectedWave);
                Freeze(waves); peak=Math.Max(peak,waves.AliveCount);
                Assert(waves.AliveCount<=settings.maximumActiveEnemies && waves.Actors.Count<=settings.maximumActiveEnemies,"Active/list cap exceeded");
                if(waves.AliveCount==settings.maximumActiveEnemies || waves.PendingCount==0)
                {
                    foreach(var actor in waves.Actors.ToArray())
                    {
                        var health=actor.GetComponent<CharacterHealth>(); if(!health.IsAlive) continue;
                        Assert(Mathf.Approximately(health.Maximum,actor.Definition.maximumHealth*settings.HealthMultiplier(expectedWave)),"Enemy health scaling mismatch");
                        Assert(actor.GetComponent<ActorAttributes>().DamageMultiplier(false)>=(expectedWave>1?1:1),"Damage modifier absent");
                        Assert(actor.GetComponent<EnemyNavigation>().Ready,"Spawn outside NavMesh");
                        var receiver=actor.GetComponent<DamageReceiver>();
                        Assert(receiver.Receive(new DamageInfo(10000000,actor.transform.position+Vector3.up,game.Player.transform.position,game.Player.transform)),"Shared lethal hit failed");
                        kills++; points+=ScoreManager.EnemyPoints(actor.Definition.role,expectedWave);
                        long once=game.Statistics.score; health.Damage(10000000); Assert(game.Statistics.score==once,"Duplicate death points");
                    }
                }
                waves.Tick(1); yield return null; Freeze(waves);
            }
            Assert(kills==expected && waves.DefeatedInWave==expected && waves.AliveCount==0 && waves.PendingCount==0,"Wave kill/group totals mismatch");
            Assert(game.Statistics.score-scoreBefore==points+ScoreManager.WaveBonus(expectedWave),"Incorrect real-wave score");
            Assert(game.Statistics.wavesCompleted==expectedWave && !game.Gate.IsOpen && game.State==MatchState.Intermission,"Infinite gate/state/clears");
            Assert(waves.Actors.Count==0,"Corpses retained after wave");
            report.AppendLine("  wave "+expectedWave+": "+kills+" kills; peak "+peak+"; score +"+(game.Statistics.score-scoreBefore));
        }
        static IEnumerator Checks()
        {
            yield return null;yield return null;
            var accountStore=new LocalAccountStore(Path.Combine(Path.GetDirectoryName(rankingPath),"Accounts"));
            var accountTask=accountStore.CreateAsync("InfiniteTest","Test-Local-73","Test-Local-73");while(!accountTask.IsCompleted) yield return null;
            Assert(accountTask.Result.Success && PlayerSession.EnterAccount(accountTask.Result),"Test account login failed");yield return null;yield return null;
            DataChecks();
            yield return Click("RankingButton"); yield return null; yield return null; Assert(GameObject.Find("LocalRanking")!=null,"Home ranking missing"); yield return Click("RankingBackButton");
            yield return Click("PlayButton"); yield return null; yield return null; Assert(UnityEngine.Object.FindAnyObjectByType<GameManager>()==null && GameObject.Find("ModeSelection")!=null,"Play bypassed mode selection");
            yield return Click("ModeBackButton");
            var catalog=AssetDatabase.LoadAssetAtPath<GladiatorClassCatalog>("Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset");
            var sharedHealth=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Game/ScriptableObjects/Enemies/Gladiator.asset"); float originalHealth=sharedHealth.maximumHealth;
            for(int classIndex=0;classIndex<4;classIndex++)
            {
                ClassSelectionState.Confirm(catalog.classes[classIndex]); yield return Click("PlayButton"); yield return null;yield return null; yield return Click("InfiniteModeButton"); yield return Scene("RomanArena");
                var game=UnityEngine.Object.FindAnyObjectByType<GameManager>(); SetTestRanking(game); var waves=game.Waves; waves.ManualSimulation=true;
                game.Player.GetComponent<ThirdPersonMotor>().AcceptInput=false; Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=false;
                Assert(game.IsInfinite && game.State==MatchState.Preparing && waves.AliveCount==0 && game.Statistics.score==0,"New infinite state not clean");
                Assert(game.Player.Current==catalog.classes[classIndex],"Class lost");
                var equipment=game.Player.GetComponent<WeaponEquipment>(); Assert(equipment.IsFixed && equipment.Current==catalog.classes[classIndex].equipment[0] && equipment.Loadout.Length==1,"Class fixed equipment changed");
                Assert(GameObject.Find("AbilityPanel")==null && GameObject.Find("InfiniteScorePanel")!=null,"Invalid infinite HUD");
                var ability=game.Player.GetComponent<PlayerAbility>(); var stamina=game.Player.GetComponent<CharacterStamina>(); float before=stamina.Current;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Q)); yield return null;yield return null;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState()); yield return null;yield return null;
                Assert(ability.CooldownRemaining>0 && stamina.Current<before,"Q ability failed "+classIndex); ability.TickAbility(100); game.Player.GetComponent<PlayerCombat>().ResetTrainingState(); stamina.Restore();
                double time=game.Statistics.survivalSeconds;game.TogglePause();yield return null;
                Assert(Time.timeScale==0 && game.Statistics.survivalSeconds==time,"Paused survival clock advanced"); game.Resume();
                waves.InfiniteDifficulty.maximumActiveEnemies=2;
                int last=classIndex==0?35:2;
                long memoryBefore=GC.GetTotalMemory(false); int objectsBefore=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
                for(int wave=1;wave<=last;wave++) yield return ClearWave(game,wave);
                yield return null;
                if(classIndex==0)
                {
                    long memoryAfter=GC.GetTotalMemory(true); int objectsAfter=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
                    Assert(objectsAfter<objectsBefore+100,"Scene object growth after 35 waves");
                    Pass("35 successive infinite waves continue beyond five and 31; exact real-damage scores, grouped spawns capped at two, no duplicated deaths, safe NavMesh spawns, closed gate, cleaned corpse lists; live transforms "+objectsBefore+" -> "+objectsAfter+"; managed bytes "+memoryBefore+" -> "+memoryAfter+" (diagnostic, not a multi-hour soak).");
                    ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/InfiniteHUD.png"); yield return null;yield return null;
                }
                var finalBefore=game.Statistics; game.Player.GetComponent<CharacterHealth>().Damage(10000000); yield return null;yield return null;
                Assert(game.State==MatchState.Defeat && waves.State==WaveState.Stopped && waves.PendingCount==0 && game.FinalStatistics.score==finalBefore.score,"Infinite death did not freeze snapshot/spawns");
                Assert(GameObject.Find("InfiniteResults")!=null && GameObject.Find("PlayerNameInput")==null,"Results requested a name instead of account identity");
                if(classIndex==0)
                {
                    Assert(game.ResultSaved && game.SaveInfiniteResult("Duplicate") && new LocalRankingManager(rankingPath).Entries.Count(e=>e.matchId==game.FinalStatistics.matchId)==1,"Result duplicate/save failed");
                    ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/InfiniteResults.png");yield return null;yield return null;
                    yield return Click("ResultsRankingButton");Assert(GameObject.Find("LocalRanking")!=null,"Results ranking missing");
                    ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/InfiniteRanking.png");yield return null;yield return null;yield return Click("RankingBackButton");
                    yield return Click("RetryInfiniteButton");while(game!=null) yield return null;yield return Scene("RomanArena");
                    game=UnityEngine.Object.FindAnyObjectByType<GameManager>();SetTestRanking(game);game.Waves.ManualSimulation=true;
                    Assert(game.IsInfinite && game.Player.Current==catalog.classes[classIndex] && game.Waves.WaveNumber==1 && game.Statistics.score==0 && game.Statistics.enemiesDefeated==0 && game.Statistics.wavesCompleted==0 && !game.ResultSaved,"Retry retained previous run");
                    game.Player.GetComponent<CharacterHealth>().Damage(10000000);yield return null;yield return null;
                }
                yield return Click("ResultsMenuButton"); yield return Scene("MainMenu");
                Pass(catalog.classes[classIndex].displayName+": infinite mode, correct fixed equipment, Q activation, no special-ability panel, pause clock, defeat snapshot, account-bound automatic result registration and results navigation.");
            }
            yield return Click("PlayButton");yield return null;yield return null;yield return Click("StoryModeButton");yield return Scene("RomanArena");
            var storyGame=UnityEngine.Object.FindAnyObjectByType<GameManager>();storyGame.Waves.ManualSimulation=true;
            Assert(!storyGame.IsInfinite && !storyGame.Waves.IsInfinite && storyGame.Waves.WaveCount==5 && storyGame.Statistics.score==0 && storyGame.Ranking==null,"Infinite leaked into story");
            storyGame.Waves.Tick(10);yield return null;Freeze(storyGame.Waves);
            foreach(var actor in storyGame.Waves.Actors)
            { Assert(actor.GetComponent<CharacterHealth>().Maximum==actor.Definition.maximumHealth,"Story health scaled");Assert(actor.GetComponent<ActorAttributes>()==null || actor.GetComponent<ActorAttributes>().DamageMultiplier(false)==1,"Story damage scaled"); }
            Assert(sharedHealth.maximumHealth==originalHealth,"Shared enemy asset changed");
            Pass("Infinite -> menu -> story restores original five-wave schedule, score zero, no ranking, base enemy health/damage and unchanged shared assets. Full five-wave physical gate crossing is covered by MatchStageValidation.");
            storyGame.ReturnToMenu();yield return Scene("MainMenu");
        }
    }
}
