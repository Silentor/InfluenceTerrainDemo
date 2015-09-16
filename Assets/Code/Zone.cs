using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Voronoi;
using UnityEngine;

namespace Assets.Code
{
    public class Zone
    {
        public readonly ZoneType Type;
        public readonly Vector2 Center;
        //public IEnumerable<Zone> Neighbours { get; private set; }

        public Zone(Cell cell, ZoneType type)
        {
            Type = type;
            Center = cell.Center;
        }

        public void Init(Land land)
        {
            _land = land;

            //Neighbours =
                //land.Zones.Where(z => z != this).OrderBy(z => Vector2.SqrMagnitude(z.Center - Center)).ToArray();
        }

        private Land _land;
    }
}
