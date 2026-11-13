using System;
using UnityEngine;
using Bellator.Character;

namespace Bellator.Match
{
    /// <summary>Detects an inside-to-outside crossing. Knows geometry/player only, not match state.</summary>
    public sealed class ArenaExitTrigger : MonoBehaviour
    {
        [SerializeField] private Transform player,gateFrame;
        [SerializeField] private float crossingDepth=1.5f,halfWidth=2.9f;
        bool armed;
        ThirdPersonMotor motor;
        public event Action<Transform> Crossed;
        public void Configure(Transform actor,Transform gate) { Unbind(); player=actor; gateFrame=gate; if (isActiveAndEnabled) Bind(); }
        private void OnEnable() => Bind();
        private void Bind()
        {
            Unbind(); motor=player!=null?player.GetComponent<ThirdPersonMotor>():null;
            if(motor!=null) motor.Moved+=ObserveMovement;
        }
        private void Unbind() { if(motor!=null) motor.Moved-=ObserveMovement; motor=null; }
        private void ObserveMovement(Vector3 from,Vector3 to)
        {
            if(player==null || gateFrame==null) return;
            Vector3 before=gateFrame.InverseTransformPoint(from),after=gateFrame.InverseTransformPoint(to);
            if(before.z>crossingDepth || after.z<=crossingDepth || !IsBeyondThreshold(player)) return;
            float fraction=(crossingDepth-before.z)/(after.z-before.z);
            Vector3 crossing=Vector3.Lerp(before,after,fraction);
            // Swept CharacterController travel observes the same exit plane even between physics ticks.
            // Teleport does not emit Moved, so repositioning cannot report a crossing.
            if(Mathf.Abs(crossing.x)<halfWidth && crossing.y>-.3f && crossing.y<3)
            { armed=false; Crossed?.Invoke(player); }
        }
        public bool IsBeyondThreshold(Transform actor)
        {
            if(actor!=player || gateFrame==null) return false;
            Vector3 p=gateFrame.InverseTransformPoint(actor.position);
            return p.z>crossingDepth && Mathf.Abs(p.x)<halfWidth && p.y>-.3f && p.y<3;
        }
        private void OnTriggerEnter(Collider other) => Observe(other);
        private void OnTriggerStay(Collider other) => Observe(other);
        private void Observe(Collider other)
        {
            if(player==null || !other.transform.IsChildOf(player)) return;
            if(gateFrame.InverseTransformPoint(player.position).z<=crossingDepth) armed=true;
            if(armed && IsBeyondThreshold(player)) { armed=false; Crossed?.Invoke(player); }
        }
        private void OnTriggerExit(Collider other) { if(player!=null && other.transform.IsChildOf(player)) armed=false; }
        private void OnDisable() { armed=false; Unbind(); }
    }
}
