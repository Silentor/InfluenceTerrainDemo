using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Combine zones of some types together
    /// </summary>
    public class PoissonClusteredLayout : LandLayout
    {
        public PoissonClusteredLayout(ILandSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected override Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density)
        {
            var checkedPoints = new List<Vector2>();
            var uncheckedPoints = new List<Vector2>();

            //Generate start point
            var zoneCenterX = Random.Range((float)landBounds.Min.X, landBounds.Max.X);
            var zoneCenterY = Random.Range((float)landBounds.Min.Z, landBounds.Max.Z);
            var startPoint = new Vector2(zoneCenterX, zoneCenterY);

            uncheckedPoints.Add(startPoint);

            //Generate point around first unchecked
            while (uncheckedPoints.Any())
            {
                var processedPoint = uncheckedPoints.First();
                uncheckedPoints.RemoveAt(0);

                for (int i = 0; i < 10; i++)
                {
                    var r = Random.Range(density.x + 0.1f, density.y);
                    var a = Random.Range(0, 2*Mathf.PI);
                    var newPoint = processedPoint + new Vector2(r * Mathf.Cos(a), r*Mathf.Sin(a));

                    if (landBounds.Contains((Vector2i) newPoint))
                    {
                        if(checkedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x) 
                            && uncheckedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x))
                            uncheckedPoints.Add(newPoint);
                    }
                }

                checkedPoints.Add(processedPoint);
                if (checkedPoints.Count >= count)
                    break;
            }

            return checkedPoints.ToArray();
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();
            var zones = new ZoneType[cells.Length];

            //Calculate zone types
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] == ZoneType.Empty)
                {
                    //Start cluster
                    var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                    var clusterSize = Random.Range(2, 5);
                    var zoneIndexes = GetFreeNeighborsDepthFirst(cells, zones, i, clusterSize);
                    foreach (var zoneIndex in zoneIndexes)
                        zones[zoneIndex] = zoneType;
                }
            }

            return zones;
        }

        private IEnumerable<int> GetFreeNeighborsDepthFirst(Cell[] cells, ZoneType[] zones, int startIndex, int count)
        {
            var result = new List<int>(count);

            var assertCount = count;
            GetFreeNeighborsDepthFirstRecursive(cells, zones, startIndex, ref count, result);
            Assert.IsTrue(result.Count <= assertCount);

            return result;
        }

        private void GetFreeNeighborsDepthFirstRecursive(Cell[] cells, ZoneType[] zones, int startIndex, ref int count,
            List<int> result)
        {
            if (zones[startIndex] != ZoneType.Empty || result.Contains(startIndex) || count <= 0)
                return;

            count --;
            result.Add(startIndex);

            if (count == 0)
                return;

            var freeNeighbors = cells[startIndex].Neighbors.Where(nc => zones[nc.Id] == ZoneType.Empty).ToArray();
            if (freeNeighbors.Length > 0)
            {
                var neighborIndex = freeNeighbors[Random.Range(0, freeNeighbors.Length)].Id;
                GetFreeNeighborsDepthFirstRecursive(cells, zones, neighborIndex, ref count, result);
            }
        }
    }
}
