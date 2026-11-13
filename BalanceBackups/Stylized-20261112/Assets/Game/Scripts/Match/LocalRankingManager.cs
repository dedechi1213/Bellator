using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

namespace Bellator.Match
{
    /// <summary>Offline top ten, versioned JSON, backup recovery and atomic replacement.</summary>
    public sealed class LocalRankingManager
    {
        [Serializable] sealed class RankingFile { public int version; public List<MatchStatistics> entries; }
        readonly string path;
        List<MatchStatistics> entries = new List<MatchStatistics>();
        public string LastError { get; private set; }
        public string FilePath => path;
        public IReadOnlyList<MatchStatistics> Entries => entries.AsReadOnly();
        public long BestScore => entries.Count > 0 ? entries[0].score : 0;
        public long BestForProfile(string profileId)
        { long best=0; if(string.IsNullOrEmpty(profileId)) return best; foreach(var entry in entries) if(entry.profileId==profileId) best=Math.Max(best,entry.score); return best; }
        public LocalRankingManager(string filePath = null)
        { path = filePath ?? Path.Combine(Application.persistentDataPath, "BellatorInfiniteRanking.json"); Reload(); }
        public void Reload()
        {
            LastError = null; entries = new List<MatchStatistics>();
            if(Read(path, out var loaded)) { entries = loaded; return; }
            if(Read(path + ".bak", out loaded)) entries = loaded;
        }
        bool Read(string file, out List<MatchStatistics> loaded)
        {
            loaded = null;
            try
            {
                if(!File.Exists(file)) return false;
                if(new FileInfo(file).Length > 1024 * 1024) throw new InvalidDataException("Arquivo excede o tamanho permitido.");
                var data = JsonUtility.FromJson<RankingFile>(File.ReadAllText(file));
                if(data == null || data.version != 1 || data.entries == null) throw new InvalidDataException("Formato de ranking inválido.");
                loaded = new List<MatchStatistics>();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach(var item in data.entries)
                {
                    if(!Valid(item) || !ids.Add(item.matchId)) continue;
                    item.playerName = CleanName(item.playerName); loaded.Add(item);
                }
                loaded.Sort(Compare); if(loaded.Count > 10) loaded.RemoveRange(10, loaded.Count - 10);
                return true;
            }
            catch(Exception e) { LastError = "Não foi possível ler o ranking: " + e.Message; return false; }
        }
        static bool Valid(MatchStatistics item) => item != null && item.mode == GameMode.Infinite &&
            Guid.TryParseExact(item.matchId, "N", out _) && item.score >= 0 && item.enemiesDefeated >= 0 &&
            item.highestWave >= 0 && item.wavesCompleted >= 0 && item.wavesCompleted <= item.highestWave &&
            !double.IsNaN(item.survivalSeconds) && !double.IsInfinity(item.survivalSeconds) && item.survivalSeconds >= 0 &&
            !string.IsNullOrWhiteSpace(item.characterName) && item.characterName.Length <= 80 &&
            DateTime.TryParse(item.playedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);
        public static int Compare(MatchStatistics a, MatchStatistics b)
        {
            int result = b.score.CompareTo(a.score); if(result != 0) return result;
            result = b.wavesCompleted.CompareTo(a.wavesCompleted); if(result != 0) return result;
            result = b.survivalSeconds.CompareTo(a.survivalSeconds); if(result != 0) return result;
            result = string.CompareOrdinal(a.playedAtUtc, b.playedAtUtc);
            return result != 0 ? result : string.CompareOrdinal(a.matchId, b.matchId);
        }
        public static string CleanName(string name)
        {
            if(string.IsNullOrWhiteSpace(name)) return "Gladiador";
            var clean = new System.Text.StringBuilder();
            foreach(char c in name.Trim()) if(!char.IsControl(c) && c != '<' && c != '>') { clean.Append(c); if(clean.Length == 24) break; }
            return clean.Length == 0 ? "Gladiador" : clean.ToString();
        }
        public bool Submit(MatchStatistics statistics, string ignoredName=null)
        {
            LastError = null;
            if(statistics==null || !PlayerSession.OwnsRun(statistics.profileId,statistics.sessionToken))
            { LastError="Apenas a conta local vinculada à partida pode registrar este resultado."; return false; }
            if(!Valid(statistics)) { LastError = "Resultado inválido para o Modo Infinito."; return false; }
            foreach(var item in entries) if(item.matchId == statistics.matchId) return true;
            var candidate = new List<MatchStatistics>(entries);
            var saved = statistics.Copy(); saved.playerName = PlayerSession.Username; candidate.Add(saved); candidate.Sort(Compare);
            if(candidate.Count > 10) candidate.RemoveRange(10, candidate.Count - 10);
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(temporary, JsonUtility.ToJson(new RankingFile { version = 1, entries = candidate }, true));
                // Backup contains the new valid payload too, so a previously corrupt primary cannot poison recovery.
                File.Copy(temporary, path + ".bak", true);
                if(File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                entries = candidate; return true;
            }
            catch(Exception e) { LastError = "Não foi possível salvar o ranking: " + e.Message; return false; }
            finally { try { if(File.Exists(temporary)) File.Delete(temporary); } catch { /* Best-effort temp cleanup. */ } }
        }
    }
}
