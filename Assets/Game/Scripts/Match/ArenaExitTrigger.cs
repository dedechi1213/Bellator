using System;
using UnityEngine;

namespace Bellator.Match
{
    /// <summary>Detects an inside-to-outside crossing. Knows geometry/player only, not match state.</summary>
    public sealed class ArenaExitTrigger : MonoBehaviour
    {
        [SerializeField] private Transform player,gateFrame;
        [SerializeField] private float crossingDepth=1.5f,halfWidth=2.9f;
        bool armed;
        public event Action<Transform> Crossed;
        public void Configure(Transform actor,Transform gate) { player=actor; gateFrame=gate; }
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
        private void OnDisable() => armed=false;
    }
}
