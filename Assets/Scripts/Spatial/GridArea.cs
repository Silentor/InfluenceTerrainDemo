﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Immutable. Handy way to store convex area rasterized to Grid
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

		public IEnumerator<GridPos> GetEnumerator ( )
		{
			for ( int z = 0; z < _elements.Length; z++ )
			{
				var (min, max) = _elements [ z ];
				for ( int x = min; x <= max; x++ )
				{
					yield return new GridPos(x, z);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator ( )
		{
			return GetEnumerator ( );
		}
	}
}