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

			public CellHolder this[ HexPos position ]
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

			public bool Contains( HexPos cell )
			{
				return _cluster.Contains( cell );
			}

			public bool NotContains( HexPos cell )
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
					if ( Grid.GetNeighborPositions( cell ).Any( hp => !Contains( hp ) ) )
						yield return cell;
				}
			}

			public IEnumerable<TEdge> GetBorderEdges( )
			{
				foreach ( var borderCell in GetBorderCells() )
				{
					foreach ( var edge in Grid[borderCell].CellEdges
					                          .Where( e => NotContains( e.OppositeCell( borderCell ) ) ) )
					{
						yield return edge.Value;
					}
				}
			}

			public HexPos GetNearestCell( GridPos position )
			{
				var hex = Grid.BlockToHex( position );
				if ( Contains( hex ) )
					return hex;

				//Check cluster cells
				var    pos         = (Vector2) position;
				var    minDistance = float.MaxValue;
				HexPos minHex      = _cluster.First();
				foreach ( var hex2 in _cluster )
				{
					var cellCenter = Grid.GetFaceCenter( hex2 );
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
				return Grid.FloodFill( start, Contains, fillCondition );
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
