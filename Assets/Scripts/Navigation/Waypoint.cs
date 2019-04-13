using System;
using JetBrains.Annotations;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
    public readonly struct Waypoint : IEquatable<Waypoint>
    {
        public readonly BaseBlockMap Map;
        public readonly Vector2i Position;

        public static readonly Waypoint Empty = new Waypoint();

        public Waypoint([NotNull] BaseBlockMap map, Vector2i position)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Position = position;
        }

        public bool Equals(Waypoint other)
        {
            return Equals(Map, other.Map) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Waypoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Map != null ? Map.GetHashCode() : 0) * 397) ^ Position.GetHashCode();
            }
        }

        public static bool operator ==(Waypoint left, Waypoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Waypoint left, Waypoint right)
        {
            return !left.Equals(right);
        }
    }
}
