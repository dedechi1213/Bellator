using System;
using UnityEngine;
using Bellator.Combat;
using Bellator.Classes;

namespace Bellator.Character
{
    public enum PlayerActionState { Idle, Moving, Sprinting, Dodging, Attacking, Blocking, Staggered, Dead }

    /// <summary>One authority for starting incompatible actions, death and stagger interruptions.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-30)]
    public sealed class PlayerActionStateMachine : MonoBehaviour
    {
        private CharacterHealth health;
        private ThirdPersonMotor motor;
        private PlayerCombat combat;
        private ShieldGuard guard;
        private float staggerRemaining;
        public PlayerActionState State { get; private set; }
        public bool IsStaggered => staggerRemaining > 0 && health != null && health.IsAlive;
        public event Action<PlayerActionState> Changed;
        private void Awake() { health = GetComponent<CharacterHealth>(); motor = GetComponent<ThirdPersonMotor>(); }
        private void OnEnable() { if (health != null) health.Changed += HealthChanged; }
        private void OnDisable() { if (health != null) health.Changed -= HealthChanged; }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float dt) { staggerRemaining = Mathf.Max(0, staggerRemaining - Mathf.Max(0, dt)); Refresh(); }
        public void Refresh()
        {
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (guard == null) guard = GetComponent<ShieldGuard>();
            Set(!health.IsAlive ? PlayerActionState.Dead : IsStaggered ? PlayerActionState.Staggered :
                motor != null && motor.IsDodging ? PlayerActionState.Dodging : combat != null && combat.IsBusy ? PlayerActionState.Attacking :
                guard != null && guard.IsBlocking ? PlayerActionState.Blocking : motor != null && motor.IsRunning ? PlayerActionState.Sprinting :
                motor != null && motor.PlanarSpeed > .05f ? PlayerActionState.Moving : PlayerActionState.Idle);
        }
        public bool TryEnter(PlayerActionState action)
        {
            Refresh();
            if (State != PlayerActionState.Idle && State != PlayerActionState.Moving && State != PlayerActionState.Sprinting) return false;
            Set(action); return true;
        }
        private void Set(PlayerActionState next) { if (State == next) return; State = next; Changed?.Invoke(next); }
        public void Stagger(float seconds)
        {
            if (!health.IsAlive || IsStaggered) return;
            staggerRemaining = Mathf.Max(.1f, seconds); Interrupt(); Set(PlayerActionState.Staggered);
        }
        private void HealthChanged(float current, float maximum)
        {
            if (current <= 0) { Interrupt(); Set(PlayerActionState.Dead); }
            else if (State == PlayerActionState.Dead) ResetState();
        }
        private void Interrupt()
        {
            motor?.CancelDodge();
            GetComponent<PlayerCombat>()?.CancelActions();
            GetComponent<PlayerAbility>()?.InterruptCast();
            GetComponent<ShieldGuard>()?.Request(false);
        }
        public void ResetState() { staggerRemaining = 0; Interrupt(); Set(PlayerActionState.Idle); }
    }
}
