using System;
using Bellator.Enemies;

namespace Bellator.Match
{
    /// <summary>One instance per run; death deduplication is owned by WaveManager's live subscriptions.</summary>
    public sealed class ScoreManager
    {
        public MatchStatistics Current { get; }
        public bool Finished { get; private set; }
        readonly string profileId,sessionToken,accountName;
        public bool CanRecord => PlayerSession.OwnsRun(profileId,sessionToken);
        public ScoreManager(GameMode mode, string characterId, string characterName)
        {
            profileId=PlayerSession.IsAccount?PlayerSession.Profile.Id:null;
            accountName=PlayerSession.IsAccount?PlayerSession.Username:"Convidado";
            sessionToken=PlayerSession.Token;
            Current = new MatchStatistics { matchId = Guid.NewGuid().ToString("N"), mode = mode,
                characterId = characterId, characterName = characterName, playedAtUtc = DateTime.UtcNow.ToString("O"),
                profileId=profileId,playerName=accountName,sessionToken=sessionToken };
        }
        public static float Multiplier(int wave) => wave <= 5 ? 1 : wave <= 10 ? 1.2f : wave <= 20 ? 1.5f : wave <= 30 ? 2 : 2.5f;
        public static int EnemyPoints(EnemyRole role, int wave) => (int)Math.Round((role == EnemyRole.Heavy ? 200 : role == EnemyRole.Archer ? 150 : 100) * (double)Multiplier(wave), MidpointRounding.AwayFromZero);
        public static long WaveBonus(int wave) => 500L + Math.Max(1, wave) * 100L;
        public void BeginWave(int wave) { if(!Finished) Current.highestWave = Math.Max(Current.highestWave, wave); }
        public void Defeated(EnemyRole role, int wave)
        {
            if(Finished) return;
            Current.enemiesDefeated++;
            Add(EnemyPoints(role, wave));
        }
        public void CompleteWave(int wave)
        {
            if(Finished || wave <= Current.wavesCompleted) return;
            Current.wavesCompleted = wave; Add(WaveBonus(wave));
        }
        void Add(long points) => Current.score = Current.score > long.MaxValue - points ? long.MaxValue : Current.score + points;
        public void Tick(float seconds) { if(!Finished && seconds > 0) Current.survivalSeconds += seconds; }
        public MatchStatistics Finish() { Finished = true; return Current.Copy(); }
        public bool SaveResult(LocalRankingManager ranking)
        {
            if(!Finished || !CanRecord || Current.mode!=GameMode.Infinite) return false;
            var result=Current.Copy(); result.profileId=profileId;result.playerName=accountName;result.sessionToken=sessionToken;
            return ranking.Submit(result);
        }
    }
}
