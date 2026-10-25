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
        public event Action<float> Consumed;
        public float LastDeniedTime { get; private set; } = float.NegativeInfinity;
        [SerializeField, Range(0,1)] private float blockingRegeneration = .35f;
        private void Awake() => Restore();
        private void Update() => Regenerate(Time.deltaTime);
        public bool TryConsume(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0) return false;
            if (Current + .0001f < amount) { LastDeniedTime = Time.time; return false; }
            if (amount == 0) return true;
            Current = Mathf.Max(0, Current - amount);
            Consumed?.Invoke(amount);
            lastConsumption = Time.time;
            Changed?.Invoke(Current, maximum);
            return true;
        }
        public void Regenerate(float deltaTime)
        {
            var motor = GetComponent<ThirdPersonMotor>();
            var combat = GetComponent<Bellator.Combat.PlayerCombat>();
            var health = GetComponent<CharacterHealth>();
            if (health != null && !health.IsAlive) return;
            if (motor != null && (motor.IsDodging || motor.IsRunning) || combat != null && combat.IsBusy) { lastConsumption = Time.time; return; }
            if (Time.time - lastConsumption < regenerationDelay || Current >= maximum) return;
            var guard = GetComponent<Bellator.Combat.ShieldGuard>();
            float scale = guard != null && guard.IsBlocking ? blockingRegeneration : 1;
            Current = Mathf.Min(maximum, Current + regenerationPerSecond * scale * Mathf.Max(0, deltaTime));
            Changed?.Invoke(Current, maximum);
        }
        public void ConfigureMaximum(float value) { maximum=Mathf.Max(1,value); Restore(); }
        public void ConfigureRecovery(float rate, float delay, float blockingScale)
        { regenerationPerSecond=Mathf.Max(0,rate); regenerationDelay=Mathf.Max(0,delay); blockingRegeneration=Mathf.Clamp01(blockingScale); }
        public void Restore()
        {
            Current = maximum;
            lastConsumption = float.NegativeInfinity;
            LastDeniedTime = float.NegativeInfinity;
            Changed?.Invoke(Current, maximum);
        }
        private void OnValidate() { maximum = Mathf.Max(1, maximum); regenerationPerSecond = Mathf.Max(0, regenerationPerSecond); }
    }
}


