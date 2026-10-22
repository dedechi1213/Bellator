using System.Collections.Generic;
using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(WeaponEquipment),typeof(CombatInput),typeof(ShieldGuard))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        private WeaponEquipment equipment;
        private CharacterStamina stamina;
        private CharacterHealth health;
        private ThirdPersonMotor motor;
        private CombatInput input;
        private ShieldGuard guard;
        private readonly HashSet<DamageReceiver> hitTargets=new HashSet<DamageReceiver>();
        private AttackConfiguration attack;
        private WeaponDefinition attackWeapon;
        private Vector3 attackForward;
        private float elapsed,windup,active,total,reloadRemaining;
        private float lastComboEnd=float.NegativeInfinity;
        private int nextCombo;
        private bool attacking,heavy,queued,shot,crossbowLoaded=true;
        public bool ManualSimulation { get; set; }
        public bool ControlsSuspended { get; set; }
        public bool ExternalActionBusy { get; set; }
        public bool IsAttacking => attacking;
        public bool IsBusy => attacking || IsReloading || ExternalActionBusy;
        public bool IsReloading => reloadRemaining>0;
        public bool IsHeavy => heavy;
        public bool CrossbowLoaded => crossbowLoaded;
        public int ComboStep { get; private set; }
        public float AttackProgress => attacking ? Mathf.Clamp01(elapsed/total) : 0;
        public float ReloadProgress => IsReloading ? 1-reloadRemaining/equipment.Current.reloadSeconds : 0;
        public int ConfirmedHits { get; private set; }
        public string Status { get; private set; }="";
        public void Configure(Camera camera) => aimCamera=camera;
        private void Awake()
        {
            equipment=GetComponent<WeaponEquipment>(); input=GetComponent<CombatInput>(); guard=GetComponent<ShieldGuard>();
            stamina=GetComponent<CharacterStamina>(); health=GetComponent<CharacterHealth>(); motor=GetComponent<ThirdPersonMotor>();
            equipment.Changed+=EquipmentChanged;
        }
        private void OnDestroy() { if(equipment!=null) equipment.Changed-=EquipmentChanged; }
        private void OnDisable()
        {
            if(motor!=null) { motor.MovementSpeedMultiplier=1; motor.SprintAllowed=motor.DodgeAllowed=motor.RotationAllowed=true; }
            if(guard!=null) guard.Request(false);
            attacking=false; queued=false; reloadRemaining=0;
        }
        private void Update()
        {
            if(ManualSimulation || ControlsSuspended) return;
            guard.Request(input.GuardHeld);
            if(input.LightPressed) TryAttack(false);
            if(input.HeavyPressed) TryAttack(true);
            if(input.ReloadPressed) TryReload();
            TickCombat(Time.deltaTime);
        }
        private void EquipmentChanged()
        { attacking=queued=false; elapsed=reloadRemaining=0; nextCombo=0; lastComboEnd=float.NegativeInfinity; Status=""; }
        public bool TryAttack(bool useHeavy)
        {
            var weapon=equipment.Current;
            if(ControlsSuspended || ExternalActionBusy || weapon==null || !health.IsAlive || motor.IsDodging || guard.IsBlocking || IsReloading) return false;
            if(attacking)
            {
                if(!useHeavy && !heavy && !attackWeapon.IsRanged && elapsed>=windup && elapsed<=total) queued=true;
                return false;
            }
            if(weapon.IsRanged && useHeavy) return false;
            if(weapon.type==EquipmentType.Crossbow && !crossbowLoaded) { TryReload(); return false; }
            if(Time.time-lastComboEnd>.8f) nextCombo=0;
            int index=useHeavy?0:nextCombo%Mathf.Max(1,weapon.lightCombo.Length);
            AttackConfiguration profile=useHeavy?weapon.heavyAttack:(weapon.lightCombo.Length>0?weapon.lightCombo[index]:null);
            if(profile==null) return false;
            if(!stamina.TryConsume(weapon.staminaCost*profile.staminaMultiplier)) { Status="Sem resistência"; return false; }
            attackWeapon=weapon; attack=profile; heavy=useHeavy; attacking=true; elapsed=0; queued=shot=false;
            ComboStep=useHeavy?0:index+1; nextCombo=useHeavy?0:index+1;
            windup=profile.windup/weapon.attackSpeed; active=profile.active/weapon.attackSpeed;
            total=windup+active+profile.recovery/weapon.attackSpeed+weapon.recoveryTime;
            if(weapon.IsRanged && aimCamera!=null)
            {
                Vector3 flat=aimCamera.transform.forward; flat.y=0;
                if(flat.sqrMagnitude>.01f) transform.rotation=Quaternion.LookRotation(flat);
            }
            attackForward=transform.forward; hitTargets.Clear(); Status=useHeavy?"Ataque pesado":profile.name;
            return true;
        }
        public bool TryReload()
        {
            if(ControlsSuspended || IsBusy || equipment.Current==null || equipment.Current.type!=EquipmentType.Crossbow || crossbowLoaded || !health.IsAlive || motor.IsDodging) return false;
            reloadRemaining=equipment.Current.reloadSeconds; Status="Recarregando"; return true;
        }
        public void TickCombat(float dt)
        {
            if(ControlsSuspended) return;
            if(!health.IsAlive) { attacking=queued=false; reloadRemaining=0; guard.Request(false); }
            if(IsReloading)
            {
                reloadRemaining=Mathf.Max(0,reloadRemaining-dt);
                if(!IsReloading) { crossbowLoaded=true; Status="Besta pronta"; }
            }
            if(attacking)
            {
                float previous=elapsed; elapsed+=Mathf.Max(0,dt);
                if(elapsed>=windup && previous<=windup+active)
                {
                    if(attackWeapon.IsRanged) { if(!shot) { shot=true; FireProjectile(); } }
                    else DetectMelee();
                }
                if(elapsed>=total)
                {
                    attacking=false; lastComboEnd=Time.time;
                    bool follow=queued; queued=false;
                    if(attackWeapon.type==EquipmentType.Crossbow && !crossbowLoaded) TryReload();
                    else if(follow) TryAttack(false);
                }
            }
            motor.MovementSpeedMultiplier=guard.IsBlocking ? 0.45f:(IsBusy ? 0.4f:1);
            motor.SprintAllowed=!IsBusy && !guard.IsBlocking;
            motor.DodgeAllowed=!IsBusy && !guard.IsBlocking;
            motor.RotationAllowed=!attacking && !ExternalActionBusy;
        }
        private void DetectMelee() => ConfirmedHits+=CombatQueries.Melee(transform,attackForward,attackWeapon,attack,hitTargets);
        public bool TryFireSpecialShot(float multiplier)
        {
            if(ControlsSuspended || !health.IsAlive || attacking || IsReloading || equipment.Current==null || equipment.Current.type!=EquipmentType.Crossbow || !crossbowLoaded) return false;
            attackWeapon=equipment.Current; attackForward=transform.forward; FireProjectile(multiplier); return true;
        }
        private void FireProjectile(float multiplier=1)
        {
            if(attackWeapon.projectilePrefab==null) { Status="Projétil não configurado"; return; }
            if(attackWeapon.type==EquipmentType.Crossbow) crossbowLoaded=false;
            Vector3 chest=transform.position+Vector3.up*1.4f;
            Vector3 muzzle=equipment.Muzzle!=null?equipment.Muzzle.position:chest+attackForward*.8f;
            Vector3 aim=chest+attackForward*80;
            if(aimCamera!=null)
            {
                var ray=aimCamera.ViewportPointToRay(new Vector3(.5f,.5f,0));
                aim=ray.GetPoint(80);
                if(CombatProjectile.Trace(ray.origin,ray.direction*100,.015f,transform,out var hit)) aim=hit.point;
            }
            CombatQueries.Fire(transform,attackWeapon,muzzle,aim,multiplier);
        }
        public static Vector3 BallisticVelocity(Vector3 from,Vector3 to,float speed,float gravity)
            => CombatQueries.BallisticVelocity(from,to,speed,gravity);
        public void ResetTrainingState()
        { attacking=queued=false; reloadRemaining=0; nextCombo=0; crossbowLoaded=true; Status="Treino restaurado"; guard.ResetGuard(); TickCombat(0); }
    }
}







