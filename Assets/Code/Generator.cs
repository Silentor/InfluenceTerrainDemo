using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Code
{
    [Obsolete]
    public static class Generator
    {
        public static float IDWCoeff = 2;

        public static WorldSettings World;
        public static ZoneSettings[] Zones;

        private static float NearestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }
    }
}
