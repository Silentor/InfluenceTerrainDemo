using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Code
{
    public static class Generator
    {
        public static int WorldSize = 16;
        public static int BlocksCount = 16;
        public static int BlockSize = 1;
        public static int ChunkSize = BlocksCount*BlockSize;
        public static float IDWCoeff = 2;

        public static WorldSettings World;
        public static ZoneSettings[] Zones;

        public static IEnumerable<Zone> Zones2 { get { return _zones; } }

        private static readonly List<Zone> _zones = new List<Zone>();

        private static readonly Stopwatch _bilinearTime = new Stopwatch();
        private static int _bilinearCounter;

        private static readonly Stopwatch _influenceTime = new Stopwatch();
        private static int _influenceCounter;


        public static void Init(int zonesCount)
        {
            var retryCount = 0;

            for (int i = 0; i < zonesCount; i++)
            {
                var zoneId = Random.Range(0, Zones.Length);
                var worldExtents = WorldSize/2f;
                var zonePosition = new Vector2(Random.Range(-worldExtents, worldExtents), Random.Range(-worldExtents, worldExtents)) *
                                   ChunkSize;

                if (_zones.Any() && _zones.Min(z => Vector3.Distance(z.Position, zonePosition)) < ChunkSize*4)
                {
                    if (retryCount++ < 5)
                        i--;
                    continue;
                }

                _zones.Add(new Zone(zonePosition, zoneId));
            }
        }

        public static Chunk Generate(Vector2 position, bool interpolate)
        {
            ZoneRatio corner11 = null;
            ZoneRatio corner12 = null;
            ZoneRatio corner22 = null;
            ZoneRatio corner21 = null;

            if (interpolate)
            {
                //Get zones influence for chunk corners
                var chunkMin = position*ChunkSize;
                var chunkMax = (position + Vector2.one)*ChunkSize;
                corner11 = GetInfluence(chunkMin);
                corner12 = GetInfluence(new Vector2(chunkMin.x, chunkMax.y));
                corner22 = GetInfluence(chunkMax);
                corner21 = GetInfluence(new Vector2(chunkMax.x, chunkMin.y));
            }

            var chunk = new Chunk(BlocksCount, BlockSize, position, Vector4.zero);

            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var realX = x * chunk.BlockSize + position.x * chunk.Size;
                    var realZ = z * chunk.BlockSize + position.y * chunk.Size;

                    ZoneRatio influence;

                    if (interpolate)
                    {
                        influence = BilinearInterpolation(new Vector2(x, z), Vector2.zero, Vector2.one * ChunkSize, corner11, corner12, corner21, corner22);    
                    }
                    else
                        influence = GetInfluence(new Vector2(realX, realZ));
                    
                    var settings = ZoneSettings.Lerp(Zones, influence);

                    //Workaround of Unity stupid PerlinNoise symmetry
                    realX += 1000;
                    realZ += 1000;

                    var yValue = 0f;
                    yValue +=
                        Mathf.PerlinNoise(realX * World.InScale1, realZ * World.InScale1) * settings.OutScale1;
                    yValue +=
                        Mathf.PerlinNoise(realX * World.InScale2, realZ * World.InScale2) * settings.OutScale2;
                    yValue +=
                        Mathf.PerlinNoise(realX * World.InScale3, realZ * World.InScale3) * settings.OutScale3;
                    yValue += settings.Height;

                    chunk.HeightMap[x, z] = yValue;
                    chunk.Influence[x, z] = influence;
                }

            return chunk;
        }

        public static string GetStaticstics()
        {
            if (_bilinearCounter == 0)
                _bilinearCounter = 1;

            if (_influenceCounter == 0)
                _influenceCounter = 1;

            return string.Format("Influence {0} ticks per operation, interpolation {1} ticks per operation, zones count {2}", 
                _influenceTime.ElapsedTicks/_influenceCounter, _bilinearTime.ElapsedTicks/_bilinearCounter, _zones.Count);
        }

        private static ZoneRatio GetInfluence(Vector2 worldPosition)
        {
            _influenceTime.Start();

            var influence = new float[Zones.Length];

            foreach (var zone in _zones/*.OrderBy(z => Vector3.SqrMagnitude(z.Position - worldPosition)).Take(6)*/)
                influence[zone.Id] += IDWSimplestWeighting(zone.Position, worldPosition);

            var result = new ZoneRatio(influence);
            result.Normalize();

            _influenceTime.Stop();
            _influenceCounter++;

            return result;
        }

        private static ZoneRatio BilinearInterpolation(Vector2 position, Vector2 min, Vector2 max, ZoneRatio q11, ZoneRatio q12, ZoneRatio q21, ZoneRatio q22)
        {
            _bilinearTime.Start();

            var x = position.x;
            var y = position.y;
            var x1 = min.x;
            var x2 = max.x;
            var y1 = min.y;
            var y2 = max.y;

            var result =  (1/(x2 - x1)*(y2 - y1))*
                   (q11 * (x2 - x)*(y2 - y) + q21*(x - x1)*(y2 - y) + q12*(x2 - x)*(y - y1) + q22*(x - x1)*(y - y1));
            result.Normalize();

            _bilinearTime.Stop();
            _bilinearCounter++;

            return result;
        }

        private static float IDWSimplestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }

        private static float NearestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }
    }
}
