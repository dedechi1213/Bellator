using UnityEngine;

namespace Bellator.Combat
{
    public enum FactionTeam { Neutral, Player, Gladiators }
    [DisallowMultipleComponent]
    public sealed class CombatFaction : MonoBehaviour
    {
        [SerializeField] private FactionTeam team=FactionTeam.Neutral;
        [SerializeField] private bool allowFriendlyFire;
        public FactionTeam Team => team;
        public bool AllowFriendlyFire { get => allowFriendlyFire; set => allowFriendlyFire=value; }
        public void Configure(FactionTeam value) => team=value;
        public static bool CanDamage(Transform attacker,DamageReceiver victim)
        {
            if(attacker==null || victim==null) return true;
            var source=attacker.GetComponentInParent<CombatFaction>();
            var target=victim.GetComponentInParent<CombatFaction>();
            return source==null || target==null || source.allowFriendlyFire || source.team==FactionTeam.Neutral || target.team==FactionTeam.Neutral || source.team!=target.team;
        }
    }
}

