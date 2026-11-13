using System.Collections;
using UnityEngine;

namespace Bellator.Arena
{
    /// <summary>Physical lifting gate. The match controller alone decides when it may open.</summary>
    [DisallowMultipleComponent]
    public sealed class MainGateController : MonoBehaviour
    {
        [SerializeField] private Transform movingGate;
        [SerializeField] private float liftHeight = 6.5f;
        [SerializeField, Min(.1f)] private float travelSeconds = 3f;
        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private AudioSource mechanismAudio;
        [SerializeField] private AudioClip openingSound;
        private Coroutine movement;
        private bool targetOpen;
        public bool IsOpen { get; private set; }
        public bool IsMoving => movement != null;
        public int OpenCount { get; private set; }
        public Transform MovingGate => movingGate;
        public void Configure(Transform gate) { movingGate=gate; closedLocalPosition=gate.localPosition; }
        public void ConfigureSound(AudioSource source,AudioClip clip) { mechanismAudio=source; openingSound=clip; }
        private void Awake() { IsOpen=targetOpen=false; if(movingGate!=null) movingGate.localPosition=closedLocalPosition; }
        public void Open() => Move(true);
        public void Close() => Move(false);
        private void Move(bool open)
        {
            if(movingGate==null || movement!=null && targetOpen==open || movement==null && IsOpen==open) return;
            if(movement!=null) StopCoroutine(movement);
            targetOpen=open;
            if(open) { OpenCount++; if(mechanismAudio!=null && openingSound!=null) mechanismAudio.PlayOneShot(openingSound); }
            movement=StartCoroutine(Animate(open));
        }
        private IEnumerator Animate(bool open)
        {
            IsOpen=false;
            Vector3 start=movingGate.localPosition;
            Vector3 target=closedLocalPosition+(open?Vector3.up*liftHeight:Vector3.zero);
            for(float t=0;t<travelSeconds;t+=Time.deltaTime)
            { movingGate.localPosition=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,t/travelSeconds)); yield return null; }
            movingGate.localPosition=target; IsOpen=open; movement=null;
        }
    }
}
