using System;
using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point,SourcePosition;
        public readonly Transform Attacker;
        public DamageInfo(float amount,Vector3 point,Vector3 source,Transform attacker)
        { Amount=amount; Point=point; SourcePosition=source; Attacker=attacker; }
    }
    public readonly struct DamageResult
    {
        public readonly float Damage;
        public readonly bool Blocked,GuardBroken;
        public DamageResult(float damage,bool blocked,bool broken) { Damage=damage; Blocked=blocked; GuardBroken=broken; }
    }
    [RequireComponent(typeof(CharacterHealth))]
    public sealed class DamageReceiver : MonoBehaviour
    {
        private CharacterHealth health;
        private ShieldGuard guard;
        public event Action<DamageResult> Impacted;
        public CharacterHealth Health => health!=null ? health : GetComponent<CharacterHealth>();
        public float LastImpactTime { get; private set; }=float.NegativeInfinity;
        public DamageResult LastImpact { get; private set; }
        private void Awake() { health=GetComponent<CharacterHealth>(); guard=GetComponent<ShieldGuard>(); }
        public bool Receive(DamageInfo hit)
        {
            if(!health.IsAlive || hit.Amount<=0 || (hit.Attacker!=null && (hit.Attacker==transform || transform.IsChildOf(hit.Attacker)))) return false;
            if(!CombatFaction.CanDamage(hit.Attacker,this)) return false;
            bool blocked=false,broken=false;
            var attributes=GetComponent<Bellator.Classes.ActorAttributes>();
            float incoming=hit.Amount*(attributes!=null?attributes.IncomingMultiplier:1);
            float amount=guard!=null ? guard.Absorb(incoming,hit.SourcePosition,out blocked,out broken) : incoming;
            float actual=Mathf.Min(health.Current,Mathf.Max(0,amount));
            health.Damage(actual);
            LastImpactTime=Time.time; LastImpact=new DamageResult(actual,blocked,broken);
            Impacted?.Invoke(LastImpact); return true;
        }
    }
}



