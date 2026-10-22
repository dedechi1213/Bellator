using UnityEngine;
using Bellator.Combat;

namespace Bellator.Enemies
{
    /// <summary>Three persistent initial actors. No spawning loop, wave or progression logic.</summary>
    public sealed class EnemyEncounter : MonoBehaviour
    {
        private void OnEnable() => TrainingArenaControls.TrainingReset+=ResetEnemies;
        private void OnDisable() => TrainingArenaControls.TrainingReset-=ResetEnemies;
        public void ResetEnemies()
        {
            foreach(var enemy in GetComponentsInChildren<EnemyBrain>(true)) enemy.ResetActor();
        }
    }
}

