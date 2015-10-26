using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Voronoi;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Code.Layout
{
    /// <summary>
    /// Desciption of Zone layout
    /// </summary>
    public struct ZoneLayout
    {
        public readonly Vector2 Center;
        public readonly ZoneType Type;
        public readonly Cell Cell;

        /// <summary>
        /// World bounds
        /// </summary>
        public readonly Bounds2i Bounds;

        public readonly Bounds2i ChunkBounds;

        public IEnumerable<ZoneLayout> Neighbors { get { return _neighbors; } }

        public static readonly IEqualityComparer<ZoneLayout> TypeComparer = new ZoneTypeComparer();

        public ZoneLayout(ZoneType type, Cell cell)
        {
            Cell = cell;
            Center = cell.Center;
            Type = type;
            Bounds = (Bounds2i)cell.Bounds;
            ChunkBounds = new Bounds2i(Chunk.GetPosition(Bounds.Min), Chunk.GetPosition(Bounds.Max));
            _neighbors = new ZoneLayout[0];
        }

        private ZoneLayout[] _neighbors;

        public static bool operator ==(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell == z2.Cell;
        }

        public static bool operator !=(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell != z2.Cell;
        }

        private class ZoneTypeComparer : IEqualityComparer<ZoneLayout>
        {
            public bool Equals(ZoneLayout x, ZoneLayout y)
            {
                return x.Type == y.Type;
            }

            public int GetHashCode(ZoneLayout obj)
            {
                return (int)obj.Type;
            }
        }
    }
}
