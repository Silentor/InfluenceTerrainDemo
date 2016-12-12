using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class ForestGenerator : ZoneGenerator
    {
        public ForestGenerator(ZoneLayout zone, LandLayout land, LandGenerator generator, LandSettings landSettings) 
            : base(ZoneType.Forest, zone, land, generator, landSettings)
        {
        }

        protected override Vector3[] DecorateZone(ZoneRatio[,] zoneInfluences, float[,] zoneHeightmap, Vector3[,] zoneNormalmap, BlockType[,] zoneBlocks)
        {
           /* var trees = new List<Vector3>();
            var bounds = _zone.Bounds;
            for (int i = 0; i < 100; i++)
            {
                var position = new Vector3(Random.Range(bounds.Min.X, bounds.Max.X), 0, Random.Range(bounds.Min.Z, bounds.Max.Z));
                var blockPos = (Vector2i) position;
                var localBlockPos = blockPos - bounds.Min;

                //Check zone bounds
                if (bounds.Contains(blockPos) && !zoneInfluences[localBlockPos.X, localBlockPos.Z].IsEmpty
                    && zoneInfluences[localBlockPos.X, localBlockPos.Z][ZoneType.Forest] > 0.5f)
                {
                    //Check slopiness
                    if (Vector3.Angle(Vector3.up, zoneNormalmap[localBlockPos.X, localBlockPos.Z]) < 20)
                        trees.Add(new Vector3(position.x, zoneHeightmap[localBlockPos.X, localBlockPos.Z], position.z));
                }
            }

            return trees.ToArray();
            */
            return new Vector3[0];
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            if (Vector3.Angle(Vector3.up, normal) > 40)
                return BlockType.Rock;

            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }
    }
}
