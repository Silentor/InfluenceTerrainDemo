using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Generates <see cref="LandLayout"/> from land settings
    /// </summary>
    public abstract class LayoutGenerator
    {
        protected readonly LandSettings _settings;

        protected LayoutGenerator(LandSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Generate or update <see cref="LandLayout"/>
        /// </summary>
        /// <param name="oldLandLayout">null - generate from scratch</param>
        /// <returns></returns>
        public virtual LandLayout Generate(LandLayout oldLandLayout)
        {
            var timer = Stopwatch.StartNew();
            long pointsTime, cellmeshTime, clustersTime, updateLayoutTime;

            var bounds = _settings.LandBounds;

            timer.Start();
            var points = GeneratePoints(bounds, _settings.ZonesRange);
            pointsTime = timer.ElapsedMilliseconds;
            timer.Reset();

            timer.Start();
            var zonesMesh = MeshGenerator.Generate<ZoneInfo>(points, (Bounds) bounds);
            cellmeshTime = timer.ElapsedMilliseconds;
            timer.Reset();

            timer.Start();
            //var zones = new ZoneInfo[cellMesh.Cells.Length];
            var clusters = GenerateClusters(zonesMesh, _settings);
            clustersTime = timer.ElapsedMilliseconds;
            timer.Reset();

            LandLayout result = new LandLayout(_settings, null, null);

            //Create land layout
            /*
            var zones = zonesMesh.Convert<ZoneLayout>();
            var clusters2 = clusters.Convert<ClusterLayout>();
            foreach (var cluster in clusters)
            {
                clusters2[cluster.Id].Data =
                    cluster.Data.Generator.CreateClusterLayout(result, zones, cluster.Data, clusters2[cluster.Id]);
            }
            */

            //foreach (var cluster in clusters2)
            //cluster.Data.Generator.GenerateZones();

            //foreach (var cluster in clusters2)
                //cluster.Data.Generator.GenerateHeights();

            timer.Start();

            if (oldLandLayout == null)
            {
                //result = new LandLayout(_settings, zonesMesh, clusters);
            }
            else
            {
                //oldLandLayout.Update(zonesMesh, clusters);
                result = oldLandLayout;
            }

            updateLayoutTime = timer.ElapsedMilliseconds;
            timer.Stop();

            UnityEngine.Debug.LogFormat(
                "Generated layout: generate points {0} msec, create cellmesh {1} msec, set clusters {2} msec, update land layout {3} msec, total {4} msec",
                pointsTime, cellmeshTime, clustersTime, updateLayoutTime,
                pointsTime + cellmeshTime + clustersTime + updateLayoutTime);

            return result;
        }

        #region Generate points

        /// <summary>
        /// Generate zone points
        /// </summary>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected Vector2[] GeneratePoints(Bounds2i landBounds, Vector2 density)
        {
            return GeneratePoissonPoints(landBounds, density);
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected Vector2[] GeneratePoissonPoints(Bounds2i landBounds, Vector2 density)
        {
            const int maxPointsCount = 1000;
            var checkedPoints = new List<Vector2>();
            var uncheckedPoints = new List<Vector2>();

            //Generate start point (near bounds center)
            var center = (landBounds.Max + landBounds.Min) / 2;
            var zoneCenterX = Random.Range(center.X - density.y / 2, center.X + density.y / 2);
            var zoneCenterY = Random.Range(center.Z - density.y / 2, center.Z + density.y / 2);
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
                    var a = Random.Range(0, 2 * Mathf.PI);
                    var newPoint = processedPoint + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));

                    if (landBounds.Contains((Vector2i) newPoint))
                    {
                        if (checkedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x)
                            && uncheckedPoints.TrueForAll(p =>
                                Vector2.SqrMagnitude(p - newPoint) > density.x * density.x))
                            uncheckedPoints.Add(newPoint);
                    }
                }

                checkedPoints.Add(processedPoint);
                if (checkedPoints.Count >= maxPointsCount)
                    break;
            }

            return checkedPoints.ToArray();
        }

        #endregion

        #region Generate clusters

        protected Graph<ClusterInfo> GenerateClusters(Mesh<ZoneInfo> mesh, LandSettings settings)
        {
            return GeneratePoissonClusters(mesh, settings);
        }

        protected Graph<ClusterInfo> GeneratePoissonClusters(Mesh<ZoneInfo> mesh, LandSettings settings)
        {
            var clusterTypes = settings.Clusters.Select(z => z.Type).ToArray();
            var clusterId = -1;
            var generators = new List<ClusterGenerator>();

            //Get clusters Poisson points
            var clusterCenters = GeneratePoints(settings.LandBounds, new Vector2(100, 100));        //Вынести в настройки

            //Fill main clusters
            for (int i = 0; i < clusterCenters.Length; i++)
            {
                /*
                var startCell = mesh.GetCellFor(clusterCenters[i]);
                if (startCell.Data.Type == ClusterType.Empty)
                {
                    clusterId++;
                    var zoneType = clusterTypes[Random.Range(0, clusterTypes.Length)];
                    var generator = CreateGenerator(zoneType, mesh, clusterId);
                    generators.Add(generator);
                    //generator.FillCluster(startCell);
                }
                */
            }

            UnityEngine.Debug.LogFormat("Main clusters: 0 - {0}", clusterId);

            //Fill remaining gaps between main clusters
            for (var i = 0; i < mesh.Nodes.Count(); i++)
            {
                if (mesh.Nodes.ElementAt(i).Data.Type == ClusterType.Empty)
                {
                    clusterId++;
                    var zoneType = clusterTypes[Random.Range(0, clusterTypes.Length)];
                    var generator = CreateGenerator(zoneType, mesh, clusterId);
                    generators.Add(generator);
                    //generator.FillCluster(mesh.Nodes.ElementAt(i));
                }
            }

            //Setup graph of clusters
            var clustersGraph = new Graph<ClusterLayout>();
            foreach (var generator in generators)
            {
                var clusterNode = clustersGraph.Add(null);
                var clusterInfo = generator.Info;
                clusterInfo.Generator = generator;
                //clusterNode.Data = clusterInfo;
            }

            /*
            foreach (var clusterNode in clustersGraph.Nodes)
            {
                //Calculate cluster neighbors
                var border = clusterNode.Data.Mesh.GetBorderCells();
                var neighborClusterIds = border.SelectMany(c => c.Neighbors)
                    .Select(c => c.Data.ClusterId)
                    .Where(cid => cid != clusterNode.Data.Id)
                    .Distinct().ToArray();

                foreach (var neighborClusterId in neighborClusterIds)
                {
                    clustersGraph.AddEdge(clusterNode, clustersGraph.Nodes.ElementAt(neighborClusterId));
                }
            }
            */

            //return clustersGraph;
            return null;
        }

        #endregion

        public enum Type
        {
            PoissonTwoSide,
            PoissonClustered
        }

        protected ClusterGenerator CreateGenerator(ClusterType type, Mesh<ZoneInfo> mesh, int clusterId)
        {
            /*
            switch (type)
            {
                default:
                    return new DefaultClusterGenerator(_settings, mesh, clusterId, type);
            }
            */

            return null;
        }
    }
}
