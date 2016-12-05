using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class SnowGenerator : ZoneGenerator
    {
        public SnowGenerator(ZoneLayout zone, LandLayout land, LandSettings landSettings) : base(ZoneType.Snow, zone, land, landSettings)
        {
        }

        public override BlockType DefaultBlock { get {return BlockType.Snow;} }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            if (Vector3.Angle(Vector3.up, normal) > 75)
                return BlockType.Rock;

            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }
    }
}
