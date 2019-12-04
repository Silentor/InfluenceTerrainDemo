using System;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	public readonly struct GridPosSide
	{
		public readonly GridPos Position;
		public readonly Side2d Side;

		public GridPosSide( GridPos position, Side2d side )
		{
			Position = position;
			Side = side;
		}

		public (Vector2 p1, Vector2 p2) GetSidePoints( )
		{
			switch ( Side )
			{
				case Side2d.Forward: return ( Position + Vector2i.Forward, Position + Vector2i.One );
				case Side2d.Right: return (Position + Vector2i.One, Position + Vector2i.Right);
				case Side2d.Back: return ( Position + Vector2i.Right, Position );
				case Side2d.Left: return (Position, Position + Vector2i.Forward);
				default:
					throw new ArgumentOutOfRangeException( );
			}
		}
	}
}
