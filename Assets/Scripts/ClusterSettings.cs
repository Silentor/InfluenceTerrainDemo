using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo
{
    [CreateAssetMenu]
    public class ClusterSettings : ScriptableObject                
    {
        public string Name;
        public Color LandColor_ = Color.magenta;
        public ClusterType Type;
        public BlockType DefaultBlock;

        /*
        [Header("Simplex Fractal noise")]
        public double NoiseFreq = 1 / 10.0;
        public double NoiseAmp = 5;
        public double NoiseLacunarity = 2;
        public double NoiseGain = 0.5;
        [Range(1, 5)]
        public int NoiseOctaves = 2;
        public FastNoise.FractalType NoiseFractal = FastNoise.FractalType.FBM;
        */

        public Color LandColor { get { return LandColor_; } }

        public static readonly ZoneTypeEqualityComparer TypeComparer = new ZoneTypeEqualityComparer();

        public class ZoneTypeEqualityComparer : IEqualityComparer<ClusterSettings>
        {
            public bool Equals(ClusterSettings x, ClusterSettings y)
            {
                if (x == null || y == null) return false;
                return x.Type == y.Type;
            }

            public int GetHashCode(ClusterSettings obj)
            {
                return obj.Type.GetHashCode();
            }
        }
    }
}