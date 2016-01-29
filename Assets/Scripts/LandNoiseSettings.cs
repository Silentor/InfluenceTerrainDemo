using UnityEngine;

namespace TerrainDemo
{
    [CreateAssetMenu]
    public class LandNoiseSettings : ScriptableObject
    {
        [Header("Fine octave")]
        [Range(0, 0.9f)]
        public float InScale2 = 0.2f;

        [Header("Coarse octave")]
        [Range(0, 0.9f)]
        public float InScale1 = 0.02f;

        [Header("Global octave")]
        [Range(0, 0.9f)]
        public float InScale3 = 0.006f;
    }
}
