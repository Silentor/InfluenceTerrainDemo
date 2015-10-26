using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Settings;
using UnityEngine;

namespace Assets.Code.Generators
{
    public class ForestGenerator : ZoneGenerator
    {
        public ForestGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        protected override void DecorateChunk(Chunk chunk)
        {
            base.DecorateChunk(chunk);

            var trees = new List<Vector3>();
            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var forestInfluence = chunk.Influence[x, z][ZoneType.Forest];
                    if (forestInfluence > 0.5f)
                        if (UnityEngine.Random.value < 0.1 * forestInfluence)
                            trees.Add(new Vector3(x, chunk.HeightMap[x, z], z));
                }

            chunk.Flora = trees.ToArray();
        }
    }
}
