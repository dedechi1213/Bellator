using System;
using UnityEngine;

namespace Bellator.Combat
{
    public enum EquipmentType { ShortSword, HeavySword, Shield, Bow, Crossbow }
    [Serializable]
    public sealed class AttackConfiguration
    {
        public string name = "Golpe";
        [Min(.01f)] public float damageMultiplier = 1;
        [Min(0)] public float staminaMultiplier = 1;
        [Min(.01f)] public float windup = .16f, active = .14f, recovery = .24f;
        [Min(0)] public float execution = .04f;
        [Min(0)] public float postureMultiplier = 1;
        [Range(0,1)] public float comboWindowStart = .55f, comboWindowEnd = .94f;
        [Range(0,1)] public float dodgeCancelStart = .8f, dodgeCancelEnd = 1;
        [Range(10,180)] public float arc = 100;
        [Min(.1f)] public float rangeMultiplier = 1;
    }
    [CreateAssetMenu(menuName="Bellator/Equipment/Weapon",fileName="Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string displayName;
        public EquipmentType type;
        [Min(0)] public float damage = 22;
        [Min(.1f)] public float reach = 1.8f;
        [Min(.1f)] public float attackSpeed = 1;
        [Min(0)] public float staminaCost = 14, recoveryTime = .08f;
        public AttackConfiguration[] lightCombo = { new AttackConfiguration() };
        public AttackConfiguration heavyAttack = new AttackConfiguration { name="Golpe pesado",damageMultiplier=1.8f,staminaMultiplier=1.8f,windup=.42f,active=.2f,recovery=.42f,arc=85,postureMultiplier=2.5f,dodgeCancelStart=.92f };
        [Min(.01f)] public float comboInputBuffer = .2f;
        [Min(0)] public float finisherRecovery = .2f;
        [Min(0)] public float postureDamage = 20;
        public GameObject visualPrefab, projectilePrefab;
        public Vector3 mountEuler, mountOffset;
        public bool permitsShield, leftHandMount;
        [Min(1)] public float projectileSpeed = 35;
        [Min(0)] public float projectileGravity = 9.81f;
        [Min(.1f)] public float projectileLifetime = 7, reloadSeconds = 1.5f;
        [Range(0,1)] public float shieldReduction = .8f;
        [Range(1,180)] public float shieldArc = 120;
        [Min(0)] public float blockStaminaPerDamage = .65f, blockBaseCost = 6, guardDrain = 3;
        public bool IsRanged => type==EquipmentType.Bow || type==EquipmentType.Crossbow;
        [Min(0)] public float reloadStaminaCost;
        private void OnValidate()
        {
            attackSpeed=Mathf.Max(.1f,attackSpeed); reach=Mathf.Max(.1f,reach);
            damage=Mathf.Max(0,damage); staminaCost=Mathf.Max(0,staminaCost);
        }
    }
}


