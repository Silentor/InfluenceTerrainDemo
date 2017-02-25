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
            var clusterId = -1;

            //Get clusters Poisson points
            var clusterCenters = GeneratePoints(settings.LandBounds, new Vector2(100, 100));        //Вынести в настройки

            //Fill main clusters
            for (int i = 0; i < clusterCenters.Length; i++)
            {
                var startCell = mesh.GetCellFor(clusterCenters[i]);
                if (zones[startCell.Id].Type == ZoneType.Empty)
                {
                    clusterId++;
                    var clusterSize = settings.ClusterSize.GetRandomRange();
                    var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];

                    var newCluster = FillCluster(mesh, startCell, clusterSize, zoneType, zones, clusterId);
                    clusters.Add(newCluster);
                }
            }

            UnityEngine.Debug.LogFormat("Main clusters: 0 - {0}", clusterId);

            //Fill remaining gaps between main clusters
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i].Type == ZoneType.Empty)
                {
                    clusterId++;
                    var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                    var clusterSize = settings.ClusterSize.GetRandomRange();

                    var newCluster = FillCluster(mesh, mesh.Cells[i], clusterSize, zoneType, zones, clusterId);
                    clusters.Add(newCluster);
                }
            }

            //Postprocessing
            foreach (var cluster in clusters)
            {
                //Calculate cluster mesh
                var border = cluster.Mesh.GetBorderCells();
                var neighborClusterIds = border.SelectMany(c => c.Neighbors)
                    .Select(c => zones[c.Id].ClusterId)
                    .Where(cid => cid != cluster.Id)
                    .Distinct().ToArray();
                cluster.Neighbors = neighborClusterIds.Select(cid => clusters[cid]).ToArray();

                //Calculate heights
                cluster.ClusterHeights = GetClusterHeights(cluster, mesh);
                cluster.ZoneHeights = GetZoneHeights(cluster, mesh, zones);
            }

            return clusters.ToArray();
        }

        private static ClusterInfo FillCluster(CellMesh mesh, Cell startCell, int clusterSize, ZoneType zoneType, ZoneInfo[] zones, int clusterId)
        {
            var neighbors = mesh.GetNeighbors(startCell, c => zones[c.Id].Type == ZoneType.Empty).Take(clusterSize).ToArray();
            var clusterCells = new List<Cell>(neighbors.Length + 1);
            clusterCells.Add(startCell);
            clusterCells.AddRange(neighbors);

            var zonesInCluster = new List<ZoneInfo>(clusterCells.Count);
            foreach (var cell in clusterCells)
            {
                var zoneInfo = new ZoneInfo {Id = cell.Id, ClusterId = clusterId, Type = zoneType};
                zones[cell.Id] = zoneInfo;
                zonesInCluster.Add(zoneInfo);
            }

            var newCluster = new ClusterInfo
            {
                Id = clusterId,
                Type = zoneType,
                Zones = zonesInCluster.ToArray(),
                Mesh = new CellMesh.Submesh(mesh, clusterCells.ToArray()),
            };
            return newCluster;
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

        private Vector3[] GetClusterHeights(ClusterInfo cluster, CellMesh mesh)
        {
            //Get most centered cell of cluster
            var borders = cluster.Mesh.GetBorderCells().ToArray();
            var floodFill = cluster.Mesh.FloodFill(borders);
            Cell[] mostCenteredCells = floodFill.GetNeighbors(0).ToArray();
            for (int floodFillStep = 1; floodFillStep < 10; floodFillStep++)
            {
                var floodFillResult = floodFill.GetNeighbors(floodFillStep).ToArray();
                if (floodFillResult.Count() == 0)
                    break;
                else
                    mostCenteredCells = floodFillResult;
            }

            //Find most centered cell by geometrical center distance;
            Vector2 center = Vector2.zero;
            foreach (var clusterCell in cluster.Mesh)
                center += clusterCell.Center;
            center /= cluster.Mesh.Cells.Length;

            float distance = float.MaxValue;
            Cell centerCell = null;
            for (int i = 0; i < mostCenteredCells.Length; i++)
            {
                var dist = Vector2.Distance(mostCenteredCells[i].Center, center);
                if (dist < distance)
                {
                    distance = dist;
                    centerCell = mostCenteredCells[i];
                }
            }

            switch (cluster.Type)
            {
                case ZoneType.Mountains:
                    return new Vector3[] {new Vector3(centerCell.Center.x, 20 + 10 *
                        cluster.Neighbors.Count(c => c.Type == ZoneType.Mountains), 
                        centerCell.Center.y) };          //по типу соседних кластеров
                case ZoneType.Lake:
                    return new Vector3[] { new Vector3(centerCell.Center.x, -20, centerCell.Center.y) };
                default:
                    return new Vector3[] { new Vector3(centerCell.Center.x,
                        cluster.Neighbors.Where(c => c.Type == ZoneType.Mountains).Take(3).Count() * 5,
                        centerCell.Center.y) };
            }
        }

        private Vector3[] GetZoneHeights(ClusterInfo cluster, CellMesh mesh, ZoneInfo[] zones)
        {
            //Get most centered cell of cluster
            var borders = cluster.Mesh.GetBorderCells().ToArray();

            if (cluster.Type == ZoneType.Mountains)
            {
                var heights = new List<Vector3>();

                var floodFiller = cluster.Mesh.FloodFill(borders);
                var neigh1 = floodFiller.GetNeighbors(1).ToArray();
                var neigh2 = floodFiller.GetNeighbors(2).ToArray();
                var neigh3 = floodFiller.GetNeighbors(3).ToArray();
                var neigh4 = floodFiller.GetNeighbors(4).ToArray();
                //todo add support for any possible neighbor step

                foreach (var cell in borders)
                {
                    //Produce deep canyons between some mountains
                    if (
                        cell.Neighbors.Any(
                            c => zones[c.Id].Type == ZoneType.Mountains && zones[c.Id].ClusterId > cluster.Id))
                        heights.Add(new Vector3(cell.Center.x, 10, cell.Center.y));
                    else
                    {
                        var heightVariance = Random.value * 10 + 10;
                        heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                    }
                }

                foreach (var cell in neigh1)
                {
                    var heightVariance = Random.value * 10 + 30; 
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                foreach (var cell in neigh2)
                {
                    var heightVariance = Random.value * 10 + 45; 
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                foreach (var cell in neigh3)
                {
                    var heightVariance = Random.value * 10 + 60; 
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                foreach (var cell in neigh4)
                {
                    var heightVariance = Random.value * 10 + 75; 
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                return heights.ToArray();
            }
            else if (cluster.Type == ZoneType.Lake)
            {
                var heights = new List<Vector3>();

                var floodFiller = cluster.Mesh.FloodFill(borders);
                var neigh1 = floodFiller.GetNeighbors(1).ToArray();
                var neigh2 = floodFiller.GetNeighbors(2).ToArray();
                var neigh3 = floodFiller.GetNeighbors(3).ToArray();
                var neigh4 = floodFiller.GetNeighbors(4).ToArray();

                foreach (var cell in borders)
                {
                    //Produce deep lake bottom between lakes
                    if (cell.Neighbors.Any(c => zones[c.Id].Type == ZoneType.Lake))
                        heights.Add(new Vector3(cell.Center.x, -5, cell.Center.y));
                    else
                    {
                        var heightVariance = 0;
                        heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                    }
                }

                foreach (var cell in neigh1)
                {
                    var heightVariance = -5;
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                foreach (var cell in neigh2)
                {
                    var heightVariance = -10;
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                foreach (var cell in neigh3)
                {
                    var heightVariance = -15;
                    heights.Add(new Vector3(cell.Center.x, heightVariance, cell.Center.y));
                }

                return heights.ToArray();
            }
            else        //Plains
            {
                var heights = new List<Vector3>();

                var nearMountainCells = cluster.Mesh.Cells.Where(c => c.Neighbors.Any(c1 => zones[c1.Id].Type == ZoneType.Mountains)).ToArray();
                var fill = cluster.Mesh.FloodFill(nearMountainCells);
                var nearMountainCells2 = fill.GetNeighbors(1).ToArray();

                foreach (var cell in cluster.Mesh.Cells)
                {
                    if(nearMountainCells.Contains(cell))
                        heights.Add(new Vector3(cell.Center.x, 5, cell.Center.y));
                    else if(nearMountainCells2.Contains(cell))
                        heights.Add(new Vector3(cell.Center.x, 2.5f, cell.Center.y));
                    else
                        heights.Add(new Vector3(cell.Center.x, 0, cell.Center.y));
                }
                
                return heights.ToArray();
            }
        }
    }
}
