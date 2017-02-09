using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Distributes zone centers by Poisson, cluster same zone types together
    /// </summary>
    public class PoissonClusteredLayoutGenerator : PoissonLayoutGenerator
    {
        public PoissonClusteredLayoutGenerator(LandSettings settings) : base(settings)
        {
        }

        protected override ClusterInfo[] SetClusters(CellMesh mesh, LandSettings settings)
        {
            var zoneTypes = settings.Zones.Select(z => z.Type).ToArray();
            var clusters = new List<ClusterInfo>();
            var zones = new ZoneInfo[mesh.Cells.Length];
            var clusterId = 0;

            //Calculate zone types
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i].Type == ZoneType.Empty)
                {
                    //Fill cluster
                    clusterId++;
                    
                    var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                    var clusterSize = Mathf.Max(mesh.Cells.Length/10, 1);
                    var neighbors = mesh.GetNeighbors(mesh[i], c => zones[c.Id].Type == ZoneType.Empty).Take(clusterSize).ToArray();
                    var clusterCells = new List<Cell>(neighbors.Length + 1);
                    clusterCells.Add(mesh[i]);
                    clusterCells.AddRange(neighbors);
                    var heightPoints = GetHeightsForCluster(zoneType, clusterCells.ToArray(), mesh);

                    var zonesInCluster = new List<ZoneInfo>();
                    foreach (var cell in clusterCells)
                    {
                        var zoneInfo = new ZoneInfo {Id = cell.Id, ClusterId = clusterId, Type = zoneType};
                        zones[cell.Id] = zoneInfo;
                        zonesInCluster.Add(zoneInfo);
                    }

                    var newCluster = new ClusterInfo { Id = clusterId, Type = zoneType, Zones = zonesInCluster.ToArray() };
                    
                    newCluster.Heights = heightPoints;
                    clusters.Add(newCluster);
                }
            }

            return clusters.ToArray();
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

        private IEnumerable<ZoneType> GetNeighborsOf(Cell cell, ZoneInfo[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id].Type);
        }

        private Vector3[] GetHeightsForCluster(ZoneType type, [NotNull] Cell[] clusterCells, CellMesh mesh)
        {
            if (clusterCells == null) throw new ArgumentNullException("clusterCells");
            if (clusterCells.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", "clusterCells");

            //Get cluster center
            Vector2 center = Vector2.zero;
            foreach (var clusterCell in clusterCells)
            {
                center += clusterCell.Center;
            }
            center /= clusterCells.Length;

            if (type == ZoneType.Mountains)
            {
                var heights = new List<Vector3>();
                var mountains = new CellMesh.Submesh(mesh, clusterCells);
                heights.AddRange(mountains.GetBorderCells().Select(c => new Vector3(c.Center.x, 10, c.Center.y)));
                heights.Add(new Vector3(center.x, 50, center.y));
                return heights.ToArray();
            }
            else if(type == ZoneType.Lake)
                return new[] { new Vector3(center.x, -20, center.y), };
            else
                return new[] { new Vector3(center.x, 0, center.y), };
        }
    }
}
