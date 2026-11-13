using Bellator.Classes;
using UnityEngine;

namespace Bellator.Multiplayer
{
    public sealed class OnlineConfiguration : ScriptableObject
    {
        public GameObject playerPrefab;
        public GameObject cameraPrefab;
        public GladiatorClassCatalog catalog;
        public const string Arena = "Assets/Game/Scenes/OnlineArena.unity";
        public const ushort Protocol = 1113;
    }
}
