using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class DefaultGenerator : ZoneGenerator
    {
        public DefaultGenerator(ZoneLayout zone, LandLayout land, LandSettings landSettings) : base(ZoneType.Hills, zone, land, landSettings)
        {
            
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            if (Vector3.Angle(Vector3.up, normal) > 50)
                return BlockType.Rock;

            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }
    }
}
