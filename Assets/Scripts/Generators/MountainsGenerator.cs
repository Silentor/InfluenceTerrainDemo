using System;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class MountainsGenerator : ZoneGenerator
    {
        public MountainsGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(ZoneType.Mountains, zone, land, landSettings)
        {
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        public override float GenerateBaseHeight(float worldX, float worldZ, ZoneRatio influence)
        {
            var height = base.GenerateBaseHeight(worldX, worldZ, influence);
            
            var mountInfluence = influence[ZoneType.Mountains];

            //Slightly raise up heightmap of Mountain zone
            //if (mountInfluence > 0.7f)
            {
                var additional = /*(mountInfluence + 0.3f)*/1.5f;
                height *= additional;
            }
            
            return height;
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            var mountInfluence = influence[ZoneType.Mountains];

            if (mountInfluence > 0.85f && Vector3.Angle(Vector3.up, normal) < 45)
                return BlockType.Snow;

            if (mountInfluence < 0.6f && Vector3.Angle(Vector3.up, normal) < 45)
                return BlockType.Grass;
            
            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }

        //protected override void DecorateZone(Chunk chunk)
        //{
        //    base.DecorateZone(chunk);

        //    var stones = new List<Vector3>();
        //    for (int x = 0; x < chunk.GridSize; x++)
        //        for (int z = 0; z < chunk.GridSize; z++)
        //        {
        //            var mountInfluence = chunk.Influence[x, z][ZoneType.Mountains];
        //            if (mountInfluence < 0.8f)
        //                if (UnityEngine.Random.value < 0.05f * mountInfluence)
        //                    stones.Add(new Vector3(x, chunk.HeightMap[x, z], z));
        //        }

        //    chunk.Stones = stones.ToArray();
        //}

    }

}
