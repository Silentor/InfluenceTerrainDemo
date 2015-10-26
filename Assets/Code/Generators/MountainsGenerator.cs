using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Settings;
using UnityEngine;

namespace Assets.Code.Generators
{
    public class MountainsGenerator : ZoneGenerator
    {
        public MountainsGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBLock { get {return BlockType.Grass;} }

        protected override float GenerateBaseHeight(int worldX, int worldZ, IZoneNoiseSettings settings)
        {
            var height = base.GenerateBaseHeight(worldX, worldZ, settings);
            var mountInfluence = Land.GetInfluence(new Vector2(worldX, worldZ))[ZoneType.Mountains];

            //Slightly raise up heightmap of Mountain zone
            if (mountInfluence > 0.7f)
            {
                var additional = Math.Pow(((mountInfluence - 0.7f)*2), 2) + 1;
                height *= (float)additional;
            }

            return height;
        }

        protected override BlockType GenerateBlock(int blockX, int blockZ)
        {
            var mountInfluence = Land.GetInfluence(new Vector2(blockX, blockZ))[ZoneType.Mountains];
            if(mountInfluence > 0.7f)
                return BlockType.Rock;
            else
                return base.GenerateBlock(blockX, blockZ);
        }

        protected override void DecorateChunk(Chunk chunk)
        {
            base.DecorateChunk(chunk);

            var stones = new List<Vector3>();
            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var mountInfluence = chunk.Influence[x, z][ZoneType.Mountains];
                    if (mountInfluence < 0.8f)
                        if (UnityEngine.Random.value < 0.05f * mountInfluence)
                            stones.Add(new Vector3(x, chunk.HeightMap[x, z], z));
                }

            chunk.Stones = stones.ToArray();
        }

    }

}
