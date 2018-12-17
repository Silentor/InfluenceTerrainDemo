using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Coordinates of Macro Cell in hexagonal grid. https://www.redblobgames.com/grids/hexagons/#coordinates-axial
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Coord : IEquatable<Coord>
    {
        public readonly short X;
        public readonly short Z;

        public static readonly Coord ZPlus = new Coord(0, 1);
        public static readonly Coord YPlus = new Coord(1, 1);
        public static readonly Coord XPlus = new Coord(1, 0);
        public static readonly Coord ZMinus = new Coord(0, -1);
        public static readonly Coord YMinus = new Coord(-1, -1);
        public static readonly Coord XMinus = new Coord(-1, 0);

        //From cell "top" clockwise
        //      Z+
        // X-  ---  Y+ 
        //    /   \
        //    \   /
        // Y-  ---  X+ 
        //      Z-
        public static readonly Coord[] Directions =
        {
            ZPlus, YPlus, XPlus, ZMinus, YMinus, XMinus,
        };

        public Coord(int x, int z)
        {
            X = (short)x;
            Z = (short)z;
        }

        [Pure]
        public Coord Translated(Coord offset)
        {
            return new Coord(X + offset.X, Z + offset.Z);
        }

        public bool Equals(Coord other)
        {
            return X == other.X && Z == other.Z;
        }

        public override int GetHashCode()
        {
            return X | (Z << 16);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Coord coord && Equals(coord);
        }

        public static bool operator ==(Coord left, Coord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Coord left, Coord right)
        {
            return !left.Equals(right);
        }

        public static Coord operator +(Coord left, Coord right)
        {
            return new Coord(left.X + right.X, left.Z + right.Z);
        }

        public override string ToString() => $"({X}; {Z})";

        public enum Sides
        {
            ZPositive = 0,
            YPositive,
            XPositive,
            ZNegative,
            YNegative,
            XNegative
        }
    }
}
