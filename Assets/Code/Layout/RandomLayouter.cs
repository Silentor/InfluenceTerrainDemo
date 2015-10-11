using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Settings;
using Assets.Code.Voronoi;
using UnityEngine;


namespace Assets.Code.Layout
{
    /// <summary>
    /// Makes simple pure random layout for land
    /// </summary>
    public class RandomLayouter
    {
        public RandomLayouter(ILandSettings settings)
        {
            _settings = settings;
        }

        public LandLayout CreateLayout()
        {
            var centers = GeneratePoints(_settings.ZonesCount, _settings.LandBounds, _settings.ZoneCenterMinDistance);
            var zones = SetZoneTypes(centers, _settings);

            return new LandLayout(_settings.LandBounds, zones);
        }

        private readonly ILandSettings _settings;

        /// <summary>
        /// Generate random centers of cells
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        private Vector2[] GeneratePoints(int count, Bounds2i landBounds, float minDistance = 32)
        {
            //Prepare input data
            var infiniteLoopChecker = 0;
            //var chunksGrid = new bool[gridSize * 2, gridSize * 2];

            //Generate zones center coords, check that only one zone occupies one chunk
            var zonesCoords = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var zoneCenterX = Random.Range((float)landBounds.Min.X, landBounds.Max.X);
                var zoneCenterY = Random.Range((float)landBounds.Min.Z, landBounds.Max.Z);
                var newCenter = new Vector2(zoneCenterX, zoneCenterY);
                if (zonesCoords.All(zc => Vector2.SqrMagnitude(zc - newCenter) > minDistance * minDistance))
                //if (IsZoneAllowed(chunksGrid, new Vector2i(zoneCenterX / 16, zoneCenterY / 16), minDistance))
                {
                    //chunksGrid[zoneCenterX / 16, zoneCenterY / 16] = true;
                    zonesCoords.Add(new Vector2 { x = zoneCenterX, y = zoneCenterY });
                }
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

        private Zone[] SetZoneTypes(Vector2[] zoneCenters, ILandSettings settings)
        {
            var cells = CellMeshGenerator.Generate(zoneCenters, settings.LandBounds);
            var zones = new List<Zone>();
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            foreach (var cell in cells)
            {
                ZoneType zoneType;
                //if (cell.IsClosed)
                zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                //else
                //zoneType = ZoneType.Empty;

                zones.Add(new Zone(cell.Center, zoneType));
            }

            return zones.ToArray();
        }
    }
}
