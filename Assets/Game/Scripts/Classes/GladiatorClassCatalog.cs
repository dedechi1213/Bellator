using UnityEngine;

namespace Bellator.Classes
{
    [CreateAssetMenu(menuName="Bellator/Classes/Catalog")]
    public sealed class GladiatorClassCatalog : ScriptableObject
    {
        public GladiatorClassDefinition[] classes;
        public int defaultClass=2;
        public GladiatorClassDefinition Default => classes[Mathf.Clamp(defaultClass,0,classes.Length-1)];
    }
}

