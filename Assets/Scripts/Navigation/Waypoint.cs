using System;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
    public readonly struct Waypoint : IEquatable<Waypoint>
    {
        public readonly BaseBlockMap Map;
        public readonly GridPos Position;

        public static readonly Waypoint Empty = new Waypoint();

        public Waypoint([NotNull] BaseBlockMap map, GridPos position)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Position = position;
        }

        public Vector3 GetPosition()
        {
            var centerPosition = BlockInfo.GetWorldCenter(Position);
            return new Vector3(centerPosition.X, Map.GetHeight(centerPosition), centerPosition.Y);
        }

        public override string ToString()
        {
            return $"{Map.Name}-{Position}";
        }


        public bool Equals(Waypoint other)
        {
            return Map.Equals(other.Map) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
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
