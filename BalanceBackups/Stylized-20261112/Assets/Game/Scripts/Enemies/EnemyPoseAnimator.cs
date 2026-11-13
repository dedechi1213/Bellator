using UnityEngine;
using Bellator.Character;
using Bellator.Combat;

namespace Bellator.Enemies
{
    [DefaultExecutionOrder(20)]
    public sealed class EnemyPoseAnimator : MonoBehaviour
    {
        [SerializeField] private Transform body,leftArm,rightArm,leftLeg,rightLeg;
        [SerializeField] private Animator animator;
        private EnemyBrain brain;
        private EnemyNavigation navigation;
        private ShieldGuard guard;
        private WeaponEquipment equipment;
        private float phase;
        private int visualState;
        private float poseTime, sampledProgress;
        private bool playingAction;
        private float walkLength, runLength, backLength, strafeLength, hitLength, deathLength, blockLength;
        private static readonly int Speed = Animator.StringToHash("Speed"), MoveX = Animator.StringToHash("MoveX"),
            MoveY = Animator.StringToHash("MoveY"), Cadence = Animator.StringToHash("LocomotionRate"),
            Pursuit = StateHash("FreeRunning"), Retreat = StateHash("LockedLocomotion"),
            Fire = StateHash("Fire"), Light = StateHash("LightAttack"), Heavy = StateHash("HeavyAttack"),
            Block = StateHash("Block"), Hit = StateHash("HitReaction"), Death = StateHash("Death");
        private static int StateHash(string name) => Animator.StringToHash("Base Layer." + name);
        public void Configure(Transform torso,Transform left,Transform right,Transform legL,Transform legR)
        { body=torso; leftArm=left; rightArm=right; leftLeg=legL; rightLeg=legR; }
        private void Awake()
        {
            brain=GetComponent<EnemyBrain>(); navigation=GetComponent<EnemyNavigation>(); guard=GetComponent<ShieldGuard>(); equipment=GetComponent<WeaponEquipment>();
            if (animator == null) return;
            animator.applyRootMotion = false;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                switch (clip.name)
                {
                    case "Walk": walkLength=clip.length; break;
                    case "Run": runLength=clip.length; break;
                    case "WalkBackward": backLength=clip.length; break;
                    case "StrafeLeft": strafeLength=clip.length; break;
                    case "HitReaction": hitLength=clip.length; break;
                    case "Death": deathLength=clip.length; break;
                    case "Block": blockLength=clip.length; break;
                }
        }
        internal void SampleRelease()
        {
            if (animator != null) Sample(Fire,.34f);
        }
        private void Sample(int state, float normalized)
        {
            normalized=Mathf.Clamp(normalized,0,.99999f);
            if (playingAction && state == visualState && normalized == sampledProgress) return;
            playingAction=true;
            animator.speed=0;
            animator.Play(state,0,normalized);
            animator.Update(0);
            visualState=state; sampledProgress=normalized;
        }
        private void Animate(float dt)
        {
            if (dt <= 0) return;
            bool shooting=brain.Definition.role==EnemyRole.Archer;
            bool heavy=brain.Definition.role==EnemyRole.Heavy;
            if (brain.State == EnemyState.Attack || brain.IsFinishingAttack)
            {
                Sample(shooting ? Fire : heavy ? Heavy : Light,
                    brain.AttackPoseProgress(shooting ? .34f : heavy ? .4f : .22f, shooting ? .5f : heavy ? .58f : .44f));
                return;
            }
            int action = brain.State == EnemyState.Dead ? Death : brain.State == EnemyState.Stunned ? Hit : guard.IsBlocking ? Block : 0;
            if (action != 0)
            {
                poseTime = visualState == action ? poseTime+dt : 0;
                float length = action == Death ? deathLength : action == Hit ? hitLength : blockLength;
                Sample(action,poseTime/Mathf.Max(.001f,length));
                return;
            }
            float speed = navigation.Speed;
            bool retreat = brain.State == EnemyState.Retreat;
            float normalized = Mathf.Clamp01(speed/Mathf.Max(.1f,navigation.TopSpeed));
            Vector3 local = transform.InverseTransformDirection(navigation.PlanarVelocity);
            float walkToRun = Mathf.InverseLerp(.5f,1,normalized);
            float forwardCadence = Mathf.Lerp(walkLength/PlayerAnimationBridge.WalkStride,runLength/PlayerAnimationBridge.RunStride,walkToRun);
            float cadence = retreat ? PlayerAnimationBridge.DirectionalCadence(local,walkLength/PlayerAnimationBridge.WalkStride,
                strafeLength/PlayerAnimationBridge.SideStride,backLength/PlayerAnimationBridge.BackStride) : forwardCadence;
            float scaledSpeed = speed/Mathf.Max(.5f,animator.transform.lossyScale.z);
            animator.SetFloat(Speed,normalized,.08f,dt);
            animator.SetFloat(MoveX,normalized*local.normalized.x,.08f,dt);
            animator.SetFloat(MoveY,normalized*local.normalized.z,.08f,dt);
            animator.SetFloat(Cadence,Mathf.Lerp(1,Mathf.Clamp(scaledSpeed*cadence,.45f,3.8f),Mathf.Clamp01(speed/.6f)),.06f,dt);
            int next = retreat ? Retreat : Pursuit;
            animator.speed=1;
            if (visualState != next)
            {
                if (visualState == 0) animator.Play(next);
                else animator.CrossFadeInFixedTime(next,playingAction ? .11f : .16f,0);
                visualState=next;
            }
            playingAction=false;
        }
        private void LateUpdate()
        {
            if (animator != null) { Animate(Time.deltaTime); return; }
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

