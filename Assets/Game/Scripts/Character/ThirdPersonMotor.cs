using UnityEngine;

namespace Bellator.Character
{
    [RequireComponent(typeof(CharacterController), typeof(CharacterStamina), typeof(ThirdPersonInput))]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
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
        public bool IsDodging => dodgeRemaining > 0;
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
            dodgeRemaining = cooldownRemaining = 0; PlanarSpeed = 0; IsRunning = false; sprintExhausted = false;
        }
        private void Awake()
        {
            controller = GetComponent<CharacterController>(); stamina = GetComponent<CharacterStamina>();
            input = GetComponent<ThirdPersonInput>(); health = GetComponent<CharacterHealth>();
        }
        private void Update()
        {
            if (!AcceptInput) return;
            ProcessMovement(input.Movement, input.Sprint, input.DodgePressed, Time.deltaTime);
        }
        /// <summary>Single movement step, also usable by deterministic validation or future replay.</summary>
        public void ProcessMovement(Vector2 axis, bool sprint, bool dodge, float deltaTime)
        {
            if (!controller.enabled || deltaTime <= 0) return;
            float dt = Mathf.Min(deltaTime, .05f);
            axis = Vector2.ClampMagnitude(axis,1);
            sprint &= SprintAllowed; dodge &= DodgeAllowed;
            if (health != null && !health.IsAlive) { axis = Vector2.zero; sprint = dodge = false; }
            cooldownRemaining = Mathf.Max(0, cooldownRemaining - dt);
            Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 direction = forward * axis.y + Vector3.Cross(Vector3.up,forward) * axis.x;
            if (!sprint) sprintExhausted = false;
            if (sprintExhausted && stamina.Current >= 10) sprintExhausted = false;
            if (dodge && !IsDodging && cooldownRemaining <= 0 && controller.isGrounded && stamina.TryConsume(dodgeCost*DodgeCostMultiplier))
            {
                dodgeDirection = direction.sqrMagnitude > .01f ? direction.normalized : transform.forward;
                dodgeTravel=dodgeDistance*DodgeDistanceMultiplier; dodgeRecovery=dodgeCooldown*DodgeCooldownMultiplier;
                dodgeRemaining = dodgeDuration;
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
                if (!IsDodging) cooldownRemaining = dodgeRecovery;
            }
            else
            {
                if (sprint && !sprintExhausted && axis.sqrMagnitude > .01f && controller.isGrounded)
                {
                    IsRunning = stamina.TryConsume(sprintDrain * dt);
                    if (!IsRunning) sprintExhausted = true;
                }
                float speed = IsRunning ? runSpeed : walkSpeed;
                Vector3 target = direction * speed * Mathf.Clamp01(MovementSpeedMultiplier) * SpeedBuff;
                planarVelocity = Vector3.MoveTowards(planarVelocity,target,(target.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration)*dt);
                displacement = planarVelocity * dt;
            }
            if (RotationAllowed && direction.sqrMagnitude > .001f)
            {
                float targetYaw = Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
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
        }
    }
}




