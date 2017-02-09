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

        protected override ClusterInfo[] SetClusters(CellMesh mesh, LandSettings settings)
        {
            //Divide cells to two sides (vertically)
            var center = mesh.Cells.Select(c => c.Center.x).Average();
            var leftSideZone = settings.Zones.ElementAt(0);
            var rightSideZone = settings.Zones.ElementAt(1);

            var cluster1Zones = new List<ZoneInfo>();
            var cluster2Zones = new List<ZoneInfo>();
            for (int i = 0; i < mesh.Cells.Length; i++)
            {
                if (mesh[i].Center.x < center)
                    cluster1Zones.Add(new ZoneInfo {Id = i, Type = leftSideZone.Type, ClusterId = 1});
                else
                    cluster2Zones.Add(new ZoneInfo {Id = i, Type = rightSideZone.Type, ClusterId = 2 });
            }

            return new ClusterInfo[2]
            {
                new ClusterInfo() {Id = 1, Zones = cluster1Zones.ToArray()},
                new ClusterInfo() {Id = 2, Zones = cluster2Zones.ToArray()},
            };
        }

        private IEnumerable<ZoneType> GetNeighborsOf(Cell cell, ZoneInfo[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id].Type);
        }
    }
}
