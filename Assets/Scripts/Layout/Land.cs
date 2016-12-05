﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Layout
{
    public class Land
    {
        public IEnumerable<Zone> Zones { get { return _zones; } }

        /// <summary>
        /// World coords bounds
        /// </summary>
        public Bounds2i Bounds { get { return _settings.LandBounds; } }

        public readonly LandLayout Layout;

        public Land(LandLayout layout, LandSettings settings)
        {
            _settings = settings;
            _zones = layout.Zones.Select(z => new Zone(z)).ToArray();
            Layout = layout;
            _idwCoeff = settings.IDWCoeff;
            _idwOffset = settings.IDWOffset;
            _zoneMaxType = settings.Zones.Max(z => z.Type);
            _zoneTypesCount = _zones.Where(z => z.Type != ZoneType.Empty).Distinct(Zone.TypeComparer).Count();
            _zoneSettings = settings.Zones.ToArray();
            _chunksBounds = new Bounds2i(settings.LandBounds.Min/(settings.BlocksCount*settings.BlockSize),
                settings.LandBounds.Max/(settings.BlocksCount*settings.BlockSize));
        }

        /// <summary>
        /// Get zone to which given point belongs
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private Zone GetZoneBy(Vector2 worldPosition)
        {
            if (!Bounds.Contains((Vector2i)worldPosition))
                return null;

            Zone result = null;
            var minDistance = float.MaxValue;
            for (var i = 0; i < _zones.Length; i++)
            {
                var distance = Vector2.SqrMagnitude(_zones[i].Center - worldPosition);
                if (distance < minDistance)
                    result = _zones[i];
            }

            return result;
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

        private readonly LandSettings _settings;
        //public static int ZonesCount;
        //public static int WorldSize;
        //public static int ChunkSize;

        private readonly Zone[] _zones;

        private readonly Stopwatch _influenceTime = new Stopwatch();
        private int _influenceCounter;
        private readonly Stopwatch _bilinearTime = new Stopwatch();
        private int _bilinearCounter;
        private float _idwCoeff;
        private ZoneType _zoneMaxType;
        private ZoneSettings[] _zoneSettings;
        private float _idwOffset;
        private int _zoneTypesCount;
        private Bounds2i _chunksBounds;

        private const float r = 100;

      
    }
}
