using System;
using System.Diagnostics;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Cardinal directions on square grid
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Z+ direction
        /// </summary>
        Forward,        //Z+
        /// <summary>
        /// X+ direction
        /// </summary>
        Right,          //X+
        /// <summary>
        /// Z- direction
        /// </summary>
        Back,           //Z-
        /// <summary>
        /// X- direction
        /// </summary>
        Left,            //X-

    }

    public static class Directions
    {
        /// <summary>
        /// Cardinal directions on 2D grid
        /// </summary>
        public static readonly Direction[] Cardinal =
        {
            Direction.Forward, Direction.Right, Direction.Back, Direction.Left
        };

        /// <summary>
        /// Directions on grid as Vector2i
        /// </summary>
        public static readonly Vector2i[] Vector2I =
        {
            Vector2i.Forward, Vector2i.Right, Vector2i.Back, Vector2i.Left,
        };

        /// <summary>
        /// Directions on grid as Vector2
        /// </summary>
        public static readonly Vector2[] Vector2 =
        {
            OpenToolkit.Mathematics.Vector2.UnitY, OpenToolkit.Mathematics.Vector2.UnitX, -OpenToolkit.Mathematics.Vector2.UnitY, -OpenToolkit.Mathematics.Vector2.UnitX,
        };

        public static readonly (Vector2, Vector2)[] BlockSide =
        {
            (new Vector2(0, 1), new Vector2(1, 1)),
            (new Vector2(1, 1), new Vector2(1, 0)),
            (new Vector2(1, 0), new Vector2(0, 0)),
            (new Vector2(0, 0), new Vector2(0, 1)),
        };

        public static Vector2 ToVector2(this Direction direction)
        {
	        return Vector2[(int) direction];
        }

        public static Vector2i ToVector2i(this Direction direction)
        {
	        return Vector2I[(int) direction];
        }

        public static bool IsPerpendicular(this Direction first, Direction second)
        {
	        return (((int)first ^ (int)second) & 0x01) == 0x01;
        }

        public static Direction Inverse( this Direction direction )
        {
	        switch ( direction )
	        {
		        case Direction.Forward: return Direction.Back;
		        case Direction.Right: return Direction.Left;
		        case Direction.Back: return Direction.Forward;
		        case Direction.Left: return Direction.Right;
		        default:
			        throw new ArgumentOutOfRangeException( nameof( direction ), direction, null );
	        }
        }

        public static Direction From( Vector2 direction )
        {
            //todo consider zero direction
	        if ( direction == OpenToolkit.Mathematics.Vector2.Zero )
		        return Direction.Forward;

	        var x = direction.X;
	        var y = direction.Y;

	        if ( y >= 0 && (x >= 0 ? x <= y : -x < y) )
		        return Direction.Forward;
            else if ( y < 0 && (x >= 0 ? x < -y : -x <= -y) )
		        return Direction.Back;
            else if ( x >= 0 && (y >= 0 ? y < x : -y <= x) )
		        return Direction.Right;
	        else if ( x < 0 && (y >= 0 ? y <= -x : -y < -x) )
		        return Direction.Left;

	        throw new InvalidOperationException("Algorithm failed");
        }
    }

}
