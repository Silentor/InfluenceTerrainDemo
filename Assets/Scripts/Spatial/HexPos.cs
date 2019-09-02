using System;
using JetBrains.Annotations;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Coordinates of Macro Cell in hexagonal grid. https://www.redblobgames.com/grids/hexagons/#coordinates-axial
    /// </summary>
    public struct HexPos : IEquatable<HexPos>
    {
        public readonly short X;
        public readonly short Z;

        public static readonly HexPos ZPlus = new HexPos(0, 1);
        public static readonly HexPos YPlus = new HexPos(1, 1);
        public static readonly HexPos XPlus = new HexPos(1, 0);
        public static readonly HexPos ZMinus = new HexPos(0, -1);
        public static readonly HexPos YMinus = new HexPos(-1, -1);
        public static readonly HexPos XMinus = new HexPos(-1, 0);

        //From cell "top" clockwise
        //      Z+
        // X-  ---  Y+ 
        //    /   \
        //    \   /
        // Y-  ---  X+ 
        //      Z-
        public static readonly HexPos[] Directions =
        {
            ZPlus, YPlus, XPlus, ZMinus, YMinus, XMinus,
        };

        public HexPos(int x, int z)
        {
            X = (short)x;
            Z = (short)z;
        }

        [Pure]
        public HexPos Translated(HexPos offset)
        {
            return new HexPos(X + offset.X, Z + offset.Z);
        }

        public bool Equals(HexPos other)
        {
            return X == other.X && Z == other.Z;
        }

        public override int GetHashCode() => X | (Z << 16);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is HexPos coord && Equals(coord);
        }

        public static bool operator ==(HexPos left, HexPos right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexPos left, HexPos right)
        {
            return !left.Equals(right);
        }

        public static HexPos operator +(HexPos left, HexPos right)
        {
            return new HexPos(left.X + right.X, left.Z + right.Z);
        }

        public override string ToString() => $"<{X}, {Z}>";

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
