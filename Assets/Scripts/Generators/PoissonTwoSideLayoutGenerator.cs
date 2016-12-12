using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;

namespace TerrainDemo.Generators
{
    public class PoissonTwoSideLayoutGenerator : PoissonLayoutGenerator
    {
        public PoissonTwoSideLayoutGenerator(LandSettings settings) : base(settings)
        {
        }

        protected override ZoneInfo[] SetZoneInfo(CellMesh mesh, LandSettings settings)
        {
            //Divide cells to two sides (vertically)
            var center = mesh.Cells.Select(c => c.Center.x).Average();
            var leftSideZone = settings.Zones.ElementAt(0);
            var rightSideZone = settings.Zones.ElementAt(1);

            var result = new ZoneInfo[mesh.Cells.Length];
            for (int i = 0; i < mesh.Cells.Length; i++)
            {
                if (mesh[i].Center.x < center)
                    result[i] = new ZoneInfo {Type = leftSideZone.Type, ClusterId = 1};
                else
                    result[i] = new ZoneInfo { Type = rightSideZone.Type, ClusterId = 2 };
            }

            //Generate some interval zones
            var ordinaryZones = settings.Zones.Where(zt => !zt.IsInterval).Select(z => z.Type).ToArray();
            var intervalZone = settings.Zones.FirstOrDefault(zt => zt.IsInterval);
            if (intervalZone != null)
            {
                for (int i = 0; i < mesh.Cells.Length; i++)
                {
                    var cell = mesh[i];

                    //Place interval zone type between all ordinary zones
                    if (GetNeighborsOf(cell, result).Where(zt => ordinaryZones.Contains(zt)).Distinct().Count() > 1)
                    {
                        result[i] = new ZoneInfo() {Type = intervalZone.Type, ClusterId = 3};
                    }
                }
            }

            return result;
        }

        private IEnumerable<ZoneType> GetNeighborsOf(Cell cell, ZoneInfo[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id].Type);
        }
    }
}
