using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Code.Settings;
using UnityEngine;

namespace Assets.Code.Layout
{
    public class Land
    {
        public IEnumerable<Zone> Zones { get { return _zones; } }

        public Land(LandLayout layout, ILandSettings settings)
        {
            _settings = settings;
            _zones = layout.Zones.ToArray();
            _idwCoeff = settings.IDWCoeff;
            _idwOffset = settings.IDWOffset;
            _zoneMaxType = settings.ZoneTypes.Max(z => z.Type);
            _zoneTypesCount = _zones.Distinct(Zone.TypeComparer).Count();
            _zoneSettings = settings.ZoneTypes.ToArray();
            _chunksBounds = new Bounds2i(settings.LandBounds.Min/(settings.BlocksCount*settings.BlockSize),
                settings.LandBounds.Max/(settings.BlocksCount*settings.BlockSize));
        }

        /// <summary>
        /// Get all chunks of given zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public IEnumerable<Vector2i> GetChunks(Zone zone)
        {
            var centerChunk = Chunk.GetChunkPosition(zone.Center);
            var result = new List<Vector2i>();
            var processed = new List<Vector2i>();

            GetChunksFloodFill(zone, centerChunk, processed, result);
            return result;
        }

        public ZoneRatio GetInfluence(Vector2 worldPosition)
        {
            _influenceTime.Start();

            var lookupResult = new float[(int) _zoneMaxType + 1];
            foreach (var zone in _zones)
            {
                var idwSimplestWeighting = IDWSimplestWeighting(zone.Center, worldPosition);
                lookupResult[(int)zone.Type] += idwSimplestWeighting;
            }

            var result = new ZoneRatio(
                lookupResult.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray(), 
                _zoneTypesCount);

            //foreach (var zone in _zones.OrderBy(z => Vector3.SqrMagnitude(z.Center - worldPosition)).Take(10))
            //result[zone.Type] += IDWSimplestWeighting(zone.Center, worldPosition);

            //var result = new ZoneRatio(_zoneMaxType);
            //result.Normalize();

            _influenceTime.Stop();
            _influenceCounter++;

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

        public IZoneNoiseSettings GetZoneNoiseSettings(ZoneRatio influence)
        {
            return ZoneSettings.Lerp(_zoneSettings, influence);
        }

        public string GetStaticstics()
        {
            if (_bilinearCounter == 0)
                _bilinearCounter = 1;

            if (_influenceCounter == 0)
                _influenceCounter = 1;

            return string.Format("Influence {0} ticks per operation, interpolation {1} ticks per operation, zones count {2}",
                _influenceTime.ElapsedTicks / _influenceCounter, _bilinearTime.ElapsedTicks / _bilinearCounter, _zones.Length);
        }

        private readonly ILandSettings _settings;
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

        private float IDWSimplestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            var d = Vector2.SqrMagnitude(interpolatePoint - point);
            var result = (float) (1/Math.Pow(d + 0.001f, _idwCoeff));
            //var result = 100 - Vector2.Distance(interpolatePoint,  point);
            return (float)result;
        }

        private void GetChunksFloodFill(Zone zone, Vector2i from, List<Vector2i> processed, List<Vector2i> result)
        {
            if (!processed.Contains(@from))
            {
                processed.Add(from);

                if (CheckChunk(from, zone))
                {
                    result.Add(from);    
                    GetChunksFloodFill(zone, from + Vector2i.Forward, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Back, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Left, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Right, processed, result);
                }
            }
        }

        /// <summary>
        /// Is chunk belongs to zone?
        /// </summary>
        /// <param name="chunkPosition"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private bool CheckChunk(Vector2i chunkPosition, Zone zone)
        {
            if (!_chunksBounds.Contains(chunkPosition))
                return false;

            var chunkCenter = Chunk.GetChunkCenter(chunkPosition);
            var distance = Vector2.SqrMagnitude(zone.Center - chunkCenter);

            for (var i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] != zone && Vector2.SqrMagnitude(_zones[i].Center - chunkCenter) < distance)
                    return false;
            }

            return true;
        }
    }
}
