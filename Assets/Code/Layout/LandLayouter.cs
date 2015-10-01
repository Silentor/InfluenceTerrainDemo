using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Settings;
using Assets.Code.Voronoi;
using UnityEngine;


namespace Assets.Code.Layout
{
    /// <summary>
    /// Makes layout for land
    /// </summary>
    public class LandLayouter
    {
        public IEnumerable<Zone> CreateLayout(IEnumerable<Cell> cells, ILandSettings settings)
        {
            var zones = new List<Zone>();
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            foreach (var cell in cells)
            {
                ZoneType zoneType;
                //if (cell.IsClosed)
                    zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                //else
                    //zoneType = ZoneType.Empty;

                zones.Add(new Zone(cell, zoneType));
            }

            return zones;
        }
    }
}
