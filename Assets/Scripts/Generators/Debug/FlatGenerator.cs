using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class FlatGenerator : ZoneGenerator
    {
        public FlatGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(ZoneType.Hills, zone, land, landSettings)
        {
        }

        public override BlockType DefaultBlock
        {
            get { return BlockType.Influence; }
        }

        public override float GenerateBaseHeight(float worldX, float worldZ, ZoneRatio influence)
        {
            return 0;
        }
    }
}
