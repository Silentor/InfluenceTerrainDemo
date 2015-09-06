using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public static class Main
    {
        public static void FillMap(int mapSize, bool interpolate)
        {
            mapSize /= 2;

            for (int x = -mapSize; x < mapSize; x++)
            {
                for (int z = -mapSize; z < mapSize; z++)
                {
                    FillChunk(new Vector2(x, z), interpolate);
                }
            }
        }

        public static IEnumerator FillMapAsync(int mapSize, bool interpolate)
        {
            mapSize /= 2;

            for (int x = -mapSize; x < mapSize; x++)
            {
                for (int z = -mapSize; z < mapSize; z++)
                {
                    FillChunk(new Vector2(x, z), interpolate);
                    yield return null;
                }
            }
        }

        public static void FillChunk(Vector2 position, bool interpolate)
        {
            var chunk = Generator.Generate(position, interpolate);
            var mesh = Mesher.Generate(chunk);
            var go = ChunkGO.Create(chunk, mesh);
        }
    }
}
