using System;

namespace Bellator.Match
{
    [Serializable]
    public sealed class MatchStatistics
    {
        public string matchId, playerName = "Gladiador", characterId, characterName, playedAtUtc;
        public string profileId;
        [NonSerialized] internal string sessionToken;
        public GameMode mode;
        public long score, enemiesDefeated;
        public int highestWave, wavesCompleted;
        public double survivalSeconds;

        public MatchStatistics Copy() => (MatchStatistics)MemberwiseClone();
        public static string FormatTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0, Math.Min(seconds, 315360000)));
            return ((long)time.TotalHours > 0 ? ((long)time.TotalHours).ToString("00") + ":" : "") + time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00");
        }
    }
}
