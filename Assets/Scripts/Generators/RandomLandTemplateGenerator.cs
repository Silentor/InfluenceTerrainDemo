using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Create random clusters at random (uniform or poisson) locations
    /// </summary>
    public class RandomLandTemplateGenerator
    {
        public IEnumerable<ClusterLayout> GenerateClusters(Mesh<ZoneLayout> landMesh, LandSettings settings)
        {
            var clusterTypes = settings.Clusters.Select(z => z.Type).ToArray();
            var clusterId = -1;
            var generators = new List<ClusterGenerator>();

            //Get clusters Poisson points
            var clusterCenters = Poisson.GeneratePoints((Bounds)settings.LandBounds, settings.ClustersDensity);

            //Fill main clusters
            for (int i = 0; i < clusterCenters.Length; i++)
            {
                //var startCell = landMesh.GetCellFor(clusterCenters[i]);
                //if (startCell.Data == null)
                {
                    clusterId++;
                    var clusterType = clusterTypes[Random.Range(0, clusterTypes.Length)];
                    /*
                    var generator = CreateGenerator(settings, clusterType, mesh, clusterId);
                    generators.Add(generator);
                    generator.FillCluster(startCell);
                    */
                }
            }

            UnityEngine.Debug.LogFormat("Main clusters: 0 - {0}", clusterId);

            /*
            //Fill remaining gaps between main clusters
            for (var i = 0; i < mesh.Nodes.Count(); i++)
            {
                if (mesh.Nodes.ElementAt(i).Data.Type == ClusterType.Empty)
                {
                    clusterId++;
                    var zoneType = clusterTypes[Random.Range(0, clusterTypes.Length)];
                    var generator = CreateGenerator(zoneType, mesh, clusterId);
                    generators.Add(generator);
                    generator.FillCluster(mesh.Nodes.ElementAt(i));
                }
            }
            */

            return null;
        }

        protected ClusterGenerator CreateGenerator(LandSettings settings, ClusterType type, Mesh<ZoneLayout> mesh, int clusterId)
        {
            switch (type)
            {
                default:
                    return new DefaultClusterGenerator(settings, mesh, clusterId, type);
            }
        }
    }
}
