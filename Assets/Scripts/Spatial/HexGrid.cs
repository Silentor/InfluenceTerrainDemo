using System;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Regular hexagonal grid (top-pointed)
	/// </summary>
	public class HexGrid
	{
		public const  float Size   = 10;
		public const  float Width  = Sqrt3 * Size ;
		public const  float Height = Size  * 2;

		/// <summary>
		/// Get hex coords for given block oords
		/// Based on https://www.redblobgames.com/grids/hexagons/more-pixel-to-hex.html (Branchless method)
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public static HexPos BlockToHex( GridPos pos )
		{
			var x = (pos.X + 0.5d)  / Width;
			var z = -(pos.Z + 0.5d) / Width;
		
			var temp = Math.Floor(x + Sqrt3 * z + 1);
			var q    = Math.Floor((Math.Floor(2 * x + 1) + temp)                           / 3);
			var r    = Math.Floor((temp                  + Math.Floor(-x + Sqrt3 * z + 1)) / 3);

			return new HexPos((int)q, -(int)r);
		}

		public static bool IsContains( HexPos hex, GridPos block )
		{
			return BlockToHex( block ) == hex;
		}

		public static Vector2 GetHexCenter( HexPos hex )
		{
			return (Vector2)(hex.Q * QBasis + hex.R * RBasis);
		}

		public static GridPos GetHexCenterBlock( HexPos hex )
		{
			return (GridPos)GetHexCenter( hex );
		}

		/// <summary>
		/// From (1;1) direction clockwise
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		[NotNull]
		public static Vector2[] GetHexVertices( HexPos hex )
		{
			var result = new Vector2[6];
			var center = GetHexCenter( hex );
			for ( var i = 0; i < 6; i++ )
			{
				var angleDeg =  30 - 60f * i;
				var angleRad = MathHelper.DegreesToRadians( angleDeg );
				result[i] = new Vector2( (center.X + Size * (float)Math.Cos(angleRad)), center.Y + Size * (float)Math.Sin(angleRad) );
			}

			return result;
		}

		public static Bounds2i GetHexBound( HexPos hex )
		{
			//Get extreme vertices (internal knowledge)
			var vertices = GetHexVertices( hex );

			var xMax = vertices[0].X;
			var zMin = vertices[2].Y;
			var xMin = vertices[3].X;
			var zMax = vertices[5].Y;

			return new Bounds2i(new GridPos(xMin, zMin), new GridPos(xMax, zMax));
		}

		private static readonly Vector2d	QBasis = new Vector2d(Width,     0);
		private static readonly Vector2d	RBasis = new Vector2d(Width / 2, 3d /4 *Height);
		private const           float		Sqrt3  = 1.732050807568877f;

	}
}
