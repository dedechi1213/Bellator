using UnityEngine;

namespace Bellator.Character
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private ThirdPersonInput input;
        [SerializeField] private Vector3 targetOffset = new Vector3(0,1.55f,0);
        [SerializeField, Range(.01f,1)] private float sensitivity = .12f;
        [SerializeField] private float minimumPitch = -25, maximumPitch = 65;
        [SerializeField, Min(.5f)] private float distance = 5;
        [SerializeField, Min(.3f)] private float minimumDistance = 1.5f, maximumDistance = 8;
        [SerializeField, Min(0)] private float zoomSpeed = .012f;
        [SerializeField, Min(.01f)] private float followSmoothTime = .07f;
        [SerializeField, Min(.1f)] private float collisionRadius = .3f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private float yaw, pitch = 18, currentDistance;
        private Vector3 pivot, followVelocity;
        private bool initialized;
        public bool AcceptInput { get; set; } = true;
        public float CurrentDistance => currentDistance;
        public float Pitch => pitch;
        public Vector3 Pivot => pivot;
        public void Configure(Transform followTarget, ThirdPersonInput playerInput)
        { target = followTarget; input = playerInput; initialized = false; }
        public void SetTargetHeight(float height) { targetOffset=new Vector3(0,height,0); initialized=false; }
        private void OnEnable() => initialized = false;
        private void LateUpdate()
        {
            if (AcceptInput) UpdateOrbit(input != null ? input.Look : Vector2.zero,input != null ? input.Zoom : 0,Time.deltaTime);
        }
        public void UpdateOrbit(Vector2 look, float scroll, float deltaTime)
        {
            if (target == null) return;
            float dt = Mathf.Clamp(deltaTime,0,.05f);
            Vector3 anchor = target.position + targetOffset;
            if (!initialized) { yaw = target.eulerAngles.y; pivot = anchor; currentDistance = distance; initialized = true; }
            // Mouse delta is already integrated per input update: do not multiply by deltaTime.
            float configuredSensitivity = sensitivity * Bellator.Match.GameSettings.Sensitivity;
            yaw = Mathf.Repeat(yaw + look.x*configuredSensitivity,360);
            pitch = Mathf.Clamp(pitch-look.y*configuredSensitivity,minimumPitch,maximumPitch);
            distance = Mathf.Clamp(distance-scroll*zoomSpeed,minimumDistance,maximumDistance);
            Vector3 smoothed = Vector3.SmoothDamp(pivot,anchor,ref followVelocity,followSmoothTime,Mathf.Infinity,dt);
            Vector3 lag = smoothed-anchor;
            pivot = anchor + (lag.sqrMagnitude > .0001f ? lag.normalized*ClearDistance(anchor,lag.normalized,lag.magnitude) : Vector3.zero);
            Quaternion orbit = Quaternion.Euler(pitch,yaw,0);
            Vector3 back = orbit*Vector3.back;
            float safe = ClearDistance(pivot,back,distance);
            // Retract immediately, recover gradually; never interpolate through a blocking wall.
            currentDistance = safe < currentDistance ? safe : Mathf.Lerp(currentDistance,safe,1-Mathf.Exp(-7*dt));
            currentDistance = Mathf.Min(currentDistance,safe);
            transform.SetPositionAndRotation(pivot+back*currentDistance,orbit);
        }
        public bool IsExternal(Collider collider) => !collider.transform.IsChildOf(target);
        private float ClearDistance(Vector3 origin,Vector3 direction,float desired)
        {
            float result = desired;
            // Radius also encloses the near clipping plane at the widest configured camera angle.
            var cameraComponent = GetComponent<Camera>();
            float halfHeight = cameraComponent.nearClipPlane*Mathf.Tan(cameraComponent.fieldOfView*.5f*Mathf.Deg2Rad);
            float radius = Mathf.Max(collisionRadius,halfHeight*Mathf.Sqrt(1+cameraComponent.aspect*cameraComponent.aspect)+.02f);
            foreach (var c in Physics.OverlapSphere(origin,radius,obstructionMask,QueryTriggerInteraction.Ignore))
                if (IsExternal(c)) return 0;
            foreach (var hit in Physics.SphereCastAll(origin,radius,direction,desired,obstructionMask,QueryTriggerInteraction.Ignore))
                if (IsExternal(hit.collider)) result = Mathf.Min(result,Mathf.Max(0,hit.distance-.06f));
            return result;
        }
    }
}


