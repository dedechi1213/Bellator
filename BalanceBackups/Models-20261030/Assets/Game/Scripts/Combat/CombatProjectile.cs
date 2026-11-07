using UnityEngine;

namespace Bellator.Combat
{
    /// <summary>Ballistic motion with swept physics queries; no tunnelling at high projectile speeds.</summary>
    public sealed class CombatProjectile : MonoBehaviour
    {
        [SerializeField,Min(.01f)] private float radius=.055f;
        private Transform owner;
        private Bellator.Character.CharacterHealth ownerHealth;
        private Vector3 velocity,source;
        private float damage,gravity,remaining,remainingRange;
        private bool launched,spent;
        public bool HasHit => spent;
        private bool special;
        public void Launch(Transform attacker,float amount,Vector3 initialVelocity,float drop,float lifetime,float range=float.PositiveInfinity,bool specialShot=false)
        {
            special=specialShot;
            ownerHealth=attacker!=null?attacker.GetComponent<Bellator.Character.CharacterHealth>():null;
            owner=attacker; source=attacker!=null?attacker.position:transform.position;
            damage=amount; velocity=initialVelocity; gravity=drop; remaining=lifetime; remainingRange=range; launched=true;
            foreach(var c in Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Ignore))
                if(!Own(c,owner)) { Impact(c,transform.position); break; }
        }
        private void Update() { if(launched) Simulate(Time.deltaTime); }
        public void Simulate(float dt)
        {
            if(spent || !launched || dt<=0) return;
            if(ownerHealth!=null && !ownerHealth.IsAlive) { spent=true; Destroy(gameObject); return; }
            float step=Mathf.Min(dt,remaining);
            Vector3 displacement=velocity*step+Vector3.down*(.5f*gravity*step*step);
            if(displacement.magnitude>remainingRange) displacement=displacement.normalized*remainingRange;
            if(Trace(transform.position,displacement,radius,owner,out var hit)) { transform.position=hit.point; Impact(hit.collider,hit.point); return; }
            transform.position+=displacement; velocity+=Vector3.down*gravity*step;
            if(velocity.sqrMagnitude>.01f) transform.rotation=Quaternion.LookRotation(velocity);
            remainingRange-=displacement.magnitude;
            remaining-=dt; if(remaining<=0 || remainingRange<=.0001f) { spent=true; Destroy(gameObject); }
        }
        public void CancelForOwner(Transform actor) { if(owner==actor) { spent=true; Destroy(gameObject); } }
        private void Impact(Collider collider,Vector3 point)
        {
            if(spent) return;
            spent=true; var receiver=collider.GetComponentInParent<DamageReceiver>();
            if(receiver!=null) receiver.Receive(new DamageInfo(damage,point,source,owner,-1,special));
            Destroy(gameObject);
        }
        public static bool Own(Collider collider,Transform owner) => owner!=null && collider.transform.IsChildOf(owner);
        public static bool Trace(Vector3 origin,Vector3 delta,float radius,Transform owner,out RaycastHit closest)
        {
            closest=default; float distance=delta.magnitude; if(distance<.00001f) return false;
            bool found=false; float minimum=float.PositiveInfinity;
            foreach(var hit in Physics.SphereCastAll(origin,radius,delta/distance,distance,~0,QueryTriggerInteraction.Ignore))
                if(!Own(hit.collider,owner) && hit.distance<minimum) { minimum=hit.distance; closest=hit; found=true; }
            return found;
        }
    }
}



