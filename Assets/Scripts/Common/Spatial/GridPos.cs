using System;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Rectangular grid position
	/// </summary>
	public struct GridPos : IEquatable<GridPos>
	{  
		public readonly short X;
		public readonly short Z;

		public static readonly GridPos Min = new GridPos(short.MinValue, short.MinValue);
		public static readonly GridPos Max = new GridPos(short.MaxValue, short.MaxValue);
		public static readonly GridPos Zero = new GridPos(0, 0);

		public GridPos( short x, short z )
		{
			X = x;
			Z = z;
		}
		public GridPos(short xz)
		{
			X = xz;
			Z = xz;
		}

		public GridPos(int x, int z)
		{
#if DEVELOPMENT_BUILD
			checked
#endif
			{
				X = (short)x;
				Z = (short)z;
			}
		}
		public GridPos(int xz)
		{
#if DEVELOPMENT_BUILD
			checked
#endif
			{
				X = (short)xz;
				Z = (short)xz;
			}
		}
		public GridPos(double x, double z)
		{
#if DEVELOPMENT_BUILD
			checked
#endif
			{
				X = (short) Math.Floor( x );
				Z = (short) Math.Floor( z );
			}
		}

		#region Public operators

		public static Vector2i operator -( GridPos first, GridPos second )
		{
			return new Vector2i(first.X - second.X, first.Z - second.Z);
		}

		public static GridPos operator +(GridPos position, Vector2i offset)
		{
			return new GridPos((short)(position.X + offset.X), (short)(position.Z + offset.Z));
		}

		public static GridPos operator -(GridPos position, Vector2i offset)
		{
			return new GridPos((short)(position.X - offset.X), (short)(position.Z - offset.Z));
		}


#endregion


#region IEquatable

		public bool Equals( GridPos other )
		{
			return X == other.X && Z == other.Z;
		}
		public override bool Equals( object obj )
		{
			return obj is GridPos other && Equals( other );
		}
		public override int GetHashCode( )
		{
			unchecked
			{
				return ( (ushort)X << 16 | (ushort)Z);
			}
		}
		public static bool operator ==( GridPos left, GridPos right )
		{
			return left.Equals( right );
		}
		public static bool operator !=( GridPos left, GridPos right )
		{
			return !left.Equals( right );
		}

#endregion

#region Typecasts

		public static explicit operator GridPos(Vector2 v)
		{
			return new GridPos((short)Math.Floor(v.X), (short)Math.Floor(v.Y));
		}

		public static explicit operator GridPos(UnityEngine.Vector2 v)
		{
			return new GridPos((short)Math.Floor(v.x), (short)Math.Floor(v.y));
		}

		public static explicit operator GridPos(Vector3 v)
		{
			return new GridPos((short)Math.Floor(v.X), (short)Math.Floor(v.Z));
		}

		public static explicit operator GridPos(UnityEngine.Vector3 v)
		{
			return new GridPos((short)Math.Floor(v.x), (short)Math.Floor(v.z));
		}

		public static implicit operator Vector2(GridPos v)
		{
			return new Vector2(v.X, v.Z);
		}

		public static implicit operator UnityEngine.Vector2(GridPos v)
		{
			return new UnityEngine.Vector2(v.X, v.Z);
		}

		public static implicit operator UnityEngine.Vector3(GridPos v)
		{
			return new UnityEngine.Vector3(v.X, 0, v.Z);
		}
		public static implicit operator (short x, short z) (GridPos v)
		{
			return (v.X, v.Z);
		}

		public static implicit operator GridPos((short, short) v)
		{
			return new GridPos(v.Item1, v.Item2);
		}

		public static implicit operator Bound2i( GridPos position )
		{
			return new Bound2i(position, 1, 1);
		}

#endregion

		public static float Distance(GridPos a, GridPos b)
		{
			var dx = b.X              - a.X;
			var dz = b.Z              - a.Z;
			return (float)Math.Sqrt(dx * dx + dz * dz);
		}

		public static int DistanceSquared(GridPos a, GridPos b)
		{
			var dx = b.X - a.X;
			var dz = b.Z - a.Z;
			return dx * dx + dz * dz;
		}

		public static int ManhattanDistance(GridPos a, GridPos b)
		{
			var dx = Math.Abs(b.X - a.X);
			var dz = Math.Abs(b.Z - a.Z);
			return dx + dz;
		}

		public static GridPos Average( GridPos g1, GridPos g2 )
		{
			return new GridPos((g1.X + g2.X) / 2, (g1.Z + g2.Z) / 2 );
		}

		public override string ToString( ) => $"[{X}, {Z}]";
	}
}
