using System;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	public readonly struct GridPosSide
	{
		public readonly GridPos Position;
		public readonly Direction Side;

		public GridPosSide( GridPos position, Direction side )
		{
			Position = position;
			Side = side;
		}

		public (Vector2 p1, Vector2 p2) GetSidePoints( )
		{
			switch ( Side )
			{
				case Direction.Forward: return ( Position + Vector2i.Forward, Position + Vector2i.One );
				case Direction.Right: return (Position + Vector2i.One, Position + Vector2i.Right);
				case Direction.Back: return ( Position + Vector2i.Right, Position );
				case Direction.Left: return (Position, Position + Vector2i.Forward);
				default:
					throw new ArgumentOutOfRangeException( );
			}
		}
	}
}
