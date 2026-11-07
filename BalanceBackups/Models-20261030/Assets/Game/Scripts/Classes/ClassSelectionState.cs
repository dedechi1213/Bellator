using UnityEngine;

namespace Bellator.Classes
{
    public static class ClassSelectionState
    {
        public static GladiatorClassDefinition Selected { get; private set; }
        public static void Confirm(GladiatorClassDefinition definition) => Selected=definition;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear() => Selected=null;
    }
}

