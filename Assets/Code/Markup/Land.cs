using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Code
{
    public class Land
    {
        public IEnumerable<Zone> Zones { get { return _zones; } }

        public readonly float InScale1;
        public readonly float InScale2;
        public readonly float InScale3;

        public Land(int zonesTotal, ZoneSettings[] zones, int worldSize, int chunkSize, float idwCoeff, WorldSettings landSettings)
        {
            _zonesTotal = zonesTotal;
            _zoneSettings = zones;
            _worldSize = worldSize;
            _chunkSize = chunkSize;
            _idwCoeff = idwCoeff;

            InScale1 = landSettings.InScale1;
            InScale2 = landSettings.InScale2;
            InScale3 = landSettings.InScale3;
        }

        public void Generate()
        {
            _zones.Clear();

            var retryCount = 0;

            for (int i = 0; i < _zonesTotal; i++)
            {
                var zoneIndex = Random.Range(0, _zoneSettings.Length);
                var worldExtents = _worldSize / 2f;
                var zonePosition = new Vector2(Random.Range(-worldExtents, worldExtents), Random.Range(-worldExtents, worldExtents)) *
                                   _chunkSize;

                if (_zones.Any(z => Vector2.Distance(z.Center, zonePosition) < _chunkSize * 4))
                {
                    if (retryCount++ < 5)
                        i--;
                    continue;
                }

                _zones.Add(new Zone(zonePosition, _zoneSettings[zoneIndex].Type));
            }

            foreach (var zoneMarkup in _zones)
                zoneMarkup.Init(this);
        }

        /// <summary>
        /// Get chunks
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public IEnumerable<Vector2i> GetChunks(Zone zone)
        {
            var centerChunk = Chunk.GetChunkPosition(zone.Center, _chunkSize);
            var result = new List<Vector2i>();
            var processed = new List<Vector2i>();

            GetChunksFloodFill(_zones.IndexOf(zone), centerChunk, processed, result);
            return result;
        }

        public ZoneRatio GetInfluence(Vector2 worldPosition)
        {
            _influenceTime.Start();

            var influence = new float[_zoneSettings.Length];

            foreach (var zone in _zones/*.OrderBy(z => Vector3.SqrMagnitude(z.Position - worldPosition)).Take(6)*/)
                influence[(int)zone.Type] += IDWSimplestWeighting(zone.Center, worldPosition);

            var result = new ZoneRatio(influence);
            result.Normalize();

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

        public IZoneSettings GetZoneSettings(ZoneRatio influence)
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
                _influenceTime.ElapsedTicks / _influenceCounter, _bilinearTime.ElapsedTicks / _bilinearCounter, _zones.Count);
        }

        private readonly int _zonesTotal;
        //public static int ZonesCount;
        //public static int WorldSize;
        //public static int ChunkSize;

        private readonly ZoneSettings[] _zoneSettings;
        private readonly int _worldSize;
        private readonly int _chunkSize;
        private readonly float _idwCoeff;

        private readonly List<Zone> _zones = new List<Zone>();

        private readonly Stopwatch _influenceTime = new Stopwatch();
        private int _influenceCounter;
        private readonly Stopwatch _bilinearTime = new Stopwatch();
        private int _bilinearCounter;

        private float IDWSimplestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), _idwCoeff));
        }

        private void GetChunksFloodFill(int zoneIndex, Vector2i from, List<Vector2i> processed, List<Vector2i> result)
        {
            if (!processed.Contains(@from))
            {
                processed.Add(from);

                if (CheckChunk(from, zoneIndex))
                {
                    result.Add(from);    
                    GetChunksFloodFill(zoneIndex, from + Vector2i.Forward, processed, result);
                    GetChunksFloodFill(zoneIndex, from + Vector2i.Back, processed, result);
                    GetChunksFloodFill(zoneIndex, from + Vector2i.Left, processed, result);
                    GetChunksFloodFill(zoneIndex, from + Vector2i.Right, processed, result);
                }
            }
        }

        /// <summary>
        /// Is chunk belongs to zone
        /// </summary>
        /// <param name="chunkPosition"></param>
        /// <param name="zoneIndex"></param>
        /// <returns></returns>
        private bool CheckChunk(Vector2i chunkPosition, int zoneIndex)
        {
            if (chunkPosition.X < -_worldSize/2 || chunkPosition.X > _worldSize / 2
                || chunkPosition.Z < -_worldSize / 2 || chunkPosition.Z > _worldSize / 2)
                return false;

            var chunkCenter = Chunk.GetChunkCenter(chunkPosition, _chunkSize);
            var distance = Vector2.SqrMagnitude(_zones[zoneIndex].Center - chunkCenter);

            for (var i = 0; i < _zones.Count; i++)
            {
                if (i != zoneIndex && Vector2.SqrMagnitude(_zones[i].Center - chunkCenter) < distance)
                    return false;
            }

            return true;
        }
    }
}
