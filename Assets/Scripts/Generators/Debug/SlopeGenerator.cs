using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class SlopeGenerator : ZoneGenerator
    {
        public SlopeGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] LandSettings landSettings) : base(ZoneType.Slope, zone, land, landSettings)
        {
        }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            //var settings = Land.GetZoneNoiseSettings(influence);
            return _zoneSettings.Height + worldX;
        }
    }
}
