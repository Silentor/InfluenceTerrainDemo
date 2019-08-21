using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
	public class NavigationGrid
	{
		public readonly Bounds2i Bounds;

		public ref readonly Block GetBlock( Vector2i worldPosition )
		{
			if ( Bounds.Contains( worldPosition ) )
			{
				var localPos = World2Local( worldPosition );
				return ref _grid[localPos.X, localPos.Z];
			}

			return ref Block.Default;
		}

		public NavigationGrid( MicroMap map )
		{
			Bounds = map.Bounds;

			_grid = new Block[map.Bounds.Size.X, map.Bounds.Size.Z];

			var xMax = _grid.GetLength(0) - 1;
			var zMax = _grid.GetLength(1) - 1;

			//Iterate in local space
			for (int x = 0; x <= xMax; x++)
			{
				for (int z = 0; z <= zMax; z++)
				{
					ref readonly var block = ref map.GetBlockDataLocal((x, z));
					if (block.IsEmpty)
						continue;

					var normal = block.Normal;
					Incline slope;
					var angle = Vector3.CalculateAngle( Vector3.UnitY, normal );
					if ( angle < MostlyFlat )
						slope = Incline.Flat;
					else if ( angle < SmallSlope )
						slope = Incline.Small;
					else if ( angle < MediumSlope )
						slope = Incline.Medium;
					else 
						slope = Incline.Steep;

					Side2d orientation;
					if ( Math.Abs( normal.Z ) > Math.Abs( normal.X ) )
					{
						if ( normal.Z > 0 )
							orientation = Side2d.Forward;
						else
							orientation = Side2d.Back;
					}
					else
					{
						if ( normal.X > 0 )
							orientation = Side2d.Right;
						else
							orientation = Side2d.Left;
					}

					_grid[x, z] = new Block( new Normal(slope, orientation) );
				}
			}


		}

		private Block[,] _grid;
		private static readonly float MostlyFlat = MathHelper.DegreesToRadians( 10 );
		private static readonly float SmallSlope = MathHelper.DegreesToRadians( 40 );
		private static readonly float MediumSlope = MathHelper.DegreesToRadians( 70 );
		private static readonly float SteepSlope = MathHelper.DegreesToRadians( 90 );

		protected Vector2i World2Local(Vector2i worldPosition)
		{
			return worldPosition - Bounds.Min;
		}


		public readonly struct Block
		{
			public readonly Normal Normal;

			public Block( Normal normal )
			{
				Normal = normal;
			}

			public static readonly Block Default = new Block();
		}
	}

	public readonly struct Normal
	{
		public readonly Incline Slope;
		public readonly Side2d Orientation;

		public Normal( Incline slope, Side2d orientation )
		{
			Slope = slope;
			Orientation = orientation;
		}
	}

	public enum Incline			: byte
	{
		Flat, 
		Small, 
		Medium, 
		Steep
	}



	
}
