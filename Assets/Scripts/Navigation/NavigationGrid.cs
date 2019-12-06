using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
	public class NavigationGrid
	{
		public readonly Bounds2i Bounds;

		/// <summary>
		/// Prepare navigation grid from micro map
		/// </summary>
		/// <param name="map"></param>
		public NavigationGrid( MicroMap map )
		{
			Bounds = map.Bounds;

			_grid = new Block[map.Bounds.Size.X, map.Bounds.Size.Z];

			var xMax = _grid.GetLength(0) - 1;
			var zMax = _grid.GetLength(1) - 1;

			//Iterate in local space
			for (uint x = 0; x <= xMax; x++)
			{
				for (uint z = 0; z <= zMax; z++)
				{
					ref readonly var blockData = ref map.GetBlockDataLocal(x, z);
					if (blockData.IsEmpty)
						continue;

					Incline slope;
					Direction orientation;
					ref readonly var block = ref map.GetBlockLocalRef( x, z );
					if ( block.IsObstacle )
					{
						slope = Incline.Blocked;
						orientation = Direction.Forward;
					}
					else
					{
						var    normal = blockData.Normal;
						var    angle  = Vector3.CalculateAngle( Vector3.UnitY, normal );
						slope  = CreateIncline.FromRadians( angle );
						if ( Math.Abs( normal.Z ) > Math.Abs( normal.X ) )
						{
							if ( normal.Z > 0 )
								orientation = Direction.Forward;
							else
								orientation = Direction.Back;
						}
						else
						{
							if ( normal.X > 0 )
								orientation = Direction.Right;
							else
								orientation = Direction.Left;
						}
					}

					_grid[x, z] = new Block( new Normal(slope, orientation) );
				}
			}


		}
		public ref readonly Block GetBlock(GridPos worldPosition )
		{
			if ( Bounds.Contains( worldPosition ) )
			{
				var localPos = World2Local( worldPosition );
				return ref _grid[localPos.X, localPos.Z];
			}

			return ref Block.Default;
		}

		private readonly Block[,] _grid;

		protected GridPos World2Local(GridPos worldPosition)
		{
			return new GridPos(worldPosition.X - Bounds.Min.X, worldPosition.Z - Bounds.Min.Z);
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
		public readonly Direction Orientation;

		public Normal( Incline slope, Direction orientation )
		{
			Slope = slope;
			Orientation = orientation;
		}

		/// <summary>
		/// Get block incline from walk direction point of view
		/// </summary>
		/// <param name="walkDirection"></param>
		/// <returns></returns>
		public LocalIncline Project ( Direction walkDirection )
		{
			if ( Slope == Incline.Flat )
				return LocalIncline.Flat;
			if ( Orientation == walkDirection )
				return (LocalIncline) Slope;
			if ( Orientation.IsPerpendicular ( walkDirection ) )
				return LocalIncline.Flat;
			return (LocalIncline)((int) Slope + 7);
		}
	}

	
}
