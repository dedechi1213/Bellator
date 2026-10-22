using UnityEngine;

namespace Bellator.Arena
{
    public enum ArenaSpawnKind { Player, Enemy }

    /// <summary>Placement metadata only; does not instantiate characters.</summary>
    public sealed class ArenaSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ArenaSpawnKind kind;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 0.6f;
        public ArenaSpawnKind Kind => kind;
        public float ClearanceRadius => clearanceRadius;
        public void Configure(ArenaSpawnKind value) => kind = value;

        private void OnDrawGizmos()
        {
            Gizmos.color = kind == ArenaSpawnKind.Player ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, clearanceRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }
    }
}
