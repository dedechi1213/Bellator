using System;
using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    [Serializable]
    public sealed class PostureConfiguration
    {
        [Min(1)] public float maximum = 100;
        [Min(0)] public float recoveryPerSecond = 28, recoveryDelay = 1.5f;
        [Min(.1f)] public float staggerSeconds = .75f;
        [Min(.1f)] public float immunityAfterStagger = 1.25f;
    }
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterHealth))]
    [DefaultExecutionOrder(-40)]
    public sealed class ActorPosture : MonoBehaviour
    {
        [SerializeField] private PostureConfiguration configuration = new PostureConfiguration();
        private CharacterHealth health;
        private float recoveryDelay, staggerRemaining, immunityRemaining;
        private bool wasAlive;
        public float Current { get; private set; }
        public float Maximum => configuration.maximum;
        public float StaggerRemaining => staggerRemaining;
        public bool IsStaggered => staggerRemaining > 0 && health.IsAlive;
        public void Configure(PostureConfiguration value) { configuration = value; ResetPosture(); }
        private void Awake() => health = GetComponent<CharacterHealth>();
        private void OnEnable() { wasAlive = health.IsAlive; health.Changed += HealthChanged; }
        private void OnDisable() { if (health != null) health.Changed -= HealthChanged; }
        private void HealthChanged(float current, float maximum) { if (current <= 0 || !wasAlive) ResetPosture(); wasAlive = current > 0; }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            dt = Mathf.Max(0, dt);
            staggerRemaining = Mathf.Max(0, staggerRemaining - dt);
            immunityRemaining = Mathf.Max(0, immunityRemaining - dt);
            recoveryDelay = Mathf.Max(0, recoveryDelay - dt);
            if (recoveryDelay <= 0 && !IsStaggered) Current = Mathf.Max(0, Current - configuration.recoveryPerSecond * dt);
        }
        public bool ApplyImpact(float amount, bool forceBreak = false, float durationScale = 1)
        {
            if (!health.IsAlive || IsStaggered || immunityRemaining > 0 || amount <= 0 && !forceBreak) return false;
            recoveryDelay = configuration.recoveryDelay;
            Current = Mathf.Min(Maximum, Current + Mathf.Max(0, amount));
            if (!forceBreak && Current < Maximum) return false;
            Current = 0; staggerRemaining = configuration.staggerSeconds * Mathf.Max(0,durationScale);
            immunityRemaining = staggerRemaining + configuration.immunityAfterStagger;
            if(staggerRemaining > 0) GetComponent<PlayerActionStateMachine>()?.Stagger(staggerRemaining);
            return true;
        }
        public void ResetPosture() { Current = 0; recoveryDelay = staggerRemaining = immunityRemaining = 0; }
    }
}
