using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Voronoi;

namespace Assets.Code.Layout
{
    public class LandLayout
    {
        public Bounds2i Bounds { get; private set; }

        public IEnumerable<Zone> Zones { get; private set; }

        public LandLayout(Bounds2i bounds, params Zone[] zones)
        {
            Bounds = bounds;
            Zones = zones;

            var cells = CellMeshGenerator.Generate(zones.Select(z => z.Center), bounds);
            for (int i = 0; i < zones.Length; i++)
                zones[i].Init(cells[i]);
        }
    }
}
