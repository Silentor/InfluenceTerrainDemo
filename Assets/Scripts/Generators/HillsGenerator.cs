using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class HillsGenerator : ZoneGenerator
    {
        public HillsGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            if (Vector3.Angle(Vector3.up, normal) > 65)
                return BlockType.Rock;

            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }
    }
}
