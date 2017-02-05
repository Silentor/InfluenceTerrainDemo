using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Layout
{
    public class Land
    {
        public Land(LandLayout layout)
        {
            _zones = layout.Zones.Select(z => new Zone(z)).ToArray();
        }


        public ZoneRatio GetBilinearInterpolationInfluence(Vector2 position, Vector2 min, Vector2 max, ZoneRatio q11, ZoneRatio q12, ZoneRatio q21, ZoneRatio q22)
        {
            _bilinearTime.Start();

            var x = position.x;
            var y = position.y;
            var x1 = min.x;
            var x2 = max.x;
            var y1 = min.y;
            var y2 = max.y;

            var result = (1 / (x2 - x1) * (y2 - y1)) *
                         (q11 * (x2 - x) * (y2 - y) + q21 * (x - x1) * (y2 - y) + q12 * (x2 - x) * (y - y1) + q22 * (x - x1) * (y - y1));
            result.Normalize();

            _bilinearTime.Stop();
            _bilinearCounter++;

            return result;
        }

        /*
        public IZoneNoiseSettings GetZoneNoiseSettings(ZoneRatio influence)
        {
            return ZoneSettings.Lerp(_zoneSettings, influence);
        }
        */

        public string GetStaticstics()
        {
            if (_bilinearCounter == 0)
                _bilinearCounter = 1;

            if (_influenceCounter == 0)
                _influenceCounter = 1;

            return string.Format("Influence {0} ticks per operation, interpolation {1} ticks per operation, zones count {2}",
                _influenceTime.ElapsedTicks / _influenceCounter, _bilinearTime.ElapsedTicks / _bilinearCounter, _zones.Length);
        }

        //public static int ZonesCount;
        //public static int WorldSize;
        //public static int ChunkSize;

        private readonly Zone[] _zones;

        private readonly Stopwatch _influenceTime = new Stopwatch();
        private int _influenceCounter;
        private readonly Stopwatch _bilinearTime = new Stopwatch();
        private int _bilinearCounter;

        private const float r = 100;

      
    }
}
