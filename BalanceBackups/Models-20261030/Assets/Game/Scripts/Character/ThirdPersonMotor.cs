using UnityEngine;

namespace Bellator.Character
{
    [RequireComponent(typeof(CharacterController), typeof(CharacterStamina), typeof(ThirdPersonInput))]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        public event System.Action<Vector3, Vector3> Moved;
        [SerializeField] private Transform movementCamera;
        [SerializeField, Min(0)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0)] private float runSpeed = 6.2f;
        [SerializeField, Min(.1f)] private float acceleration = 18;
        [SerializeField, Min(.1f)] private float deceleration = 24;
        [SerializeField, Min(.01f)] private float turnSmoothTime = .12f;
        [SerializeField] private float gravity = -25;
        [SerializeField, Min(0)] private float sprintDrain = 18;
        [SerializeField, Min(0)] private float dodgeCost = 28;
        [SerializeField, Min(.1f)] private float dodgeDuration = .42f;
        [SerializeField, Min(0)] private float dodgeDistance = 3.6f;
        [SerializeField, Min(0)] private float dodgeCooldown = .8f;
        private CharacterController controller;
        private CharacterStamina stamina;
        private ThirdPersonInput input;
        private CharacterHealth health;
        private PlayerLockOn lockOn;
        private PlayerActionStateMachine actionState;
        [SerializeField] private DodgeStyle dodgeStyle;
        [SerializeField, Range(0,1)] private float invulnerableStart = .18f, invulnerableEnd = .62f;
        [SerializeField, Min(0)] private float recoveryDuration = .22f;
        private float dodgeRecoveryRemaining;
        private float activeRecoveryDuration;
        private DodgeConfiguration primaryDodge, secondaryQuickstep;
        public bool HasQuickstep => secondaryQuickstep != null;
        public void ConfigureQuickstep(DodgeConfiguration value) => secondaryQuickstep = value;
        public DodgeStyle DodgeStyle => dodgeStyle;
        public Vector3 DodgeDirection => dodgeDirection;
        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 ActualPlanarVelocity { get; private set; }
        public float WalkSpeed => walkSpeed * SpeedBuff;
        public float RunSpeed => runSpeed * SpeedBuff;
        public int DodgeSequence { get; private set; }
        public float DodgeAnimationProgress => IsDodgeTravelling ? DodgeProgress * .8f :
            dodgeRecoveryRemaining > 0 ? .8f + .2f * (1 - dodgeRecoveryRemaining / Mathf.Max(.001f,activeRecoveryDuration)) : 1;
        public void ConfigureMovement(float speedingUp, float slowingDown, float turning, float drain)
        { acceleration = Mathf.Max(.1f, speedingUp); deceleration = Mathf.Max(.1f, slowingDown); turnSmoothTime = Mathf.Max(.01f, turning); sprintDrain = Mathf.Max(0, drain); }
        public bool IsDodgeTravelling => dodgeRemaining > 0;
        public bool IsInvulnerable => health != null && health.IsAlive && IsDodgeTravelling && DodgeProgress >= invulnerableStart && DodgeProgress < invulnerableEnd;
        public void ConfigureDodge(DodgeConfiguration value)
        {
            primaryDodge = value; secondaryQuickstep = null;
            CancelDodge(); ApplyDodgeProfile(value);
        }
        private void ApplyDodgeProfile(DodgeConfiguration value)
        {
            dodgeStyle = value.style; dodgeDuration = Mathf.Max(.1f, value.duration);
            dodgeCost = Mathf.Max(0, value.cost); dodgeDistance = Mathf.Max(0, value.distance);
            recoveryDuration = Mathf.Max(0, value.recovery); dodgeCooldown = 0;
            invulnerableStart = Mathf.Clamp01(value.invulnerableStart);
            invulnerableEnd = Mathf.Clamp(value.invulnerableEnd, invulnerableStart, 1);
        }
        public void CancelDodge() { dodgeRemaining = dodgeRecoveryRemaining = cooldownRemaining = 0; planarVelocity = ActualPlanarVelocity = Vector3.zero; PlanarSpeed = 0; IsRunning = false; }
        private Vector3 planarVelocity, dodgeDirection;
        private float verticalVelocity, turnVelocity, dodgeRemaining, cooldownRemaining;
        private bool sprintExhausted;
        private float dodgeTravel=3.6f,dodgeRecovery=.8f;
        public float SpeedBuff { get; set; }=1;
        public float DodgeCostMultiplier { get; set; }=1;
        public float DodgeDistanceMultiplier { get; set; }=1;
        public float DodgeCooldownMultiplier { get; set; }=1;
        public void ConfigureSpeeds(float walk,float run) { walkSpeed=walk; runSpeed=run; }
        public bool AcceptInput { get; set; } = true;
        public float MovementSpeedMultiplier { get; set; } = 1;
        public bool SprintAllowed { get; set; } = true;
        public bool DodgeAllowed { get; set; } = true;
        public bool RotationAllowed { get; set; } = true;
        public bool IsDodging => dodgeRemaining > 0 || dodgeRecoveryRemaining > 0;
        public bool IsRunning { get; private set; }
        public float PlanarSpeed { get; private set; }
        public float DodgeProgress => IsDodging ? 1 - dodgeRemaining / dodgeDuration : 0;
        public float CooldownRemaining => cooldownRemaining;
        public void Configure(Transform cameraTransform) => movementCamera = cameraTransform;
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled; controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation); controller.enabled = wasEnabled;
            planarVelocity = Vector3.zero; verticalVelocity = 0; turnVelocity = 0;
            CancelDodge(); PlanarSpeed = 0; sprintExhausted = false;
        }
        private void Awake()
        {
            controller = GetComponent<CharacterController>(); stamina = GetComponent<CharacterStamina>();
            input = GetComponent<ThirdPersonInput>(); health = GetComponent<CharacterHealth>();
            lockOn = GetComponent<PlayerLockOn>();
            if (lockOn == null) lockOn = gameObject.AddComponent<PlayerLockOn>();
            actionState = GetComponent<PlayerActionStateMachine>();
            if (actionState == null) actionState = gameObject.AddComponent<PlayerActionStateMachine>();
            if (primaryDodge == null) primaryDodge = new DodgeConfiguration { style=dodgeStyle, duration=dodgeDuration, recovery=recoveryDuration, cost=dodgeCost, distance=dodgeDistance, invulnerableStart=invulnerableStart, invulnerableEnd=invulnerableEnd };
        }
        private void Start() { if (movementCamera == null && Camera.main != null) movementCamera = Camera.main.transform; }
        private void Update()
        {
            if (!AcceptInput) return;
            ProcessMovement(input.Movement, input.Sprint, input.DodgePressed, Time.deltaTime, input.QuickstepPressed);
        }
        /// <summary>Single movement step, also usable by deterministic validation or future replay.</summary>
        public void ProcessMovement(Vector2 axis, bool sprint, bool dodge, float deltaTime, bool quickstep = false)
        {
            if (!controller.enabled || deltaTime <= 0) return;
            // Sweep in short steps even at low frame rates; never teleport a fast dodge.
            float remaining = Mathf.Min(deltaTime, .25f);
            while (remaining > .00001f)
            {
                float step = Mathf.Min(remaining, .02f);
                StepMovement(axis, sprint, dodge, quickstep, step);
                dodge = quickstep = false;
                remaining -= step;
            }
        }
        private void StepMovement(Vector2 axis, bool sprint, bool dodge, bool quickstep, float dt)
        {
            // Space takes priority if both commands arrive in the same frame.
            var requested = dodge ? primaryDodge : quickstep ? secondaryQuickstep : null;
            dodge = requested != null;
            float requestedCost = requested != null ? Mathf.Max(0, requested.cost) : 0;
            axis = Vector2.ClampMagnitude(axis,1);
            sprint &= SprintAllowed;
            // A press received during an existing dodge/recovery cannot become a second dodge
            // merely because the recovery timer reaches zero inside this movement step.
            dodge &= DodgeAllowed && !IsDodging;
            bool incapacitated = health != null && !health.IsAlive || actionState != null && actionState.IsStaggered || (GetComponent<Bellator.Combat.PlayerCombat>()?.ControlsSuspended ?? false);
            if (incapacitated) { CancelDodge(); axis = Vector2.zero; sprint = dodge = false; }
            cooldownRemaining = Mathf.Max(0, cooldownRemaining - dt);
            dodgeRecoveryRemaining = Mathf.Max(0, dodgeRecoveryRemaining - dt);
            Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 direction = forward * axis.y + Vector3.Cross(Vector3.up,forward) * axis.x;
            if (!sprint) sprintExhausted = false;
            if (sprintExhausted && stamina.Current >= 10) sprintExhausted = false;
            if (dodge && !IsDodging && cooldownRemaining <= 0 && controller.isGrounded && stamina.Current >= requestedCost * DodgeCostMultiplier)
                GetComponent<Bellator.Combat.PlayerCombat>()?.TryCancelForDodge();
            if (dodge && !IsDodging && cooldownRemaining <= 0 && controller.isGrounded &&
                actionState.TryEnter(PlayerActionState.Dodging) && stamina.TryConsume(requestedCost*DodgeCostMultiplier))
            {
                // Snapshot only an accepted action; rejected inputs cannot change active timing or i-frames.
                ApplyDodgeProfile(requested);
                dodgeDirection = direction.sqrMagnitude > .01f ? direction.normalized : transform.forward;
                dodgeTravel=dodgeDistance*DodgeDistanceMultiplier; dodgeRecovery=dodgeCooldown*DodgeCooldownMultiplier;
                activeRecoveryDuration = recoveryDuration * Mathf.Max(0, DodgeCooldownMultiplier);
                dodgeRemaining = dodgeDuration;
                DodgeSequence++;
                planarVelocity = Vector3.zero;
            }
            Vector3 displacement;
            IsRunning = false;
            if (IsDodging)
            {
                float step = Mathf.Min(dt,dodgeRemaining);
                displacement = dodgeDirection * (dodgeTravel / dodgeDuration) * step;
                direction = dodgeDirection;
                dodgeRemaining = Mathf.Max(0,dodgeRemaining - dt);
                if (!IsDodgeTravelling && step > 0) { cooldownRemaining = dodgeRecovery; dodgeRecoveryRemaining = activeRecoveryDuration; }
            }
            else
            {
                if (sprint && !incapacitated && !sprintExhausted && axis.sqrMagnitude > .01f && controller.isGrounded)
                {
                    IsRunning = stamina.TryConsume(sprintDrain * dt);
                    if (!IsRunning) sprintExhausted = true;
                }
                float speed = IsRunning ? runSpeed : walkSpeed;
                Vector3 target = direction * speed * Mathf.Clamp01(MovementSpeedMultiplier) * SpeedBuff;
                planarVelocity = Vector3.MoveTowards(planarVelocity,target,(target.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration)*dt);
                displacement = planarVelocity * dt;
            }
            Vector3 facing = lockOn != null && lockOn.IsLocked ? Vector3.ProjectOnPlane(lockOn.TargetPoint - transform.position, Vector3.up) : direction;
            if (RotationAllowed && facing.sqrMagnitude > .001f)
            {
                float targetYaw = Mathf.Atan2(facing.x,facing.z)*Mathf.Rad2Deg;
                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y,targetYaw,ref turnVelocity,turnSmoothTime,Mathf.Infinity,dt);
                transform.rotation = Quaternion.Euler(0,yaw,0);
            }
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt,-45);
            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move(displacement + Vector3.up * verticalVelocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            Vector3 travelled = transform.position - before; travelled.y = 0;
            PlanarSpeed = travelled.magnitude / dt;
            ActualPlanarVelocity = travelled / dt;
            if ((transform.position - before).sqrMagnitude > .0000001f) Moved?.Invoke(before, transform.position);
            actionState.Refresh();
        }
    }
}




