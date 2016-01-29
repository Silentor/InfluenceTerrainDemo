using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Makes simple pure random layout for land
    /// </summary>
    public class TestLayout : LandLayout
    {
        public TestLayout(ILandSettings settings) : base(settings)
        {
        }

        protected override Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density)
        {
            return new[] {new Vector2(8, 9), new Vector2(-8, 24), new Vector2(10, -8), new Vector2(17, 18) };
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            var zones = new ZoneType[cells.Length];
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            for (var i = 0; i < zones.Length; i++)
            {
                var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                zones[i] = zoneType;
            }

            return zones;
        }
    }
}
