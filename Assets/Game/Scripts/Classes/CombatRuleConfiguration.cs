using System;
using UnityEngine;
using Bellator.Combat;

namespace Bellator.Classes
{
    [Serializable]
    public sealed class CombatRuleConfiguration
    {
        [Min(0)] public float damage = 1, specialDamage = 1, postureDamage = 1;
        [Min(0)] public float staggerDuration = 1, healing = 1, staminaRecovery = 1;
        [Min(0)] public float abilityEffect = 1, abilityDuration = 1, pushStrength = 1;
    }

    public enum CombatTargetKind { Training, AI, Player }

    public static class CombatBalance
    {
        public static CombatTargetKind TargetKind(DamageReceiver target) =>
            target.GetComponent<PlayerClassController>() != null ? CombatTargetKind.Player :
            target.GetComponent<Bellator.Enemies.EnemyBrain>() != null ? CombatTargetKind.AI : CombatTargetKind.Training;

        // This chooses numbers only. Faction permissions still reject player-on-player hits.
        public static CombatRuleConfiguration Rules(Transform attacker, DamageReceiver target)
        {
            var source = attacker != null ? attacker.GetComponent<PlayerClassController>() : null;
            return source == null || source.Current == null ? null :
                TargetKind(target) == CombatTargetKind.Player ? source.Current.pvp : source.Current.pve;
        }
    }
}
