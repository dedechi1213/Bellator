#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Opt-in development-player smoke test; inert in normal games and excluded from release builds.</summary>
    public static class RankingPersistenceProbe
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Run()
        {
            if(Application.isEditor) return;
            var args=Environment.GetCommandLineArgs();
            // A one-shot sidecar lets the presentation probe run in a normally launched, visible development player.
            string presentationMarker=Path.GetFullPath(Path.Combine(Application.dataPath,"..","BellatorAccessProbe.txt"));
            if(args.Length==1 && File.Exists(presentationMarker))
            {
                string output=File.ReadAllText(presentationMarker).Trim();File.Delete(presentationMarker);
                if(!string.IsNullOrEmpty(output)) args=new[]{args[0],"-bellator-access-probe",output};
            }
            int presentation=Array.IndexOf(args,"-bellator-access-probe");
            if(presentation>=0 && presentation+1<args.Length)
            {
                string output=args[presentation+1];
                try
                {
                    Application.runInBackground=true;
                    Screen.SetResolution(1280,720,FullScreenMode.Windowed);
                    await Task.Delay(1000);
                    for(int frame=0;frame<12;frame++) await Task.Yield();
                    Canvas.ForceUpdateCanvases();
                    var notice=GameObject.Find("GuestNotice")?.GetComponent<Text>();
                    var guest=GameObject.Find("GuestButton");
                    if(notice==null || guest==null || PlayerSession.HasSession || GameObject.Find("PlayButton")!=null ||
                        notice.fontSize>=guest.GetComponentInChildren<Text>().fontSize || notice.transform.parent!=guest.transform.parent ||
                        notice.GetComponent<RectTransform>().anchorMax.y>=guest.GetComponent<RectTransform>().anchorMin.y)
                        throw new InvalidOperationException("Access presentation/guest notice failed");
                    ScreenCapture.CaptureScreenshot(output+".png");
                    await Task.Delay(1000);
                    for(int frame=0;frame<12;frame++) await Task.Yield();
                    File.WriteAllText(output+".result.txt","PASS: standalone access screen; "+Screen.width+"x"+Screen.height+"; guest note separate and below button; font "+notice.fontSize+" vs button "+guest.GetComponentInChildren<Text>().fontSize+"; no session/menu bypass.");
                    Application.Quit(0);
                }
                catch(Exception error) { File.WriteAllText(output+".result.txt","FAIL: "+error);Application.Quit(1); }
                return;
            }
            int phaseIndex=Array.IndexOf(args,"-bellator-ranking-probe"), pathIndex=Array.IndexOf(args,"-bellator-ranking-file");
            if(phaseIndex<0 || phaseIndex+1>=args.Length || pathIndex<0 || pathIndex+1>=args.Length) return;
            string phase=args[phaseIndex+1], path=args[pathIndex+1];
            try
            {
                var store=new LocalAccountStore(Path.Combine(Path.GetDirectoryName(path),"Accounts"));
                var access=phase=="write"?await store.CreateAsync("PersistenceTest","Process-local-72","Process-local-72"):await store.LoginAsync("PersistenceTest","Process-local-72");
                if(!access.Success || !PlayerSession.EnterAccount(access)) throw new InvalidOperationException("Local account did not persist or authenticate");
                var ranking=new LocalRankingManager(path);
                if(phase=="write")
                {
                    var result=new ScoreManager(GameMode.Infinite,"Warrior","Guerreiro").Finish();
                    result.matchId="7242d708401e47eda20432c782d3cdd7";result.score=123456;result.highestWave=9;result.wavesCompleted=8;result.enemiesDefeated=64;result.survivalSeconds=321.5;
                    if(!ranking.Submit(result,result.playerName)) throw new IOException(ranking.LastError);
                }
                else if(phase!="read") throw new ArgumentException("Unknown probe phase");
                if(ranking.Entries.Count!=1 || ranking.BestScore!=123456 || ranking.Entries[0].survivalSeconds!=321.5 || ranking.Entries[0].wavesCompleted!=8 || ranking.Entries[0].playerName!="PersistenceTest" || ranking.Entries[0].profileId!=PlayerSession.Profile.Id)
                    throw new InvalidDataException("Ranking did not survive the player process round-trip");
                File.WriteAllText(path+"."+phase+".result.txt","PASS: standalone player "+phase+"; Unity "+Application.unityVersion+"; persisted local credentials, stable profile ID and score/name/waves/time verified.");
                Application.Quit(0);
            }
            catch(Exception error)
            {
                try { File.WriteAllText(path+"."+phase+".result.txt","FAIL: "+error); } catch { }
                Debug.LogException(error); Application.Quit(1);
            }
        }
    }
}
#endif
