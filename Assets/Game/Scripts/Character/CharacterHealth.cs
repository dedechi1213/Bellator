using System;
using UnityEngine;

namespace Bellator.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maximum = 100;
        public float Maximum => maximum;
        public float Current { get; private set; }
        public bool IsAlive => Current > 0;
        public event Action<float, float> Changed;
        private void Awake() => Restore();
        public void ConfigureMaximum(float value) { maximum=Mathf.Max(1,value); Restore(); }
        public void Restore() { Current = maximum; Changed?.Invoke(Current, Maximum); }
        public void Damage(float amount) { Current = Mathf.Clamp(Current - Mathf.Max(0, amount), 0, maximum); Changed?.Invoke(Current, Maximum); }
        public void Heal(float amount) { Current = Mathf.Clamp(Current + Mathf.Max(0, amount), 0, maximum); Changed?.Invoke(Current, Maximum); }
        private void OnValidate() => maximum = Mathf.Max(1, maximum);
    }
}


