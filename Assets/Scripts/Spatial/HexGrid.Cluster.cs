using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	public partial class HexGrid<TCell, TEdge, TVertex>
	{
		/// <summary>
		/// Connected group of Cells
		/// </summary>
		public class Cluster : IReadOnlyCollection<HexPos>
		{
			public readonly HexGrid<TCell, TEdge, TVertex> Grid;

			public TCell this[ HexPos position ]
			{
				get
				{
					if ( _cluster.Contains( position ) )
						return Grid[position];
					throw new ArgumentOutOfRangeException( nameof(position), position, "Out of cluster" );
				}
			}

			public int Count => _cluster.Count;

			internal Cluster( HexGrid<TCell, TEdge, TVertex> grid )
			{
				Grid = grid;
			}

			public bool IsContain( HexPos cell )
			{
				return _cluster.Contains( cell );
			}

			public bool NotContain( HexPos cell )
			{
				return !_cluster.Contains( cell );
			}

			public void Add( HexPos cell )
			{
				if ( Grid.IsContains( cell ) )
					_cluster.Add( cell );
			}

			public void Remove( HexPos cell )
			{
				_cluster.Remove( cell );
			}

			public IEnumerable<HexPos> GetBorderCells( )
			{
				foreach ( var cell in _cluster )
				{
					if ( Grid.GetNeighborPositions( cell ).Any( hp => !Grid.IsContains( hp ) ) )
						yield return cell;
				}
			}

			public IEnumerable<EdgeHolder> GetBorderEdges( )
			{
				foreach ( var borderCell in GetBorderCells() )
				{
					foreach ( var edge in Grid.GetEdges( borderCell )
					                          .Where( e => NotContain( e.OppositeCell( borderCell ) ) ) )
					{
						yield return edge;
					}
				}
			}

			public HexPos GetNearestCell( GridPos position )
			{
				var hex = Grid.BlockToHex( position );
				if ( IsContain( hex ) )
					return hex;

				//Check cluster cells
				var    pos         = (Vector2) position;
				var    minDistance = float.MaxValue;
				HexPos minHex      = _cluster.First();
				foreach ( var hex2 in _cluster )
				{
					var cellCenter = Grid.GetHexCenter( hex2 );
					var distance   = Vector2.DistanceSquared( pos, cellCenter );
					if ( distance < minDistance )
					{
						minDistance = distance;
						minHex      = hex2;
					}
				}

				return minHex;
			}

			public IEnumerable<HexPos> FloodFill( HexPos start, CheckCellPredicate fillCondition = null )
			{
				return Grid.FloodFill( start, IsContain, fillCondition );
			}
			
			private readonly HashSet<HexPos> _cluster = new HashSet<HexPos>();

			public IEnumerator<HexPos> GetEnumerator( )
			{
				return _cluster.GetEnumerator(  );
			}
			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator( );
			}
		}
	}
}
