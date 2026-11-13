using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    [RequireComponent(typeof(WeaponEquipment),typeof(CharacterStamina),typeof(CharacterHealth))]
    public sealed class ShieldGuard : MonoBehaviour
    {
        private WeaponEquipment equipment;
        private CharacterStamina stamina;
        private CharacterHealth health;
        private ThirdPersonMotor motor;
        private PlayerCombat combat;
        private bool requested;
        private float brokenRemaining;
        private float CostMultiplier => GetComponent<Bellator.Classes.ActorAttributes>() is Bellator.Classes.ActorAttributes stats ? stats.BlockCostMultiplier * stats.BaseBlockCostMultiplier : 1;
        public bool IsBroken => brokenRemaining>0;
        public bool IsBlocking => requested && !IsBroken && equipment.HasShield && health.IsAlive && (motor==null || !motor.IsDodging) && (combat==null || !combat.IsBusy) && !(GetComponent<PlayerActionStateMachine>()?.IsStaggered ?? false) && !(GetComponent<ActorPosture>()?.IsStaggered ?? false);
        public void Request(bool value) => requested=value;
        private void Awake()
        { equipment=GetComponent<WeaponEquipment>(); stamina=GetComponent<CharacterStamina>(); health=GetComponent<CharacterHealth>(); motor=GetComponent<ThirdPersonMotor>(); combat=GetComponent<PlayerCombat>(); }
        private void Update()
        {
            brokenRemaining=Mathf.Max(0,brokenRemaining-Time.deltaTime);
            // Holding guard slows regeneration; only an actual blocked impact spends stamina.
        }
        public float Absorb(float damage,Vector3 source,out bool blocked,out bool broken)
        {
            blocked=broken=false;
            if(!IsBlocking) return damage;
            Vector3 direction=source-transform.position; direction.y=0;
            if(direction.sqrMagnitude<.0001f || Vector3.Dot(transform.forward,direction.normalized)<Mathf.Cos(equipment.Shield.shieldArc*.5f*Mathf.Deg2Rad)) return damage;
            var shield=equipment.Shield;
            if(!stamina.TryConsume((damage*shield.blockStaminaPerDamage+shield.blockBaseCost)*CostMultiplier))
            { Break(); broken=true; return damage; }
            blocked=true; return damage*(1-shield.shieldReduction);
        }
        private void Break() { stamina.TryConsume(stamina.Current); brokenRemaining=1.2f; requested=false; }
        public void ResetGuard() { brokenRemaining=0; requested=false; }
    }
}


