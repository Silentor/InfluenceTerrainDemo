using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;

namespace TerrainDemo.Generators
{
    public class PoissonTwoSideLayoutGenerator : PoissonLayoutGenerator
    {
        public PoissonTwoSideLayoutGenerator(LandSettings settings) : base(settings)
        {
        }

        protected Graph<ClusterGenerator> GenerateClusters(Mesh<ZoneInfo> mesh, LandSettings settings)
        {
            //Divide cells to two sides (vertically)
            /*
            var center = mesh.Nodes.Select(c => c.Center.x).Average();
            var leftSideCluster = settings.Clusters.ElementAt(0);
            var rightSideCluster = settings.Clusters.ElementAt(1);

            var leftCells = new List<Mesh<ZoneInfo>.Cell>();
            var rightCells = new List<Mesh<ZoneInfo>.Cell>();
            for (int i = 0; i < mesh.Nodes.Count(); i++)
            {
                if (mesh[i].Center.x < center)
                    leftCells.Add(mesh[i]);
                else
                    rightCells.Add(mesh[i]);
            }
            var leftGenerator = CreateGenerator(leftSideCluster.Type, mesh, 0);
            leftGenerator.FillCluster(leftCells.ToArray());
            var rightGenerator = CreateGenerator(rightSideCluster.Type, mesh, 1);
            rightGenerator.FillCluster(rightCells.ToArray());

            //Setup graph of cluster generators
            var clustersGraph = new Graph<ClusterGenerator>();
            clustersGraph.Add(leftGenerator);
            clustersGraph.Add(rightGenerator);
            clustersGraph.AddEdge(clustersGraph.Nodes.ElementAt(0), clustersGraph.Nodes.ElementAt(1));

            return clustersGraph;
            */

            return null;
        }

        private IEnumerable<ClusterType> GetNeighborsOf(Cell cell, ZoneInfo[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id].Type);
        }
    }
}
