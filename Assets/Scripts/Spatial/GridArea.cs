using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Immutable. Compact way to store convex area rasterized to Grid
	/// todo support flipped from Z to Z GridArea if 
	/// </summary>
	public class GridArea : IEnumerable<GridPos>
	{
		public readonly Bounds2i Bound;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sortedPositions">Min-max positions</param>
		public GridArea ( IReadOnlyCollection<(GridPos min, GridPos max)> sortedPositions )
		{
			var minX = int.MaxValue;
			var minZ = sortedPositions.First().min.Z;
			var maxX = int.MinValue;
			var maxZ = sortedPositions.Last().max.Z;

			_elements = new (short, short)[sortedPositions.Count];
			var index = 0;

			//Calculate bounds and prepare buffer
			foreach ( var position in sortedPositions )
			{
				if(position.min.Z != position.max.Z || position.min.X > position.max.X)
					throw new InvalidOperationException();
				
				if (minX > position.min.X)
					minX = position.min.X;

				if (maxX < position.max.X)
					maxX = position.max.X;

				_elements [ index++ ] = (position.min.X, position.max.X);
			}

			Bound = new Bounds2i(new GridPos(minX, minZ), new GridPos(maxX, maxZ));
		}

		public bool IsContains ( GridPos position )
		{
			if ( !Bound.Contains ( position ) )
				return false;

			var (min, max) = _elements [ position.Z - Bound.Min.Z ];
			return position.X >= min && position.X <= max;
		}


		private readonly (short min, short max)[] _elements;

		/// <summary>
		/// Get border blocks
		/// </summary>
		/// <returns></returns>
		public IEnumerable<GridPos> GetBorder( )
		{
			throw new NotImplementedException( "Bugged. Implement via enumerate and test all blocks" );

			//Left side from bottom to top
			for ( int z = 0; z < Bound.Size.Z; z++ )
			{
				yield return new GridPos(_elements[z].min, z + Bound.Min.Z);
			}

			//Top side
			var topRow = _elements[Bound.Size.Z - 1];
			for ( int x = topRow.min + 1; x < topRow.max; x++ )
			{
				yield return new GridPos(x, Bound.Max.Z);
			}

			//Right side from top to bottom
			for ( int z = Bound.Size.Z - 1; z >= 0; z-- )
			{
				yield return new GridPos(_elements[z].max, z + Bound.Min.Z);
			}

			//Bottom side
			var bottomRow = _elements[0];
			for ( int x = bottomRow.max - 1; x > bottomRow.min; x-- )
			{
				yield return new GridPos(x, 0);
			}
		}

		public IEnumerator<GridPos> GetEnumerator ( )
		{
			for ( int z = 0; z < _elements.Length; z++ )
			{
				var resultZ = z + Bound.Min.Z;
				var (min, max) = _elements [ z ];
				for ( int x = min; x <= max; x++ )
				{
					yield return new GridPos(x, resultZ );
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator ( )
		{
			return GetEnumerator ( );
		}
	}
}