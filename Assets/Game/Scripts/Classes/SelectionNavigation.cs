using UnityEngine;

namespace Bellator.Classes
{
    /// <summary>Explicit intent: browsing never starts a match.</summary>
    public static class SelectionNavigation
    {
        public static bool StartAfterConfirmation { get; private set; }
        public static bool ReturnToModes { get; private set; }
        public static void Browse() { StartAfterConfirmation = false; ReturnToModes = false; }
        public static void Play() { StartAfterConfirmation = true; ReturnToModes = false; }
        public static void Back() { ReturnToModes = StartAfterConfirmation; StartAfterConfirmation = false; }
        public static bool ConsumeReturnToModes() { bool value = ReturnToModes; ReturnToModes = false; return value; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Browse();
    }
}
