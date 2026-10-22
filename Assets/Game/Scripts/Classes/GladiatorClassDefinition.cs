using UnityEngine;
using Bellator.Combat;

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
        public WeaponDefinition[] equipment;
        public WeaponDefinition shield;
        public AbilityDefinition ability;
        public float colliderHeight=2.1f,colliderRadius=.34f;
        public Color color=Color.white;
    }
}

