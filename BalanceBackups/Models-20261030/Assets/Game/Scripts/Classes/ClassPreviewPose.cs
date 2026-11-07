using UnityEngine;
using Bellator.Combat;

namespace Bellator.Classes
{
    public sealed class ClassPreviewPose : MonoBehaviour
    {
        [SerializeField] private Transform leftArm,rightArm,shield;
        [SerializeField] private EquipmentType type;
        public void Configure(Transform left,Transform right,Transform shieldVisual,EquipmentType equipment)
        { leftArm=left; rightArm=right; shield=shieldVisual; type=equipment; }
        private void LateUpdate()
        {
            if(leftArm==null) return;
            transform.localRotation=Quaternion.Euler(0,180+Mathf.Sin(Time.time*.65f)*4,0);
            rightArm.localRotation=Quaternion.Euler(type==EquipmentType.Crossbow?-85:(type==EquipmentType.HeavySword?-50:-25),0,5);
            leftArm.localRotation=Quaternion.Euler(type==EquipmentType.Crossbow?-75:-8,0,-5);
            if(shield!=null) shield.rotation=transform.rotation;
        }
    }
}

