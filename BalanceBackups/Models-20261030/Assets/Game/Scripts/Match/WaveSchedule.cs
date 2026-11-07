using System;
using UnityEngine;
using Bellator.Enemies;

namespace Bellator.Match
{
    [Serializable]
    public sealed class WaveGroup
    {
        public EnemyBrain prefab;
        [Min(0)] public int count=1;
    }
    [Serializable]
    public sealed class WaveDefinition
    {
        public string displayName;
        public WaveGroup[] groups;
    }
    [CreateAssetMenu(menuName="Bellator/Match/Wave Schedule",fileName="ArenaWaves")]
    public sealed class WaveSchedule : ScriptableObject
    {
        [Min(0)] public float initialCountdown=5,betweenWaves=8;
        [Min(.05f)] public float spawnInterval=.65f,blockedSpawnRetry=.5f;
        [Min(1)] public float minimumPlayerDistance=5;
        public WaveDefinition[] waves;
    }
}
