using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Makes simple pure random layout for land
    /// </summary>
    public class RandomLayout : LandLayout
    {
        public RandomLayout(ILandSettings settings) : base(settings)
        {
        }

        protected override Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density)
        {
            //Prepare input data
            var infiniteLoopChecker = 0;

            //Generate zones center coords, check that only one zone occupies one chunk
            var zonesCoords = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var zoneCenterX = Random.Range((float)landBounds.Min.X, landBounds.Max.X);
                var zoneCenterY = Random.Range((float)landBounds.Min.Z, landBounds.Max.Z);
                var newCenter = new Vector2(zoneCenterX, zoneCenterY);
                if (zonesCoords.All(zc => Vector2.SqrMagnitude(zc - newCenter) > density.x*density.x))
                    zonesCoords.Add(new Vector2 {x = zoneCenterX, y = zoneCenterY});
                else
                {
                    if (infiniteLoopChecker++ < 100)
                        i--;
                    else
                        break;
                }
            }

            return zonesCoords.ToArray();
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            var zones = new ZoneType[cells.Length];
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            for (var i = 0; i < zones.Length; i++)
            {
                ZoneType zoneType;
                //if (cell.IsClosed)
                zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                //else
                //zoneType = ZoneType.Empty;

                zones[i] = zoneType;
            }
            
            return zones;
        }
    }
}
