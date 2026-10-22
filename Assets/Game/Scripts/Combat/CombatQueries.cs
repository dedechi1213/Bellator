using System.Collections.Generic;
using UnityEngine;

namespace Bellator.Combat
{
    /// <summary>Shared hit rules for player and AI: range, arc, occlusion, deduplication and ballistic launch.</summary>
    public static class CombatQueries
    {
        public static int Melee(Transform owner,Vector3 forward,WeaponDefinition weapon,AttackConfiguration attack,HashSet<DamageReceiver> struck)
        {
            int hits=0; Vector3 origin=owner.position+Vector3.up*1.15f;
            foreach(var collider in Physics.OverlapSphere(origin,weapon.reach*attack.rangeMultiplier,~0,QueryTriggerInteraction.Ignore))
            {
                if(CombatProjectile.Own(collider,owner)) continue;
                var receiver=collider.GetComponentInParent<DamageReceiver>();
                if(receiver==null || struck.Contains(receiver) || !receiver.Health.IsAlive || !CombatFaction.CanDamage(owner,receiver)) continue;
                Vector3 point=collider.ClosestPoint(origin),delta=point-origin;
                Vector3 flat=Vector3.ProjectOnPlane(delta,Vector3.up);
                if(flat.sqrMagnitude>.0001f && Vector3.Dot(forward,flat.normalized)<Mathf.Cos(attack.arc*.5f*Mathf.Deg2Rad)) continue;
                if(CombatProjectile.Trace(origin,delta,.025f,owner,out var obstruction) && obstruction.collider.GetComponentInParent<DamageReceiver>()!=receiver) continue;
                struck.Add(receiver);
                if(receiver.Receive(new DamageInfo(weapon.damage*attack.damageMultiplier*DamageMultiplier(owner,false),point,owner.position,owner))) hits++;
            }
            return hits;
        }
        public static CombatProjectile Fire(Transform owner,WeaponDefinition weapon,Vector3 muzzle,Vector3 aim,float multiplier=1)
        {
            if(weapon.projectilePrefab==null) return null;
            Vector3 chest=owner.position+Vector3.up*1.4f;
            if(CombatProjectile.Trace(chest,muzzle-chest,.055f,owner,out var obstruction))
                muzzle=chest+(muzzle-chest).normalized*Mathf.Max(0,obstruction.distance-.08f);
            Vector3 velocity=BallisticVelocity(muzzle,aim,weapon.projectileSpeed,weapon.projectileGravity);
            if(velocity.sqrMagnitude<.001f) velocity=owner.forward*weapon.projectileSpeed;
            var projectile=Object.Instantiate(weapon.projectilePrefab,muzzle,Quaternion.LookRotation(velocity)).GetComponent<CombatProjectile>();
            projectile.Launch(owner,weapon.damage*DamageMultiplier(owner,true)*multiplier,velocity,weapon.projectileGravity,weapon.projectileLifetime,weapon.reach);
            return projectile;
        }
        private static float DamageMultiplier(Transform owner,bool ranged)
        { var stats=owner.GetComponent<Bellator.Classes.ActorAttributes>(); return stats!=null?stats.DamageMultiplier(ranged):1; }
        public static Vector3 BallisticVelocity(Vector3 from,Vector3 to,float speed,float gravity)
        {
            Vector3 delta=to-from,flat=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(gravity<.001f || flat.magnitude<.01f) return delta.normalized*speed;
            float x=flat.magnitude,v2=speed*speed,discriminant=v2*v2-gravity*(gravity*x*x+2*delta.y*v2);
            if(discriminant<0) return delta.normalized*speed;
            float angle=Mathf.Atan((v2-Mathf.Sqrt(discriminant))/(gravity*x));
            return flat.normalized*(speed*Mathf.Cos(angle))+Vector3.up*(speed*Mathf.Sin(angle));
        }
    }
}


