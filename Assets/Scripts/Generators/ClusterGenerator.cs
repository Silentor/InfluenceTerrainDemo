using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Creates layout and blockmap for cluster
    /// </summary>
    public abstract class ClusterGenerator
    {
        /// <summary>
        /// Fill some cells as cluster
        /// </summary>
        /// <param name="start"></param>
        /// <param name="zones"></param>
        /// <returns></returns>
        public virtual ClusterInfo FillCluster(Cell start, ZoneInfo[] zones)
        {
            var clusterSize = _settings.ClusterSize.GetRandomRange();
            var neighbors = _mesh.GetNeighbors(start, c => zones[c.Id].Type == ZoneType.Empty).Take(clusterSize).ToArray();
            var clusterCells = new List<Cell>(neighbors.Length + 1);
            clusterCells.Add(start);
            clusterCells.AddRange(neighbors);

            var zonesInCluster = new List<ZoneInfo>(clusterCells.Count);
            foreach (var cell in clusterCells)
            {
                var zoneInfo = new ZoneInfo { Id = cell.Id, ClusterId = _clusterId, Type = DefaultZoneType };
                zones[cell.Id] = zoneInfo;
                zonesInCluster.Add(zoneInfo);
            }

            _cluster = new ClusterInfo
            {
                Id = _clusterId,
                Type = DefaultZoneType,
                Zones = zonesInCluster.ToArray(),
                Mesh = new CellMesh.Submesh(_mesh, clusterCells.ToArray()),
            };

            return _cluster;
        }

        /// <summary>
        /// Get height for cluster-scale (global) height calculation
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetClusterHeight()
        {
            var centerCell = GetCenterCell();
            return new Vector3(centerCell.Center.x, 0, centerCell.Center.y);   
        }

        /// <summary>
        /// Get heights for zone-scale (middle) height calculation
        /// </summary>
        /// <returns></returns>
        public virtual Vector3[] GetZoneHeights()
        {
            return _cluster.Mesh.Select(c => new Vector3(c.Center.x, 0, c.Center.y)).ToArray();
        }

        protected readonly LandSettings _settings;
        private readonly CellMesh _mesh;
        private readonly int _clusterId;
        protected readonly ZoneType DefaultZoneType;
        private ClusterInfo _cluster;
        private Cell _centerCell;

        protected ClusterGenerator([NotNull] LandSettings settings, [NotNull] CellMesh mesh, int clusterId, ZoneType defaultZone)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (mesh == null) throw new ArgumentNullException("mesh");

            _settings = settings;
            _mesh = mesh;
            _clusterId = clusterId;
            DefaultZoneType = defaultZone;
        }

        /// <summary>
        /// Most isolated cell of cluster
        /// </summary>
        /// <returns></returns>
        protected Cell GetCenterCell()
        {
            if(_cluster == null)
                throw new InvalidOperationException();

            if (_centerCell == null)
            {
                //Get most centered cell of cluster
                var borders = _cluster.Mesh.GetBorderCells().ToArray();
                var floodFill = _cluster.Mesh.FloodFill(borders);
                Cell[] mostCenteredCells = floodFill.GetNeighbors(0).ToArray();
                for (int floodFillStep = 1; floodFillStep < 10; floodFillStep++)
                {
                    var floodFillResult = floodFill.GetNeighbors(floodFillStep).ToArray();
                    if (!floodFillResult.Any())
                        break;
                    else
                        mostCenteredCells = floodFillResult;
                }

                //Find most centered cell by geometrical center distance;
                var center = Vector2.zero;
                foreach (var clusterCell in _cluster.Mesh)
                    center += clusterCell.Center;
                center /= _cluster.Mesh.Cells.Length;

                var distance = float.MaxValue;
                for (int i = 0; i < mostCenteredCells.Length; i++)
                {
                    var dist = Vector2.Distance(mostCenteredCells[i].Center, center);
                    if (dist < distance)
                    {
                        distance = dist;
                        _centerCell = mostCenteredCells[i];
                    }
                }
            }

            return _centerCell;
        }
    }
}
