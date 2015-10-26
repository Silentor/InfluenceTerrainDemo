using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Settings;
using Assets.Code.Voronoi;
using UnityEngine;


namespace Assets.Code.Layout
{
    /// <summary>
    /// Makes simple pure random layout for land
    /// </summary>
    public class RandomLayout : LandLayout
    {
        public RandomLayout(ILandSettings settings) : base(settings)
        {
        }

        protected override ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings)
        {
            var zones = new ZoneType[cells.Length];
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            for (var i = 0; i < zones.Length; i++)
            {
                ZoneType zoneType;
                //if (cell.IsClosed)
                zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                //else
                //zoneType = ZoneType.Empty;

                zones[i] = zoneType;
            }
            
            return zones;
        }
    }
}
