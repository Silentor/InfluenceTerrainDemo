using System;
using UnityEngine;

namespace TerrainDemo
{
    [Obsolete]
    public static class Generator
    {
        public static float IDWCoeff = 2;

        public static LandNoiseSettings LandNoise;
        public static ZoneSettings[] Zones;

        private static float NearestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }
    }
}
