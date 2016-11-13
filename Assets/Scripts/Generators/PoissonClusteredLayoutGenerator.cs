using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Distributes zone centers by Poisson, cluster same zone types together
    /// </summary>
    public class PoissonClusteredLayoutGenerator : PoissonLayoutGenerator
    {
        public PoissonClusteredLayoutGenerator(ILandSettings settings) : base(settings)
        {
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            var ordinaryZones = settings.ZoneTypes.Where(zt => !zt.IsInterval).Select(z => z.Type).ToArray();
            var zones = new ZoneType[cells.Length];

            //Calculate zone types
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] == ZoneType.Empty)
                {
                    //Start cluster
                    var zoneType = ordinaryZones[Random.Range(0, ordinaryZones.Length)];
                    //var clusterSize = Random.Range(2, 5);
                    var clusterSize = cells.Length/3;
                    var zoneIndexes = GetFreeNeighborsDepthFirst(cells, zones, i, clusterSize);
                    foreach (var zoneIndex in zoneIndexes)
                        zones[zoneIndex] = zoneType;
                }
            }

            //Generate some interval zones
            var intervalZone = settings.ZoneTypes.FirstOrDefault(zt => zt.IsInterval);
            if (intervalZone != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    var cell = cells[i];

                    //Place interval zone type between all ordinary zones
                    if (GetNeighborsOf(cell, zones).Where(zt => ordinaryZones.Contains(zt)).Distinct().Count() > 1)
                    {
                        zones[i] = intervalZone.Type;
                    }
                }
            }

            return zones;
        }

        /// <summary>
        /// Search for unassigned neighbors zones
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="zones"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
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

            count--;
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

        private IEnumerable<ZoneType> GetNeighborsOf(Cell cell, ZoneType[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id]);
        }
    }
}
