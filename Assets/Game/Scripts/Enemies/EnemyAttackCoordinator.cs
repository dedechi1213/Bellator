using System.Collections.Generic;
using UnityEngine;
using Bellator.Combat;

namespace Bellator.Enemies
{
    /// <summary>Leases cover windup, hit and recovery. Each victim has an independent attack budget.</summary>
    public static class EnemyAttackCoordinator
    {
        private static readonly HashSet<EnemyBrain> holders = new HashSet<EnemyBrain>();
        private static readonly Dictionary<DamageReceiver, float> nextStart = new Dictionary<DamageReceiver, float>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { holders.Clear(); nextStart.Clear(); }
        public static bool TryAcquire(EnemyBrain actor, DamageReceiver target, int maximum, float spacing)
        {
            holders.RemoveWhere(e => e == null || !e.isActiveAndEnabled || e.State != EnemyState.Attack && e.State != EnemyState.Recover);
            int count = 0;
            foreach (var holder in holders) if (holder.Target == target) count++;
            if (count >= Mathf.Max(1, maximum) || nextStart.TryGetValue(target, out float next) && Time.time < next) return false;
            holders.Add(actor); nextStart[target] = Time.time + Mathf.Max(.1f, spacing); return true;
        }
        public static void Release(EnemyBrain actor) => holders.Remove(actor);
    }
}
