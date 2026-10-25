using UnityEngine;
namespace Bellator.AnimationPreview
{
    /// <summary>Editor scene reference only; runtime uses the selected catalog prefab.</summary>
    public sealed class AnimationPreviewEditorReference : MonoBehaviour
    {
        void Awake() { gameObject.SetActive(false); }
    }
}
