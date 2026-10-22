using System;
using UnityEngine;

namespace Bellator.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterStamina : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maximum = 100;
        [SerializeField, Min(0)] private float regenerationPerSecond = 22;
        [SerializeField, Min(0)] private float regenerationDelay = 1.2f;
        private float lastConsumption = float.NegativeInfinity;
        public float Maximum => maximum;
        public float Current { get; private set; }
        public event Action<float, float> Changed;
        private void Awake() => Restore();
        private void Update() => Regenerate(Time.deltaTime);
        public bool TryConsume(float amount)
        {
            if (amount < 0 || Current + .0001f < amount) return false;
            if (amount == 0) return true;
            Current = Mathf.Max(0, Current - amount);
            lastConsumption = Time.time;
            Changed?.Invoke(Current, maximum);
            return true;
        }
        public void Regenerate(float deltaTime)
        {
            if (Time.time - lastConsumption < regenerationDelay || Current >= maximum) return;
            Current = Mathf.Min(maximum, Current + regenerationPerSecond * Mathf.Max(0, deltaTime));
            Changed?.Invoke(Current, maximum);
        }
        public void ConfigureMaximum(float value) { maximum=Mathf.Max(1,value); Restore(); }
        public void Restore()
        {
            Current = maximum;
            lastConsumption = float.NegativeInfinity;
            Changed?.Invoke(Current, maximum);
        }
        private void OnValidate() { maximum = Mathf.Max(1, maximum); regenerationPerSecond = Mathf.Max(0, regenerationPerSecond); }
    }
}


