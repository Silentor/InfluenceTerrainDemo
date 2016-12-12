using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class FlatGenerator : ZoneGenerator
    {
        public FlatGenerator(ZoneLayout zone, LandLayout land, LandGenerator generator, LandSettings landSettings) 
            : base(ZoneType.Hills, zone, land, generator, landSettings)
        {
        }

        public override BlockType DefaultBlock
        {
            get { return BlockType.Influence; }
        }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            return 0;
        }
    }
}
