using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Voronoi;

namespace Assets.Code.Layout
{
    /// <summary>
    /// Geometrical representaton of land
    /// </summary>
    public class LandLayout
    {
        public Bounds2i Bounds { get; private set; }

        public IEnumerable<Zone> Zones { get; private set; }

        public LandLayout(Bounds2i bounds, IEnumerable<Zone> zones)
        {
            Bounds = bounds;
            Zones = zones;
        }
    }
}
