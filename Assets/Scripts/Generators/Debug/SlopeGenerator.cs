using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators.Debug
{
    public class SlopeGenerator : ZoneGenerator
    {
        public SlopeGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        protected override float GenerateBaseHeight(float worldX, float worldZ, IZoneNoiseSettings settings)
        {
            return settings.Height + worldX;
        }
    }
}
