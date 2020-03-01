using System;
using JetBrains.Annotations;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Axial coordinates in hexagonal grid. https://www.redblobgames.com/grids/hexagons/#coordinates-axial
    /// </summary>
    public struct HexPos : IEquatable<HexPos>
    {
        public readonly short Q;
        public readonly short R;

        //Basic axes
        public static readonly HexPos RPlus = new HexPos(0, 1);
        public static readonly HexPos QPlus = new HexPos(1, 0);
        public static readonly HexPos RMinus = new HexPos(0, -1);
        public static readonly HexPos QMinus = new HexPos(-1, 0);

        public static readonly HexPos Zero = new HexPos(0, 0);

        // From "right" (Q+) clockwise
		//
        //         R+
        //    / \ 
        //   |   |  Q+
        //   |   |
        //    \ /
        //      
        //


        public static readonly Vector2i[] Directions =
        {
	        ( 1, 0), (1, -1), ( 0, -1), ( -1, 0), (-1, 1), ( 0, 1),
        };

        public HexPos(int q, int r)
        {
            Q = (short)q;
            R = (short)r;
        }

        [Pure]
        public HexPos Translated(Vector2i offset)
        {
            return new HexPos( Q + offset.X, R + offset.Z );
        }

        public bool Equals(HexPos other)
        {
            return Q == other.Q && R == other.R;
        }

        public override int GetHashCode() => (ushort)Q | ((ushort)R << 16);

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

        //public static HexPos operator +(HexPos left, HexPos right)
        //{
        //    return new HexPos(left.Q + right.Q, left.R + right.R);
        //}

        public override string ToString() => $"<{Q}, {R}>";
    }
}
