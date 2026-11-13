using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    public sealed class TrainingArenaControls : MonoBehaviour
    {
        public static event System.Action TrainingReset;
        [SerializeField] private PlayerCombat player;
        private CombatInput input;
        public void Configure(PlayerCombat value) => player=value;
        private void Start() => input=player.GetComponent<CombatInput>();
        private void Update()
        {
            if(input.FrontPressed) TestHit(true);
            if(input.BackPressed) TestHit(false);
            if(input.ResetPressed) ResetTraining();
        }
        public void TestHit(bool frontal)
        {
            var root=player.transform;
            player.GetComponent<DamageReceiver>().Receive(new DamageInfo(24,root.position+Vector3.up,root.position+root.forward*(frontal?2:-2),null));
        }
        public void ResetTraining()
        {
            foreach(var target in FindObjectsByType<DamageReceiver>(FindObjectsSortMode.None)) target.Health.Restore();
            player.GetComponent<CharacterStamina>().Restore();
            player.ResetTrainingState();
            TrainingReset?.Invoke();
        }
    }
}


