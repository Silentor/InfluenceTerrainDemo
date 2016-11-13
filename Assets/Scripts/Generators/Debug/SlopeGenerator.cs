using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class SlopeGenerator : ZoneGenerator
    {
        public SlopeGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] ILandSettings landSettings) : base(ZoneType.Slope, zone, land, landSettings)
        {
        }

        public override float GenerateBaseHeight(float worldX, float worldZ, ZoneRatio influence)
        {
            //var settings = Land.GetZoneNoiseSettings(influence);
            return _zoneSettings.Height + worldX;
        }
    }
}
