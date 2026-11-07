using System.Collections;
using UnityEngine;

namespace Bellator.Arena
{
    /// <summary>Independent portcullis actuator. A future progression system may call Open/Close.</summary>
    [DisallowMultipleComponent]
    public sealed class MainGateController : MonoBehaviour
    {
        [SerializeField] private Transform movingGate;
        [SerializeField] private float liftHeight = 6.5f;
        [SerializeField, Min(0.1f)] private float travelSeconds = 3f;
        [SerializeField] private Vector3 closedLocalPosition;
        private Coroutine movement;
        public bool IsOpen { get; private set; }

        public void Configure(Transform gate)
        {
            movingGate = gate;
            closedLocalPosition = gate.localPosition;
        }

        private void Awake()
        {
            IsOpen = false;
            if (movingGate != null) movingGate.localPosition = closedLocalPosition;
        }

        public void Open() => Move(true);
        public void Close() => Move(false);

        private void Move(bool open)
        {
            if (movingGate == null) return;
            if (movement != null) StopCoroutine(movement);
            movement = StartCoroutine(Animate(open));
        }

        private IEnumerator Animate(bool open)
        {
            IsOpen = false;
            Vector3 start = movingGate.localPosition;
            Vector3 target = closedLocalPosition + (open ? Vector3.up * liftHeight : Vector3.zero);
            for (float t = 0; t < travelSeconds; t += Time.deltaTime)
            {
                movingGate.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t / travelSeconds));
                yield return null;
            }
            movingGate.localPosition = target;
            IsOpen = open;
            movement = null;
        }
    }
}
