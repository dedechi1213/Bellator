using UnityEngine;

namespace Bellator.Classes
{
    [DefaultExecutionOrder(30)]
    public sealed class AbilityPoseOverlay : MonoBehaviour
    {
        private PlayerAbility ability;
        private void Awake() => ability=GetComponent<PlayerAbility>();
        private void LateUpdate() { if(ability!=null && ability.enabled) ability.ApplyPose(); }
    }
}

