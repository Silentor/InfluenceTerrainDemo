using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Settings;
using Assets.Code.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;


namespace Assets.Code.Layout
{
    /// <summary>
    /// Combine zones of some types together
    /// </summary>
    public class ClusteredLayouter : ILayouter
    {
        public ClusteredLayouter(ILandSettings settings)
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
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();
            var zones = new Zone[cells.Length];

            //Calculate zone types
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                {
                    //Start cluster
                    var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                    var clusterSize = Random.Range(2, 5);
                    var zoneIndexes = GetFreeNeighborsDepthFirst(cells, zones, i, clusterSize);
                    foreach (var zoneIndex in zoneIndexes)
                        zones[zoneIndex] = new Zone(cells[zoneIndex], zoneType);
                }
            }

            //Init zones
            var allZones = new Dictionary<Cell, Zone>(zones.Length);
            for (var i = 0; i < zones.Length; i++)
                allZones.Add(cells[i], zones[i]);
            for (var i = 0; i < zones.Length; i++)
                zones[i].Init(allZones);

            return zones;
        }

        private IEnumerable<int> GetFreeNeighborsDepthFirst(Cell[] cells, Zone[] zones, int startIndex, int count)
        {
            var result = new List<int>(count);

            var assertCount = count;
            GetFreeNeighborsDepthFirstRecursive(cells, zones, startIndex, ref count, result);
            Assert.IsTrue(result.Count <= assertCount);

            return result;
        }

        private void GetFreeNeighborsDepthFirstRecursive(Cell[] cells, Zone[] zones, int startIndex, ref int count,
            List<int> result)
        {
            if (zones[startIndex] != null || result.Contains(startIndex) || count <= 0)
                return;

            count --;
            result.Add(startIndex);

            if (count == 0)
                return;

            var freeNeighbors = cells[startIndex].Neighbors.Where(nc => zones[nc.Id] == null).ToArray();
            if (freeNeighbors.Length > 0)
            {
                var neighborIndex = freeNeighbors[Random.Range(0, freeNeighbors.Length)].Id;
                GetFreeNeighborsDepthFirstRecursive(cells, zones, neighborIndex, ref count, result);
            }
        }
    }
}
