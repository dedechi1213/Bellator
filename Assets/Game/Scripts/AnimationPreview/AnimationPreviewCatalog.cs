using System;
using UnityEngine;

namespace Bellator.AnimationPreview
{
    [CreateAssetMenu(menuName = "Bellator/Animation Preview/Catalog")]
    public sealed class AnimationPreviewCatalog : ScriptableObject
    {
        [Serializable] public sealed class CharacterEntry
        {
            public string label;
            public GameObject prefab;
            public AnimationClip[] clips;
            public float playbackSpeed = 1;
        }
        public CharacterEntry[] characters;
        public string[] stateNames;
        public string[] labels;
    }
}
