using UnityEngine;

namespace Bellator.Match
{
    public enum GameMode { Story, Infinite }

    /// <summary>Only menu intent survives a scene change; all run state belongs to GameManager.</summary>
    public static class GameModeManager
    {
        public static GameMode Selected { get; private set; } = GameMode.Story;
        public static void Select(GameMode mode) => Selected = mode;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Selected = GameMode.Story;
    }
}
