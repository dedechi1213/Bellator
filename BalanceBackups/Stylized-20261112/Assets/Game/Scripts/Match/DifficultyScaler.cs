using System;
using UnityEngine;
using Bellator.Enemies;
using Bellator.Classes;

namespace Bellator.Match
{
    /// <summary>Serialized on WaveManager. Never edits shared enemy or weapon assets.</summary>
    [Serializable]
    public sealed class DifficultyScaler
    {
        [Min(0)] public float initialCountdown = 5, betweenWaves = 5;
        [Min(.05f)] public float spawnInterval = .65f;
        [Range(1, 15)] public int maximumActiveEnemies = 12;
        [Min(1)] public int initialEnemies = 2, maximumEnemiesPerWave = 90;
        [Min(0)] public float enemiesPerWave = 1;
        [Min(0)] public float healthPerWave = .08f, damagePerWave = .035f;
        [Min(1)] public float maximumHealthMultiplier = 1000, maximumDamageMultiplier = 25;
        [Range(0, .4f)] public float heavyFrequency = .30f, archerFrequency = .25f;
        [Min(0)] public float aggressionPerWave = .015f;
        [Range(1, 1.8f)] public float maximumAttackRate = 1.6f;
        [Min(.15f)] public float minimumAttackPause = .18f;

        // Linear early progression, logarithmic late growth. Arithmetic remains safe at int.MaxValue.
        public float Progress(int wave)
        {
            double n = Math.Max(0d, (double)wave - 1);
            return (float)(Math.Min(n, 29) + (n > 29 ? 10 * Math.Log(1 + (n - 29) / 10) : 0));
        }
        public int EnemyCount(int wave) => (int)Math.Max(1, Math.Min(Math.Max(1, maximumEnemiesPerWave),
            Math.Max(1, initialEnemies) + Math.Floor(Progress(wave) * Math.Max(0, enemiesPerWave))));
        public float HealthMultiplier(int wave) => Mathf.Clamp(1 + Progress(wave) * Mathf.Max(0, healthPerWave), 1, Mathf.Max(1, maximumHealthMultiplier));
        public float DamageMultiplier(int wave) => Mathf.Clamp(1 + Progress(wave) * Mathf.Max(0, damagePerWave), 1, Mathf.Max(1, maximumDamageMultiplier));
        public float AttackRate(int wave) => Mathf.Clamp(1 + Progress(wave) * Mathf.Max(0, aggressionPerWave), 1, Mathf.Clamp(maximumAttackRate, 1, 1.8f));
        public EnemyRole RoleFor(int wave, int slot, int total)
        {
            // Spread a deterministic composition through successive groups, with common enemies first.
            float introduction = Mathf.Clamp01((wave - 2) / 8f);
            int heavies = wave >= 3 ? Mathf.FloorToInt(total * Mathf.Clamp(heavyFrequency, 0, .4f) * introduction) : 0;
            int archers = wave >= 4 ? Mathf.FloorToInt(total * Mathf.Clamp(archerFrequency, 0, .4f) * introduction) : 0;
            if(wave >= 3 && heavyFrequency > 0) heavies = Mathf.Max(1, heavies);
            if(wave >= 4 && archerFrequency > 0) archers = Mathf.Max(1, archers);
            int position = total - 1 - slot;
            if(position < archers) return EnemyRole.Archer;
            if(position < archers + heavies) return EnemyRole.Heavy;
            return EnemyRole.Gladiator;
        }
        public void Apply(EnemyBrain actor, int wave)
        {
            actor.GetComponent<Bellator.Character.CharacterHealth>().ConfigureMaximum(actor.Definition.maximumHealth * HealthMultiplier(wave));
            var attributes = actor.GetComponent<ActorAttributes>();
            if(attributes == null) attributes = actor.gameObject.AddComponent<ActorAttributes>();
            attributes.ConfigureEnemyDamage(DamageMultiplier(wave));
            actor.ConfigureAggression(AttackRate(wave), Mathf.Max(.15f, minimumAttackPause));
        }
    }
}
