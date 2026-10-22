using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    [DefaultExecutionOrder(20)]
    public sealed class CombatPoseAnimator : MonoBehaviour
    {
        [SerializeField] private Transform body,leftArm,rightArm;
        private PlayerCombat combat;
        private WeaponEquipment equipment;
        private ShieldGuard guard;
        private DamageReceiver receiver;
        private ThirdPersonMotor motor;
        public void Configure(Transform torso,Transform left,Transform right) { body=torso; leftArm=left; rightArm=right; }
        private void Awake()
        { combat=GetComponent<PlayerCombat>(); equipment=GetComponent<WeaponEquipment>(); guard=GetComponent<ShieldGuard>(); receiver=GetComponent<DamageReceiver>(); motor=GetComponent<ThirdPersonMotor>(); }
        private void LateUpdate()
        {
            if(body==null || equipment.Current==null) return;
            if(!receiver.Health.IsAlive) { body.localRotation=Quaternion.Euler(0,0,65); return; }
            if(motor.IsDodging) return;
            var type=equipment.Current.type;
            if(type==EquipmentType.Bow)
            {
                leftArm.localRotation=Quaternion.Euler(-80,0,-8);
                rightArm.localRotation=Quaternion.Euler(-70,combat.IsAttacking?Mathf.Lerp(5,65,combat.AttackProgress):5,10);
            }
            else if(type==EquipmentType.Crossbow)
            {
                rightArm.localRotation=Quaternion.Euler(-85,0,0);
                leftArm.localRotation=Quaternion.Euler(combat.IsReloading?-35:-75,30,10);
            }
            else if(combat.IsAttacking)
            {
                float p=combat.AttackProgress;
                float swing=Mathf.SmoothStep(0,1,Mathf.Clamp01((p-.15f)/.5f));
                rightArm.localRotation=Quaternion.Euler(combat.IsHeavy?Mathf.Lerp(-160,-50,swing):-95,Mathf.Lerp(-65,80,swing)*(combat.ComboStep%2==0?-1:1),-12);
                body.localRotation*=Quaternion.Euler(combat.IsHeavy?12:0,Mathf.Lerp(-15,20,swing),0);
                if(type==EquipmentType.HeavySword && !equipment.HasShield) leftArm.localRotation=Quaternion.Euler(-100,25,15);
            }
            else
                rightArm.localRotation=Quaternion.Euler(type==EquipmentType.HeavySword?-50:-25,0,5);
            if(guard.IsBlocking) leftArm.localRotation=Quaternion.Euler(-80,15,8);
            if(equipment.ShieldVisual!=null) equipment.ShieldVisual.rotation=transform.rotation;
            float age=Time.time-receiver.LastImpactTime;
            if(age<.22f) body.localRotation*=Quaternion.Euler(-12*(1-age/.22f),0,receiver.LastImpact.GuardBroken?15:0);
            if(!receiver.Health.IsAlive) body.localRotation=Quaternion.Euler(0,0,65);
        }
    }
}




