using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using UnityEngine;

namespace Assets.Code
{
    public class Zone
    {
        public readonly ZoneType Type;
        public readonly Vector2 Center;
        public IEnumerable<Zone> Neighbours { get; private set; }

        public Zone(Vector2 center, ZoneType type)
        {
            Type = type;
            Center = center;
        }

        public void Init(Land land)
        {
            _land = land;

            Neighbours =
                land.Zones.Where(z => z != this).OrderBy(z => Vector2.SqrMagnitude(z.Center - Center)).ToArray();
        }

        private Land _land;
    }
}
