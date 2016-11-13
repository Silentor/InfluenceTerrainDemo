using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;

namespace TerrainDemo.Generators
{
    public class PoissonTwoSideLayoutGenerator : PoissonLayoutGenerator
    {
        public PoissonTwoSideLayoutGenerator(ILandSettings settings) : base(settings)
        {
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            //Divide cells to two sides (vertically)
            var center = cells.Select(c => c.Center.x).Average();
            var leftSideZone = settings.ZoneTypes.ElementAt(0);
            var rightSideZone = settings.ZoneTypes.ElementAt(1);

            var result = new ZoneType[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Center.x < center)
                    result[i] = leftSideZone.Type;
                else
                    result[i] = rightSideZone.Type;
            }

            //Generate some interval zones
            var ordinaryZones = settings.ZoneTypes.Where(zt => !zt.IsInterval).Select(z => z.Type).ToArray();
            var intervalZone = settings.ZoneTypes.FirstOrDefault(zt => zt.IsInterval);
            if (intervalZone != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    var cell = cells[i];

                    //Place interval zone type between all ordinary zones
                    if (GetNeighborsOf(cell, result).Where(zt => ordinaryZones.Contains(zt)).Distinct().Count() > 1)
                    {
                        result[i] = intervalZone.Type;
                    }
                }
            }

            return result;
        }

        private IEnumerable<ZoneType> GetNeighborsOf(Cell cell, ZoneType[] zones)
        {
            return cell.Neighbors.Select(c => zones[c.Id]);
        }
    }
}
