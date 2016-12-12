using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class LakeGenerator : ZoneGenerator
    {
        public LakeGenerator(ZoneLayout zone, LandLayout land, LandGenerator generator, LandSettings landSettings) 
            : base(ZoneType.Lake, zone, land, generator, landSettings)
        {
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            if (Vector3.Angle(Vector3.up, normal) < 5)
                return BlockType.Water;

            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }
    }
}
