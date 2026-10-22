using UnityEngine;

namespace Bellator.Classes
{
    /// <summary>Per-actor modifiers; shared weapon assets remain unchanged.</summary>
    public sealed class ActorAttributes : MonoBehaviour
    {
        public float BaseMelee { get; private set; }=1;
        public float BaseRanged { get; private set; }=1;
        public float BaseDefense { get; private set; }
        public float AbilityDamage { get; set; }=1;
        public float AbilityDefense { get; set; }
        public float BlockCostMultiplier { get; set; }=1;
        public float DamageMultiplier(bool ranged) => (ranged?BaseRanged:BaseMelee)*AbilityDamage;
        public float IncomingMultiplier => 1-Mathf.Clamp(BaseDefense+AbilityDefense,0,.8f);
        public void Configure(GladiatorClassDefinition definition)
        { BaseMelee=definition.meleeDamage; BaseRanged=definition.rangedDamage; BaseDefense=definition.defense; ClearTemporary(); }
        public void Legacy() { BaseMelee=BaseRanged=1; BaseDefense=0; ClearTemporary(); }
        public void ConfigureEnemyDamage(float multiplier)
        { BaseMelee=BaseRanged=Mathf.Max(1,multiplier); BaseDefense=0; ClearTemporary(); }
        public void ClearTemporary() { AbilityDamage=1; AbilityDefense=0; BlockCostMultiplier=1; }
    }
}

