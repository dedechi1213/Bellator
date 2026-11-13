using System;
using UnityEngine;

namespace Bellator.Character
{
    public enum DodgeStyle { Roll, Quickstep }
    [Serializable]
    public sealed class DodgeConfiguration
    {
        public DodgeStyle style;
        [Min(.1f)] public float duration = .65f;
        [Min(0)] public float recovery = .22f, cost = 28, distance = 3.6f;
        [Range(0, 1)] public float invulnerableStart = .18f, invulnerableEnd = .62f;
    }
}
