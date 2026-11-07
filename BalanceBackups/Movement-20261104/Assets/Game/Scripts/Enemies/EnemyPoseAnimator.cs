using UnityEngine;
using Bellator.Character;
using Bellator.Combat;

namespace Bellator.Enemies
{
    [DefaultExecutionOrder(20)]
    public sealed class EnemyPoseAnimator : MonoBehaviour
    {
        [SerializeField] private Transform body,leftArm,rightArm,leftLeg,rightLeg;
        private EnemyBrain brain;
        private EnemyNavigation navigation;
        private ShieldGuard guard;
        private WeaponEquipment equipment;
        private float phase;
        public void Configure(Transform torso,Transform left,Transform right,Transform legL,Transform legR)
        { body=torso; leftArm=left; rightArm=right; leftLeg=legL; rightLeg=legR; }
        private void Awake() { brain=GetComponent<EnemyBrain>(); navigation=GetComponent<EnemyNavigation>(); guard=GetComponent<ShieldGuard>(); equipment=GetComponent<WeaponEquipment>(); }
        private void LateUpdate()
        {
            if(body==null) return;
            if(brain.State==EnemyState.Dead)
            {
                body.localRotation=Quaternion.Slerp(body.localRotation,Quaternion.Euler(85,0,10),1-Mathf.Exp(-7*Time.deltaTime));
                body.localPosition=Vector3.Lerp(body.localPosition,new Vector3(0,.16f,0),1-Mathf.Exp(-7*Time.deltaTime)); return;
            }
            phase+=Time.deltaTime*navigation.Speed*3;
            float swing=Mathf.Sin(phase)*Mathf.Clamp01(navigation.Speed/3)*32;
            body.localPosition=new Vector3(0,Mathf.Abs(Mathf.Sin(phase))*.025f,0);
            body.localRotation=Quaternion.Euler(brain.State==EnemyState.Stunned?-15:0,0,0);
            leftLeg.localRotation=Quaternion.Euler(-swing,0,0); rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            leftArm.localRotation=Quaternion.Euler(swing,0,-5); rightArm.localRotation=Quaternion.Euler(-25,0,5);
            if(brain.Definition.role==EnemyRole.Archer)
            {
                leftArm.localRotation=Quaternion.Euler(-80,0,-8);
                rightArm.localRotation=Quaternion.Euler(-70,brain.State==EnemyState.Attack?Mathf.Lerp(0,65,brain.AttackProgress):5,10);
            }
            else if(brain.State==EnemyState.Attack)
            {
                float p=Mathf.SmoothStep(0,1,brain.AttackProgress);
                bool heavy=brain.Definition.role==EnemyRole.Heavy;
                rightArm.localRotation=Quaternion.Euler(heavy?Mathf.Lerp(-155,-65,p):-95,Mathf.Lerp(-65,70,p),-10);
                if(heavy) leftArm.localRotation=Quaternion.Euler(-100,25,10);
            }
            else if(brain.Definition.role==EnemyRole.Heavy) rightArm.localRotation=Quaternion.Euler(-50,0,5);
            if(guard.IsBlocking) leftArm.localRotation=Quaternion.Euler(-80,15,8);
            if(equipment.ShieldVisual!=null) equipment.ShieldVisual.rotation=transform.rotation;
        }
    }
}

