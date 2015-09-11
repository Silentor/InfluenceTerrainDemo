using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Assets.Code.Layout
{
    /// <summary>
    /// Makes layout for land
    /// </summary>
    public class LandLayouter
    {
        public Land CreateLand(ILandSettings settings)
        {
            var zones = new List<Zone>();
            var landBoundsBlocks = settings.LandSizeChunks*settings.ChunkSize;
            var zoneTypes = settings.ZoneTypes.Select(z => z.Type).ToArray();

            var retryCount = 0;

            for (var i = 0; i < settings.ZonesCount; i++)
            {
                var zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
                var zonePosition = new Vector2(Random.Range(landBoundsBlocks.Min.X, landBoundsBlocks.Max.X), Random.Range(landBoundsBlocks.Min.Z, landBoundsBlocks.Max.Z));

                //Discard creating zone center near existing zone
                if (zones.Any(z => Vector2.Distance(z.Center, zonePosition) < settings.ChunkSize * 4))
                {
                    if (retryCount++ < 5)
                    {
                        i--;
                        continue;
                    }
                    else
                        break;
                }

                zones.Add(new Zone(zonePosition, zoneType));
            }

            var land = new Land(zones, settings);
            return land;
        }
    }
}
