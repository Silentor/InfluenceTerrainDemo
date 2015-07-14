using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public static class Main
    {
        public static void FillMap(int mapSize)
        {
            mapSize /= 2;

            for (int x = -mapSize; x < mapSize; x++)
            {
                for (int z = -mapSize; z < mapSize; z++)
                {
                    var chunk = Generator.Generate(new Vector2(x, z));
                    var mesh = Mesher.Generate(chunk);
                    var go = ChunkGO.Create(chunk, mesh);

                    Debug.Log("Created chunk " + chunk.Position);
                }
            }
        }
    }
}
