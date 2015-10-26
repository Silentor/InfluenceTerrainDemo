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
    public class ClusteredLayout : LandLayout
    {
        public ClusteredLayout(ILandSettings settings) : base(settings)
        {
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
