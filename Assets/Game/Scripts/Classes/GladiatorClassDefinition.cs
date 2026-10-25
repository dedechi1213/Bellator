using UnityEngine;
using Bellator.Combat;
using Bellator.Character;

namespace Bellator.Classes
{
    public enum GladiatorClassId { Survivor, Colossus, Warrior, Hunter }
    [CreateAssetMenu(menuName="Bellator/Classes/Gladiator")]
    public sealed class GladiatorClassDefinition : ScriptableObject
    {
        public GladiatorClassId id;
        public string displayName;
        [TextArea] public string description;
        [Min(1)] public float health=120,stamina=100;
        [Min(.1f)] public float walkSpeed=3.5f,runSpeed=6.2f;
        [Min(.1f)] public float meleeDamage=1,rangedDamage=1;
        [Range(0,.8f)] public float defense=.18f;
        public GameObject visualPrefab;
        [Tooltip("Articulated gameplay visual with its Animator. Selection keeps the original display prefab.")]
        public GameObject animatedVisualPrefab;
        public WeaponDefinition[] equipment;
        public WeaponDefinition shield;
        public AbilityDefinition ability;
        public float colliderHeight=2.1f,colliderRadius=.34f;
        public Color color=Color.white;
        [Header("Soulslike movement and posture")]
        public DodgeConfiguration dodge = new DodgeConfiguration();
        public bool hasQuickstep;
        public DodgeConfiguration quickstep = new DodgeConfiguration { style = DodgeStyle.Quickstep, duration = .35f, recovery = .12f, cost = 18, distance = 2.6f };
        public PostureConfiguration posture = new PostureConfiguration();
        [Min(.1f)] public float acceleration = 18, deceleration = 28;
        [Min(.01f)] public float turnSmoothTime = .12f;
        [Min(0)] public float sprintDrain = 18;
        [Min(.1f)] public float postureDamageMultiplier = 1, blockCostMultiplier = 1;
        [Header("Stamina recovery (class authority)")]
        [Min(0)] public float staminaRegeneration = 22, staminaRegenerationDelay = 1.2f;
        [Range(0,1)] public float blockingRegeneration = .35f;
        [Header("Independent combat rules; PvP is reserved, never selected automatically")]
        public CombatRuleConfiguration pve = new CombatRuleConfiguration();
        public CombatRuleConfiguration pvp = new CombatRuleConfiguration { damage = .85f, specialDamage = .85f, postureDamage = .75f, staggerDuration = .6f };
    }
}

