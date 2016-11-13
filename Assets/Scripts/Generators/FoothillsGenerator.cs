using System.Linq;
using System.Xml;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Interval zone, smoothly transfer from plains to mountains
    /// </summary>
    public class FoothillsGenerator : ZoneGenerator
    {
        public FoothillsGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(ZoneType.Foothills, zone, land, landSettings)
        {
            var plains = Land.Zones.Where(z => z.Type == ZoneType.Hills).ToArray();
            var mountains = Land.Zones.Where(z => z.Type == ZoneType.Mountains).ToArray();
            var zones = plains.Concat(mountains).ToArray();

            var mountainGenerator = new MountainsGenerator(zone, land, landSettings);
            var plainsGenerator = new HillsGenerator(zone, land, landSettings);

            var mountHeights = mountains.Select(
                m => mountainGenerator.GenerateBaseHeight(m.Center.x, m.Center.y, land.GetInfluence(m.Center))).ToArray();
            var plainsHeight = plains.Select(
                p => plainsGenerator.GenerateBaseHeight(p.Center.x, p.Center.y, land.GetInfluence(p.Center))).ToArray();

            var heights = plainsHeight.Concat(mountHeights).ToArray();
            _heights = zones.Select((z, i) => new ZoneHeight {Zone = z, Height = heights[i]}).ToArray();
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        public override float GenerateBaseHeight(float worldX, float worldZ, ZoneRatio influence)
        {
            var vertex = new Vector2(worldX, worldZ);
            var a = 0f;
            var b = 0f;

            //Calculate simple IDW for all plains+mountains zones
            for (int i = 0; i < _heights.Length; i++)
            {
                var zone = _heights[i];
                var d = Vector2.SqrMagnitude(zone.Zone.Center - vertex);

                var m = 1/(d);
                a += m * zone.Height;
                b += m;
            }

            var foothillHeight = a/b;

            //Workaround Unity Perlin noise implementation symmetry 'bug'
            worldX += 1000;
            worldZ += 1000;

            foothillHeight +=
                (Mathf.PerlinNoise(worldX * _landSettings.LandNoiseSettings.InScale3, worldZ * _landSettings.LandNoiseSettings.InScale3) - 0.5f) *
                _zoneSettings.OutScale3;

            foothillHeight +=
                (Mathf.PerlinNoise(worldX * _landSettings.LandNoiseSettings.InScale1, worldZ * _landSettings.LandNoiseSettings.InScale1) - 0.5f) *
                _zoneSettings.OutScale1;

            var result = foothillHeight + _zoneSettings.Height;
            return result;
        }

        private readonly ZoneHeight[] _heights;

        private struct ZoneHeight
        {
            public ZoneLayout Zone;
            public float Height;
        }
    }
}
