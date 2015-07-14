using System;
using System.Collections.Generic;
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

        public static GeneratorSettings[] Zones;

        public static IEnumerable<Zone> Zones2 { get { return _zones; } }

        private static readonly List<Zone> _zones = new List<Zone>();

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

        public static Chunk Generate(Vector2 position)
        {
            //Get zones influence for chunk corners
            //var chunkMin = position*ChunkSize;
            //var chunkMax = (position + Vector2.one) * ChunkSize;
            //var corner1 = GetInfluence(chunkMin);
            //var corner2 = GetInfluence(new Vector2(chunkMin.x, chunkMax.y));
            //var corner3 = GetInfluence(chunkMax);
            //var corner4 = GetInfluence(new Vector2(chunkMax.x, chunkMin.y));
            
            var chunk = new Chunk(BlocksCount, BlockSize, position, Vector4.zero);

            //Workaround of Unity stupid PerlinNoise symmetry
            

            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var realX = x * chunk.BlockSize + position.x * chunk.Size;
                    var realZ = z * chunk.BlockSize + position.y * chunk.Size;
                    var influence = GetInfluence(new Vector2(realX, realZ));
                    var settings = GeneratorSettings.Lerp(Zones, influence);

                    realX += 1000;
                    realZ += 1000;
                    var yValue = 0f;
                    yValue +=
                        Mathf.PerlinNoise(realX * settings.InScale1, realZ * settings.InScale1) * settings.OutScale1;
                    yValue +=
                        Mathf.PerlinNoise(realX * settings.InScale2, realZ * settings.InScale2) * settings.OutScale2;
                    yValue +=
                        Mathf.PerlinNoise(realX * settings.InScale3, realZ * settings.InScale3) * settings.OutScale3;
                    yValue += settings.Height;

                    chunk.HeightMap[x, z] = yValue;
                }

            return chunk;
        }

        private static Vector3 BilinearInterpolation(Vector2 position, Vector2 min, Vector2 max, Vector3 q11, Vector3 q12, Vector3 q21, Vector3 q22)
        {
            var x = position.x;
            var y = position.y;
            var x1 = min.x;
            var x2 = max.x;
            var y1 = min.y;
            var y2 = max.y;

            return (1/(x2 - x1)*(y2 - y1))*
                   (q11*(x2 - x)*(y2 - y) + q21*(x - x1)*(y2 - y) + q12*(x2 - x)*(y - y1) + q22*(x - x1)*(y - y1));
        }

        private static float IDWSimplestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }

        private static float NearestWeighting(Vector2 interpolatePoint, Vector2 point)
        {

            return (float)(1 / Math.Pow(Vector2.Distance(interpolatePoint, point), IDWCoeff));
        }

        public static float[] GetInfluence(Vector2 worldPosition)
        {
            var influence = new float[Zones.Length];

            //var nearestZone = _zones.OrderBy(z => Vector3.SqrMagnitude(z.Position - worldPosition)).First();
            //influence[nearestZone.Id] += IDWSimplestWeighting(worldPosition, nearestZone.Position);

            foreach (var zone in _zones/*.OrderBy(z => Vector3.SqrMagnitude(z.Position - worldPosition)).Take(7)*/)
                influence[zone.Id] += IDWSimplestWeighting(zone.Position, worldPosition);

            //Normalize
            var sum = influence.Sum();
            for (var i = 0; i < influence.Length; i++)
                influence[i] = influence[i]/sum;

            return influence;
        }
    }
}
