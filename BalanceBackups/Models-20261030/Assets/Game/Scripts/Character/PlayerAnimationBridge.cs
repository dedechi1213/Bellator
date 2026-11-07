using System.Collections.Generic;
using Bellator.Classes;
using Bellator.Combat;
using UnityEngine;

namespace Bellator.Character
{
    /// <summary>Real clips follow gameplay timelines. Only visual descendants are animated.</summary>
    [DefaultExecutionOrder(35), DisallowMultipleComponent]
    public sealed class PlayerAnimationBridge : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField, Min(.1f)] float walkCycleDistance=1.6f, runCycleDistance=2.3f;
        readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
        readonly HashSet<int> parameters=new HashSet<int>();
        ThirdPersonMotor motor; PlayerCombat combat; PlayerActionStateMachine states; PlayerLockOn lockOn;
        CharacterHealth health; ShieldGuard guard; PlayerAbility ability; WeaponEquipment equipment; DamageReceiver receiver;
        Transform motion,torso; Quaternion baseRotation;
        string action; float hitElapsed=100,deathElapsed; bool dead; int actionSequence=-1;
        public bool DrivesAnimator => animator!=null && animator.runtimeAnimatorController!=null;
        public Animator Animator => animator;
        public string VisualState => action ?? Locomotion(lockOn!=null && lockOn.IsLocked);
        void Awake()
        {
            motor=GetComponent<ThirdPersonMotor>(); combat=GetComponent<PlayerCombat>(); states=GetComponent<PlayerActionStateMachine>();
            lockOn=GetComponent<PlayerLockOn>(); health=GetComponent<CharacterHealth>(); guard=GetComponent<ShieldGuard>();
            ability=GetComponent<PlayerAbility>(); equipment=GetComponent<WeaponEquipment>(); receiver=GetComponent<DamageReceiver>();
        }
        void OnEnable() { if(receiver!=null) receiver.Impacted+=Impacted; }
        void OnDisable() { if(receiver!=null) receiver.Impacted-=Impacted; }
        void Impacted(DamageResult hit)
        { if(!hit.Blocked && hit.Damage>0 && (hit.Staggered || !combat.IsBusy && !motor.IsDodging)) hitElapsed=0; }
        public void Bind(Animator value)
        {
            animator=value; clips.Clear(); parameters.Clear(); action=null; actionSequence=-1; hitElapsed=100; deathElapsed=0; dead=false;
            if(!DrivesAnimator) return;
            animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var clip in animator.runtimeAnimatorController.animationClips) if(clip!=null) clips[clip.name]=clip;
            foreach(var parameter in animator.parameters) parameters.Add(parameter.nameHash);
            baseRotation=animator.transform.localRotation;
            motion=animator.transform.Find("MotionPivot"); torso=animator.transform.Find("MotionPivot/BodyPivot/TorsoPivot");
            if(Application.isPlaying) { animator.Rebind(); animator.Update(0); }
        }
        void Float(string name,float value,float damping=0)
        {
            int hash=Animator.StringToHash(name); if(!parameters.Contains(hash)) return;
            if(damping>0) animator.SetFloat(hash,value,damping,Time.deltaTime); else animator.SetFloat(hash,value);
        }
        void Bool(string name,bool value) { int hash=Animator.StringToHash(name); if(parameters.Contains(hash)) animator.SetBool(hash,value); }
        float Length(string name) => clips.TryGetValue(name,out var clip)?clip.length:.5f;
        void LateUpdate() => TickVisual(Time.deltaTime);
        string Locomotion(bool locked) => locked ? motor.IsRunning?"LockedRunning":"LockedLocomotion" : motor.IsRunning?"FreeRunning":"FreeLocomotion";
        public void TickVisual(float dt)
        {
            if(!DrivesAnimator || motor==null || health==null || dt<=0) return;
            states.Refresh(); bool locked=lockOn!=null && lockOn.IsLocked;
            Vector3 local=transform.InverseTransformDirection(motor.ActualPlanarVelocity); float speed=motor.PlanarSpeed;
            Float("Speed",speed<.04f?0:Mathf.Clamp01(speed/Mathf.Max(.1f,motor.IsRunning?motor.RunSpeed:motor.WalkSpeed)),.07f);
            Vector3 direction=speed>.04f?local.normalized:Vector3.zero;
            Float("MoveX",direction.x*Mathf.Clamp01(speed/Mathf.Max(.1f,motor.WalkSpeed)),.07f);
            Float("MoveY",direction.z*Mathf.Clamp01(speed/Mathf.Max(.1f,motor.WalkSpeed)),.07f);
            Bool("IsLockedOn",locked); Bool("IsRunning",motor.IsRunning); Bool("IsBlocking",guard.IsBlocking);
            float stride=(motor.IsRunning?runCycleDistance:walkCycleDistance)*Mathf.Max(.5f,animator.transform.localScale.z);
            Float("LocomotionRate",speed<.05f?1:Mathf.Clamp(Length(motor.IsRunning?"Run":"Walk")*speed/stride,.55f,2.5f));
            string next=null; float progress=0; int sequence=0; hitElapsed+=dt;
            if(!health.IsAlive)
            {
                if(!dead) { dead=true; deathElapsed=0; }
                deathElapsed+=dt; next="Death"; progress=deathElapsed/Length(next);
            }
            else
            {
                if(dead) { dead=false; deathElapsed=0; action=null; animator.Rebind(); }
                if(states.IsStaggered || hitElapsed<Length("HitReaction")) { next="HitReaction"; progress=states.IsStaggered?Mathf.Min(.22f,hitElapsed/Length(next)):hitElapsed/Length(next); }
                else if(motor.IsDodging) { next=motor.DodgeStyle==DodgeStyle.Roll?"DodgeRoll":"Quickstep"; progress=motor.DodgeAnimationProgress; sequence=motor.DodgeSequence; }
                else if(combat.IsAttacking) { next=equipment.Current.IsRanged?"Fire":combat.IsHeavy?"HeavyAttack":"LightAttack"; progress=combat.AnimationAttackProgress; sequence=combat.AttackSequence; }
                else if(combat.IsReloading) { next="Reload"; progress=combat.ReloadProgress; }
                else if(ability!=null && ability.IsCasting)
                {
                    bool shot=ability.Definition.kind==AbilityKind.PreciseShot; next=shot?"Fire":"HeavyAttack";
                    float contact=shot ? .34f : .4f;
                    float point=ability.Definition.windup/Mathf.Max(.001f,ability.Definition.windup+ability.Definition.recovery);
                    progress=ability.CastProgress<point?Mathf.Lerp(0,contact,ability.CastProgress/Mathf.Max(.001f,point)):
                        Mathf.Lerp(contact,1,(ability.CastProgress-point)/Mathf.Max(.001f,1-point));
                }
                else if(guard.IsBlocking) { next="Block"; progress=action==next?animator.GetCurrentAnimatorStateInfo(0).normalizedTime+dt/Length(next):0; }
            }
            animator.transform.localRotation=baseRotation;
            if(next!=null)
            {
                if(next!=action || sequence!=actionSequence)
                {
                    ResetTriggers();
                    string trigger=next=="DodgeRoll"?"Dodge":next=="HitReaction"?"Hit":next=="Death"?"Die":next;
                    if(parameters.Contains(Animator.StringToHash(trigger)) && next!="Block") animator.SetTrigger(trigger);
                }
                // Gameplay owns cancellation and recovery; no competing action exit timer.
                animator.speed=0; animator.Play(next,0,Mathf.Clamp(progress,0,.99999f)); animator.Update(0);
                if(next=="DodgeRoll")
                {
                    Vector3 dodge=transform.InverseTransformDirection(motor.DodgeDirection);
                    float angle=Mathf.Atan2(dodge.x,dodge.z)*Mathf.Rad2Deg;
                    float weight=progress<.8f?Mathf.SmoothStep(0,1,progress/.15f):1-Mathf.SmoothStep(0,1,(progress-.8f)/.2f);
                    animator.transform.localRotation=baseRotation*Quaternion.Euler(0,angle*weight,0);
                }
                else if(next=="Quickstep" && motion!=null && torso!=null)
                {
                    Vector3 dodge=transform.InverseTransformDirection(motor.DodgeDirection);
                    Quaternion heading=Quaternion.FromToRotation(new Vector3(-.14f,0,-.1f).normalized,new Vector3(dodge.x,0,dodge.z).normalized);
                    Vector3 p=motion.localPosition, offset=heading*new Vector3(p.x,0,p.z); motion.localPosition=new Vector3(offset.x,p.y,offset.z);
                    Vector3 e=torso.localEulerAngles; float x=Mathf.DeltaAngle(0,e.x),z=Mathf.DeltaAngle(0,e.z);
                    Vector3 lean=heading*new Vector3(-z,0,x); torso.localRotation=Quaternion.Euler(lean.z,e.y,-lean.x);
                }
            }
            else
            {
                animator.speed=1;
                if(action!=null) { ResetTriggers(); animator.CrossFadeInFixedTime(Locomotion(locked),.07f); }
            }
            action=next; actionSequence=sequence;
        }
        void ResetTriggers() { foreach(var parameter in animator.parameters) if(parameter.type==AnimatorControllerParameterType.Trigger) animator.ResetTrigger(parameter.nameHash); }
    }
}
