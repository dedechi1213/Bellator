using UnityEngine;
using Bellator.Combat;

namespace Bellator.Enemies
{
    public enum EnemyRole { Gladiator, Heavy, Archer }
    [CreateAssetMenu(menuName="Bellator/Enemies/Archetype")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string displayName;
        public EnemyRole role;
        [Min(1)] public float maximumHealth=85;
        [Min(.1f)] public float moveSpeed=3.1f, radius=.4f, height=2.1f;
        [Min(1)] public float detectionRange=30;
        [Min(.1f)] public float engageDistance=1.7f, preferredRange=10, retreatRange=6;
        [Min(0)] public float attackPause=.7f, stunSeconds=.25f;
        [Range(0,1)] public float defenseChance=.55f;
        public WeaponDefinition weapon,shield;
    }
}

