using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Zone
    {
        public readonly int Id;
        public readonly Vector2 Position;

        public Zone(Vector2 position, int id)
        {
            Id = id;
            Position = position;
        }
    }
}
