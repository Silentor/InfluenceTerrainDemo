using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class SlopeGenerator : ZoneGenerator
    {
        public SlopeGenerator(ZoneLayout zone, [NotNull] LandLayout land, LandGenerator generator, [NotNull] LandSettings landSettings) 
            : base(ZoneType.Slope, zone, land, generator, landSettings)
        {
        }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            //var settings = Land.GetZoneNoiseSettings(influence);
            return worldX;
        }
    }
}
