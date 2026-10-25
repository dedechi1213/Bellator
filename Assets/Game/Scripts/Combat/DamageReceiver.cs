using System;
using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly float PostureDamage;
        public readonly Vector3 Point,SourcePosition;
        public readonly Transform Attacker;
        public readonly bool Special;
        public DamageInfo(float amount,Vector3 point,Vector3 source,Transform attacker,float postureDamage=-1,bool special=false)
        { Amount=amount; Point=point; SourcePosition=source; Attacker=attacker; PostureDamage=postureDamage < 0 ? amount * .7f : postureDamage; Special=special; }
    }
    public readonly struct DamageResult
    {
        public readonly float Damage;
        public readonly bool Blocked,GuardBroken,Staggered;
        public DamageResult(float damage,bool blocked,bool broken,bool staggered=false) { Damage=damage; Blocked=blocked; GuardBroken=broken; Staggered=staggered; }
    }
    [RequireComponent(typeof(CharacterHealth))]
    public sealed class DamageReceiver : MonoBehaviour
    {
        private CharacterHealth health;
        private ShieldGuard guard;
        private ActorPosture posture;
        public event Action<DamageResult> Impacted;
        public static event Action<DamageReceiver, DamageInfo, DamageResult> Resolved;
        public CharacterHealth Health => health!=null ? health : GetComponent<CharacterHealth>();
        public float LastImpactTime { get; private set; }=float.NegativeInfinity;
        public DamageResult LastImpact { get; private set; }
        public Vector3 LastImpactPoint { get; private set; }
        public Transform LastAttacker { get; private set; }
        public Bellator.Classes.CombatTargetKind TargetKind => Bellator.Classes.CombatBalance.TargetKind(this);
        private void Awake()
        {
            health=GetComponent<CharacterHealth>(); guard=GetComponent<ShieldGuard>();
            posture=GetComponent<ActorPosture>();
            if (posture==null) posture=gameObject.AddComponent<ActorPosture>();
        }
        public bool Receive(DamageInfo hit)
        {
            if(!health.IsAlive || hit.Amount<=0 || (hit.Attacker!=null && (hit.Attacker==transform || transform.IsChildOf(hit.Attacker)))) return false;
            // Dodge invulnerability rejects damage explicitly; physical collisions remain enabled.
            var motor = GetComponent<ThirdPersonMotor>();
            if (motor != null && motor.IsInvulnerable) return false;
            if(!CombatFaction.CanDamage(hit.Attacker,this)) return false;
            bool blocked=false,broken=false;
            var attributes=GetComponent<Bellator.Classes.ActorAttributes>();
            var rules=Bellator.Classes.CombatBalance.Rules(hit.Attacker,this);
            float incoming=hit.Amount*(rules != null ? rules.damage * (hit.Special ? rules.specialDamage : 1) : 1)*(attributes!=null?attributes.IncomingMultiplier:1);
            float amount=guard!=null ? guard.Absorb(incoming,hit.SourcePosition,out blocked,out broken) : incoming;
            float actual=Mathf.Min(health.Current,Mathf.Max(0,amount));
            health.Damage(actual);
            bool staggered = posture.ApplyImpact(hit.PostureDamage * (rules != null ? rules.postureDamage : 1) * (blocked ? .35f : 1), broken, rules != null ? rules.staggerDuration : 1);
            LastImpactTime=Time.time; LastImpact=new DamageResult(actual,blocked,broken,staggered);
            LastImpactPoint = hit.Point; LastAttacker = hit.Attacker;
            Impacted?.Invoke(LastImpact); Resolved?.Invoke(this,hit,LastImpact); return true;
        }
    }
}



