using UnityEngine;
using UnityEngine.AI;
using Bellator.Combat;

namespace Bellator.Character
{
    /// <summary>Target selection is requested on input only. Tracking checks just the current target.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThirdPersonInput), typeof(CharacterHealth))]
    public sealed class PlayerLockOn : MonoBehaviour
    {
        [SerializeField, Min(1)] private float acquisitionRange = 18;
        [SerializeField, Min(1)] private float maximumRange = 26;
        [SerializeField, Min(.1f)] private float obstructionGrace = 1;
        [SerializeField] private Camera viewCamera;
        private ThirdPersonInput input;
        private CharacterHealth health;
        private float hiddenTime;
        private float pathCheckRemaining;
        private bool pathAccessible = true;
        private NavMeshPath path;
        public DamageReceiver Target { get; private set; }
        public bool IsLocked => Target != null && Target.Health.IsAlive && Target.gameObject.activeInHierarchy;
        public bool ControlsSuspended { get; set; }
        public Vector3 TargetPoint => IsLocked ? AimPoint(Target) : transform.position + Vector3.up;
        public void Configure(Camera camera) => viewCamera = camera;
        private void Awake() { input = GetComponent<ThirdPersonInput>(); health = GetComponent<CharacterHealth>(); path = new NavMeshPath(); }
        private void Start() { if (viewCamera == null) viewCamera = Camera.main; }
        private void Update()
        {
            if (ControlsSuspended) return;
            TickLock(Time.deltaTime);
            if (input.LockPressed) Toggle();
            else if (input.NextTargetPressed) SwitchTarget(1);
            else if (input.PreviousTargetPressed) SwitchTarget(-1);
        }
        public void TickLock(float dt)
        {
            if (!health.IsAlive) { Clear(); return; }
            if (Target == null) return;
            if (!IsLocked || Vector3.Distance(transform.position, Target.transform.position) > maximumRange) { Clear(); return; }
            pathCheckRemaining -= Mathf.Max(0, dt);
            if (pathCheckRemaining <= 0) { pathAccessible = Reachable(Target); pathCheckRemaining = .3f; }
            hiddenTime = HasSight(Target) && pathAccessible ? 0 : hiddenTime + Mathf.Max(0, dt);
            if (hiddenTime >= obstructionGrace) Clear();
        }
        public bool Toggle()
        {
            if (Target != null) { Clear(); return false; }
            return Select(null, 0);
        }
        public bool SwitchTarget(int direction) => IsLocked ? Select(Target, direction >= 0 ? 1 : -1) : Select(null, 0);
        private bool Select(DamageReceiver previous, int direction)
        {
            if (!health.IsAlive || ControlsSuspended || viewCamera == null) return false;
            DamageReceiver best = null;
            float bestScore = float.PositiveInfinity;
            float previousX = previous != null ? viewCamera.WorldToViewportPoint(AimPoint(previous)).x : .5f;
            foreach (var candidate in FindObjectsByType<DamageReceiver>(FindObjectsSortMode.None))
            {
                var faction = candidate.GetComponent<CombatFaction>();
                if (candidate == previous || candidate.transform == transform || !candidate.Health.IsAlive ||
                    faction == null || faction.Team != FactionTeam.Gladiators || !CombatFaction.CanDamage(transform, candidate)) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > acquisitionRange) continue;
                Vector3 screen = viewCamera.WorldToViewportPoint(AimPoint(candidate));
                if (screen.z <= 0 || screen.x < .03f || screen.x > .97f || screen.y < .03f || screen.y > .97f || !HasSight(candidate) || !Reachable(candidate)) continue;
                float score = new Vector2(screen.x - .5f, screen.y - .5f).magnitude * 2 + distance / acquisitionRange;
                if (direction != 0)
                {
                    float offset = (screen.x - previousX) * direction;
                    score = (offset > .015f ? offset : 2 + Mathf.Abs(offset)) + distance / acquisitionRange * .05f;
                }
                if (score < bestScore) { best = candidate; bestScore = score; }
            }
            if (best == null) return false;
            Target = best; hiddenTime = pathCheckRemaining = 0; pathAccessible = true; return true;
        }
        private bool Reachable(DamageReceiver candidate)
        {
            var agent = candidate.GetComponent<NavMeshAgent>();
            if (agent == null) return true;
            if (!agent.enabled || !agent.isOnNavMesh || !NavMesh.SamplePosition(transform.position, out var start, 2, agent.areaMask)) return false;
            return NavMesh.CalculatePath(start.position, agent.nextPosition, agent.areaMask, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        public bool HasSight(DamageReceiver candidate)
        {
            Vector3 origin = transform.position + Vector3.up * 1.3f;
            if (CombatProjectile.Trace(origin, AimPoint(candidate) - origin, .025f, transform, out var hit) &&
                hit.collider.GetComponentInParent<DamageReceiver>() != candidate) return false;
            if (viewCamera != null && CombatProjectile.Trace(viewCamera.transform.position, AimPoint(candidate) - viewCamera.transform.position, .025f, transform, out hit) &&
                hit.collider.GetComponentInParent<DamageReceiver>() != candidate) return false;
            return true;
        }
        public static Vector3 AimPoint(DamageReceiver target)
        {
            var body = target.GetComponent<CharacterController>();
            return target.transform.position + Vector3.up * (body != null ? body.height * .7f : 1.4f);
        }
        public void Clear() { Target = null; hiddenTime = 0; }
        private void OnDisable() => Clear();
        private void OnGUI()
        {
            if (!IsLocked || ControlsSuspended || viewCamera == null || hiddenTime > 0) return;
            Vector3 point = viewCamera.WorldToScreenPoint(TargetPoint);
            if (point.z <= 0) return;
            Color old = GUI.color; GUI.color = new Color(1, .85f, .4f, .9f);
            float x = point.x, y = Screen.height - point.y;
            GUI.DrawTexture(new Rect(x - 5, y - 1, 10, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - 5, 2, 10), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
