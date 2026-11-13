using UnityEngine;

namespace Bellator.Classes
{
    public enum AbilityKind { DesperateSteps, DevastatingBlow, CombatStance, PreciseShot }
    [CreateAssetMenu(menuName="Bellator/Classes/Ability")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public AbilityKind kind;
        [Min(0)] public float duration=5,staminaCost=25;
        [Min(.1f)] public float cooldown=14,windup=.5f,recovery=.4f;
        public float speedMultiplier=1.45f,dodgeCostMultiplier=.5f,dodgeDistanceMultiplier=1.35f,dodgeCooldownMultiplier=.5f;
        public float damageMultiplier=1.35f,defenseBonus=.15f,blockCostMultiplier=.6f;
        public float areaRadius=3.4f,areaDamage=48,pushSpeed=7,pushDuration=.35f,shotMultiplier=3;
        [Min(0)] public float areaPostureDamage = 85;
        [Range(10,180)] public float areaArc = 110;
        public Color feedbackColor=new Color(.2f,.7f,1);
    }
}

