using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators
{
    public class FlatGenerator : ZoneGenerator
    {
        public FlatGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBlock
        {
            get { return BlockType.Influence; }
        }

        protected override float GenerateBaseHeight(float worldX, float worldZ, IZoneNoiseSettings settings)
        {
            return 0;
        }
    }
}
