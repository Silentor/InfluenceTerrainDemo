using System;
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
    [Obsolete("Base height generation need rework (zone layout generator support)")]
    public class FoothillsGenerator : ZoneGenerator
    {
        public FoothillsGenerator(ZoneLayout zone, LandLayout land, LandGenerator generator, LandSettings landSettings) 
            : base(ZoneType.Foothills, zone, land, generator, landSettings)
        {
            var plains = Land.Zones.Where(z => z.Type == ZoneType.Hills).ToArray();
            var mountains = Land.Zones.Where(z => z.Type == ZoneType.Mountains).ToArray();
            var zones = plains.Concat(mountains).ToArray();

            var mountainGenerator = new MountainsGenerator(zone, land, generator, landSettings);
            var plainsGenerator = new HillsGenerator(zone, land, generator, landSettings);

            var mountHeights = mountains.Select(
                m => mountainGenerator.GenerateBaseHeight(m.Center.x, m.Center.y)).ToArray();
            var plainsHeight = plains.Select(
                p => plainsGenerator.GenerateBaseHeight(p.Center.x, p.Center.y)).ToArray();

            var heights = plainsHeight.Concat(mountHeights).ToArray();
            _heights = zones.Select((z, i) => new ZoneHeight {Zone = z, Height = heights[i]}).ToArray();
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            var vertex = new Vector2(worldX, worldZ);
            var a = 0d;
            var b = 0d;

            //Calculate simple IDW for all plains+mountains zones
            for (int i = 0; i < _heights.Length; i++)
            {
                var zone = _heights[i];
                var d = Vector2.SqrMagnitude(zone.Zone.Center - vertex);

                var m = 1d/(d);
                a += m * zone.Height;
                b += m;
            }

            var foothillHeight = a/b;

            //Workaround Unity Perlin noise implementation symmetry 'bug'
            worldX += 1000;
            worldZ += 1000;

            //foothillHeight +=
            //    (Mathf.PerlinNoise(worldX * _landSettings.LandNoiseSettings.InScale3, worldZ * _landSettings.LandNoiseSettings.InScale3) - 0.5f) *
            //    _zoneSettings.OutScale3;

            //foothillHeight +=
            //    (Mathf.PerlinNoise(worldX * _landSettings.LandNoiseSettings.InScale1, worldZ * _landSettings.LandNoiseSettings.InScale1) - 0.5f) *
            //    _zoneSettings.OutScale1;

            var result = foothillHeight;
            return result;
        }

        private readonly ZoneHeight[] _heights;

        private struct ZoneHeight
        {
            public ZoneLayout Zone;
            public double Height;
        }
    }
}
