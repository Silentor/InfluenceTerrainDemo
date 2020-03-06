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
        public static readonly Vector2i RPlus = new Vector2i(0, 1);
        public static readonly Vector2i QPlus = new Vector2i(1, 0);
        public static readonly Vector2i SPlus = new Vector2i(1, -1);
        public static readonly Vector2i RMinus = new Vector2i(0, -1);
        public static readonly Vector2i QMinus = new Vector2i(-1, 0);
        public static readonly Vector2i SMinus = new Vector2i(-1, 1);

        //Origin
        public static readonly HexPos Zero = new HexPos(0, 0);

        // From "right" (Q+) clockwise
		//
        //         R+
        //    / \ 
        //   |   |  Q+
        //   |   |
        //    \ /
        //         S+
        //


        public static readonly Vector2i[] Directions =
        {
	        QPlus, SPlus, RMinus, QMinus, SMinus, RPlus,
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

        public int Distance( HexPos to )
        {
	        var fromCube = ToCube( );
	        var toCube = to.ToCube( );
	        return Vector3i.ManhattanDistance( fromCube, toCube ) / 2;
        }
        public Vector3i ToCube( ) =>      new Vector3i( Q, R, -Q - R );

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

        public static HexPos operator +(HexPos position, Vector2i offset)
        {
	        return new HexPos((short)(position.Q + offset.X), (short)(position.R + offset.Z));
        }

        //public static HexPos operator +(HexPos left, HexPos right)
        //{
        //    return new HexPos(left.Q + right.Q, left.R + right.R);
        //}

        public override string ToString() => $"<{Q}, {R}>";
    }

    /// <summary>
    /// Clockwise
    /// </summary>
    public enum HexDir
    {
        QPlus, SPlus, RMinus, QMinus, SMinus, RPlus
    }
}
