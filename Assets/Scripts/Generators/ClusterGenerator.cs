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
        public ClusterInfo Info { get; private set; }
        public ClusterLayout Layout { get; private set; }

        /// <summary>
        /// Fill some cells as cluster
        /// </summary>
        /// <param name="start"></param>
        /// <param name="allZones">All land zones</param>
        /// <returns>Occupied zones</returns>
        public virtual ClusterInfo FillCluster(Mesh<ZoneLayout>.Face start)
        {
            var clusterSize = _settings.ClusterSize.GetRandomRange();
            var neighbors = _allMesh.GetNeighbors(start, c => c.Data.Type == ClusterType.Empty).Take(clusterSize).ToArray();
            _mesh = new Mesh<ZoneLayout>.Submesh(_allMesh, neighbors);

            /*
            var zonesInCluster = new ZoneLayout[neighbors.Length];
            for (var i = 0; i < neighbors.Length; i++)
            {
                var zoneInfo = new ZoneLayout { Id = neighbors[i].Id, ClusterId = _clusterId, Type = DefaultZoneType};
                neighbors[i].Data = zoneInfo;
                zonesInCluster[i] = zoneInfo;
            }
            Info = new ClusterInfo {Id = _clusterId, Center = start, Mesh = _mesh, Type = DefaultZoneType, Zones = zonesInCluster, };

            return Info;
            */
            return null;
        }

        /// <summary>
        /// Fill given cells as cluster. Mostly debug method
        /// </summary>
        /// <param name="faces"></param>
        /// <returns>Occupied zones</returns>
        public virtual ClusterInfo FillCluster(Mesh<ZoneInfo>.Face[] faces)
        {
            //_mesh = new Mesh<ZoneInfo>.Submesh(_allMesh, cells);

            var zonesInCluster = new ZoneInfo[faces.Length];
            for (var i = 0; i < zonesInCluster.Length; i++)
            {
                var zoneInfo = new ZoneInfo { Id = faces[i].Id, ClusterId = _clusterId, Type = DefaultZoneType };
                faces[i].Data = zoneInfo;
                zonesInCluster[i] = zoneInfo;
            }
            //Info = new ClusterInfo { Id = _clusterId, Center = cells[0], Mesh = _mesh, Type = DefaultZoneType, Zones = zonesInCluster, };

            //return Info;
            return null;
        }

        /*
        /// <summary>
        /// Call after <see cref="FillCluster"/>
        /// </summary>
        public virtual void GenerateZones()
        {
            //Add some zone features...
            for (var i = 0; i < Info.Zones.Length; i++)
            {
                var zone = Info.Zones[i];
                zone.Type = DefaultZoneType;
                Info.Zones[i] = zone;
            }
        }
        */

        /// <summary>
        /// Generate heights for cluster zones
        /// </summary>
        public virtual void GenerateHeights()
        {
            foreach (var zoneLayout in Layout.Zones)
            {
                //zoneLayout.Height = 0;
            }
        }

        /// <summary>
        /// Get height for cluster-scale (global) height calculation
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetClusterHeight()
        {
            //var centerCell = GetCenterCell();
            //return new Vector3(centerCell.Center.x, 0, centerCell.Center.y);   
            return default(Vector3);
        }

        /// <summary>
        /// Get heights for zone-scale (middle) height calculation
        /// </summary>
        /// <returns></returns>
        public virtual Vector3[] GetZoneHeights()
        {
            //return _mesh.Select(c => new Vector3(c.Center.x, 0, c.Center.y)).ToArray();
            return default(Vector3[]); 
        }

        
        public ClusterLayout CreateClusterLayout(LandLayout land, Mesh<ZoneLayout> allZones, ClusterInfo clusterInfo, Graph<ClusterLayout>.Node node)
        {
            var clusterIds = clusterInfo.Zones.Select(z => z.Id).ToArray();
            var clusterZones = allZones.Where(zl => Array.FindIndex(clusterIds, id => id == zl.Id) > -1);
            var clusterMesh = new Mesh<ZoneLayout>.Submesh(allZones, clusterZones);
            foreach (var clusterCell in clusterMesh)
            {
                clusterCell.Data = new ZoneLayout(clusterInfo.Mesh[clusterCell.Id].Data, clusterCell, null);
            }

            var result = new ClusterLayout(Info, clusterMesh, node, land);
            result.Generator = this;
            return result;
        }
        

        protected readonly LandSettings _settings;
        private readonly Mesh<ZoneLayout> _allMesh;
        private Mesh<ZoneLayout>.Submesh _mesh;
        private readonly int _clusterId;
        protected readonly ClusterType DefaultZoneType;
        private Mesh<ZoneLayout>.Face _centerFace;

        protected ClusterGenerator([NotNull] LandSettings settings, [NotNull] Mesh<ZoneLayout> mesh, int clusterId, ClusterType defaultZone)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (mesh == null) throw new ArgumentNullException("mesh");

            _settings = settings;
            _allMesh = mesh;
            _clusterId = clusterId;
            DefaultZoneType = defaultZone;
        }

        /// <summary>
        /// Most isolated cell of cluster
        /// </summary>
        /// <returns></returns>
        protected Mesh<ZoneInfo>.Face GetCenterCell()
        {
            if(Info == null)
                throw new InvalidOperationException();

            if (_centerFace == null)
            {
                //Get most centered cell of cluster
                var borders = _mesh.GetBorderCells().ToArray();
                var floodFill = _mesh.FloodFill(borders);
                var mostCenteredCells = floodFill.GetNeighbors(0).ToArray();
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
                //foreach (var clusterCell in _mesh)
                    //center += clusterCell.Center;
                //center /= _mesh.Cells.Count();

                var distance = float.MaxValue;
                for (int i = 0; i < mostCenteredCells.Length; i++)
                {
                    /*
                    var dist = Vector2.Distance(mostCenteredCells[i].Center, center);
                    if (dist < distance)
                    {
                        distance = dist;
                        _centerCell = mostCenteredCells[i];
                    }
                    */
                }
            }

            //return _centerCell;
            return null;
        }
    }
}
