using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Tools;
using UnityEngine.Assertions;


[assembly: InternalsVisibleToAttribute( "Assembly-CSharp-Editor")] //Unit tests

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Regular hexagonal grid (top-pointed)
	/// </summary>
	public partial class HexGrid<TCell, TEdge, TVertex> : IEnumerable<HexPos> //  : IReadOnlyHexGrid<TCell>
	{
		public readonly float CellSideSize;
		public readonly float CellWidth;
		public readonly float CellHeight;

		/// <summary>
		/// Block bound of grid
		/// </summary>
		public Box2 Bound => _bound;

		public int Count { get; private set; }

		public IReadOnlyList<EdgeHolder>	Edges		=> _allEdges;
		public IReadOnlyList<VertexHolder>	Vertices	=> _allVertices;

		public HexGrid( float hexSide, int gridSide )
		{
			if ( gridSide <= 0 ) throw new ArgumentOutOfRangeException( nameof( gridSide ) );
			if ( hexSide  <= 0 ) throw new ArgumentOutOfRangeException( nameof( hexSide ) );

			CellSideSize = hexSide;
			CellWidth = (float)(Sqrt3 * CellSideSize) ;
			CellHeight = CellSideSize * 2;
			QBasis = new Vector2d(CellWidth,     0);
			RBasis = new Vector2d(CellWidth / 2, 3d /4 *CellHeight);

			_grid = new CellHolder[ gridSide, gridSide ];
			var minX = gridSide / 3;
			var minZ = gridSide / 3;
			_gridBound = new Bound2i( new GridPos(-minX, -minZ), gridSide, gridSide );

			var bound1 = GetFaceBound( new HexPos(Array2dToHex( 0, 0 )) );
			var bound2 = GetFaceBound( new HexPos(Array2dToHex( gridSide * 2 - 1, 0 )) );
			var bound3 = GetFaceBound( new HexPos(Array2dToHex( 0, gridSide * 2 - 1 )) );
			var bound4 = GetFaceBound( new HexPos(Array2dToHex( gridSide * 2 - 1, gridSide * 2 - 1 )) );
			_bound = bound1.Inflated( bound2 ).Inflated( bound3 ).Inflated( bound4 );

			FillTheGrid();
		}

		
		public CellHolder this[ HexPos position ]
		{
			get => GetHolder( position.Q, position.R );
		}
		
		public CellHolder this[ GridPos position ]
		{
			get
			{
				var hexPos = BlockToHex( position );
				if( IsContains( hexPos ) )
					return this[ hexPos ];

				return null;
			}
		}

		protected virtual void FillTheGrid( )
		{
			//Fill all the grid by default
			for ( int i = 0; i < _grid.GetLength( 0 ); i++ )
			{
				for ( int j = 0; j < _grid.GetLength( 1 ); j++ )
				{
					_grid[ i, j ] = new CellHolder( new HexPos( Array2dToHex( i, j ) ), this );
					Count++;
				}
			}
		}

		public IEnumerable<HexPos> FloodFill(HexPos startFace, CheckCellPredicate fillCondition = null)
		{
			return FloodFill ( startFace, IsContains, fillCondition );
		}

		private IEnumerable<HexPos> FloodFill(HexPos startFace, Predicate<HexPos> bound, CheckCellPredicate fillCondition = null)
		{
			var distanceEnumerator = new FloodFiller(this, startFace, bound, fillCondition);
			for (var distance = 0; distance < 10; distance++)
			{
				foreach (var position in distanceEnumerator.GetNeighbors(distance))
					yield return position;
			}
		}

		public IEnumerable<HexPos> FindNearestNeighbors( Vector2 from, float radius )
		{
			//Find main cell
			var centerHexPos = BlockToHex( (GridPos) from );
			var center       = GetFaceCenter( centerHexPos );
			
			//Flood fill around main cell
			var filler = new FloodFiller( this, centerHexPos, _ => true );
			for ( int i = 0; i < 10; i++ )
			{
				var cells           = filler.GetNeighbors( i );
				var anyCellInRadius = false;
				foreach ( var cell in cells )
				{
					var cellCenter = GetFaceCenter( cell );
					if ( Vector2.Distance( center, cellCenter ) <= radius )
					{
						anyCellInRadius = true;
						if ( IsContains( cell ) )
							yield return cell;
					}
				}
				if( !anyCellInRadius )
					break;
			}
		}

		/// <summary>
		/// Get hex coords for given block coords
		/// Based on https://www.redblobgames.com/grids/hexagons/more-pixel-to-hex.html (Branchless method)
		/// todo consider more detailed algo https://justinpombrio.net/programming/2020/04/28/pixel-to-hex.html
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public HexPos BlockToHex( GridPos pos )
		{
			var x = (pos.X + 0.5d)  / CellWidth;
			var z = -(pos.Z + 0.5d) / CellWidth;
		
			var temp = Math.Floor(x + Sqrt3 * z + 1);
			var q    = Math.Floor((Math.Floor(2 * x + 1) + temp)                           / 3);
			var r    = Math.Floor((temp                  + Math.Floor(-x + Sqrt3 * z + 1)) / 3);

			return new HexPos((int)q, -(int)r);
		}

		public bool IsContains( HexPos pos )
		{
			var (x, y) = HexToArray2d( pos.Q, pos.R );

			return x >= 0 && x < _gridBound.Size.X && y >= 0 && y < _gridBound.Size.Z && GetHolder( pos ) != null;
		}

		//https://www.redblobgames.com/grids/hexagons/#line-drawing
		public HexPos[] RasterizeLine( HexPos from, HexPos to )
		{
			if(from == to)
				return new []{ from };

			var distance = from.Distance( to );
			var result = new HexPos[distance + 1];
			var from3 = (Vector3)from.ToCube( );
			var to3   = (Vector3)to.ToCube( );

			for ( int i = 0; i <= distance; i++ )
			{
				var lerperVector = Vector3.Lerp( from3, to3, 1f / distance * i );
				var roundedVector = CubeRound( lerperVector );
				result[i] = HexPos.FromCube( roundedVector );
			}

			return result;
		}

		public Cluster GetCluster( IEnumerable<HexPos> cluster )
		{
			var result = new Cluster( this );
			foreach ( var hexPos in cluster )
			{
				result.Add( hexPos );
			}

			return result;
		}

#region Layout

		public Vector2 GetFaceCenter( HexPos hex )
		{
			return (Vector2)(hex.Q * QBasis + hex.R * RBasis);
		}

		/// <summary>
		/// From (1;1) direction clockwise
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		[NotNull]
		public Vector2[] GetFaceVerticesPosition( HexPos hex )
		{
			var result = new Vector2[6];
			var center = GetFaceCenter( hex );
			for ( var i = 0; i < 6; i++ )
			{
				var angleDeg =  30 - 60f * i;
				var angleRad = MathHelper.DegreesToRadians( angleDeg );
				result[i] = new Vector2( (center.X + CellSideSize * (float)Math.Cos(angleRad)), center.Y + CellSideSize * (float)Math.Sin(angleRad) );
			}

			return result;
		}

		public Box2 GetFaceBound( HexPos hex )
		{
			//Get extreme vertices (internal knowledge)
			var vertices = GetFaceVerticesPosition( hex );

			var xMax = vertices[0].X;
			var zMin = vertices[2].Y;
			var xMin = vertices[3].X;
			var zMax = vertices[5].Y;

			return new Box2(new Vector2(xMin, zMin), new Vector2(xMax, zMax));
		}

		#endregion


		private readonly CellHolder[,] _grid;
		private readonly List<EdgeHolder> _allEdges = new List<EdgeHolder>();
		private readonly List<VertexHolder> _allVertices = new List<VertexHolder>();
		private readonly Vector2d	QBasis ;
		private readonly Vector2d	RBasis ;
		private const           double		Sqrt3  = 1.732050807568877d;
		private readonly Bound2i _gridBound;
		private readonly Box2	_bound;

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private CellHolder Set( int q, int r, TCell data )
		{
			var holder = GetOrCreateHolder( q, r );
			holder.Value = data;
			return holder;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private TCell Get( int q, int r )
		{
			var faceHolder = GetHolder( q, r );
			if ( faceHolder != null )
				return faceHolder.Value;

			return default;
		}
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private CellHolder GetHolder( int q, int r )
		{
			CheckHexPosition( q, r );

			var (x, y) = HexToArray2d( q, r );
			return _grid[x, y];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private CellHolder GetHolder( HexPos pos )
		{
			return GetHolder( pos.Q, pos.R );
		}

		private CellHolder GetOrCreateHolder( int q, int r )
		{
			CheckHexPosition( q, r );

			var (x, y) = HexToArray2d( q, r );
			var face = _grid[x, y];
			
			if ( face != null )
				return face;

			face = new CellHolder( new HexPos(q, r), this );
			_grid[x, y] = face;
			return face;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal (int x, int y) HexToArray2d( int q, int r )
		{
			var x = q + r / 2	- _gridBound.Min.X;
			var y = r			- _gridBound.Min.Z;
			
			return ( x, y );
		}
		
		internal (int q, int r) Array2dToHex( int x, int y )
		{
			var r = y + _gridBound.Min.Z;
			var q = x - r / 2 + _gridBound.Min.X;
			
			return ( q, r );
		}
		private void CheckHexPosition( int q, int r )
		{
			var (x, y) = HexToArray2d( q, r );

			if ( x < 0 || x >= _gridBound.Size.X || y < 0 || y >= _gridBound.Size.Z ) 
				throw new ArgumentOutOfRangeException( "hex", new HexPos(q, r), 
				                                       $"out of range <0, {_gridBound.Size.X}> <0, {_gridBound.Size.Z}>" );
		}
		private void CheckHexPosition( HexPos pos )
		{
			CheckHexPosition( pos.Q, pos.R );
		}

		private IEnumerable<HexPos> GetNeighborPositions( HexPos hex )
		{
			for ( var i = 0; i < HexPos.VectorDirections.Length; i++ )
			{
				var dir      = HexPos.VectorDirections[i];
				var neighPos = hex + dir;
				if ( IsContains( neighPos ) )
					yield return neighPos;
			}
		}

		internal CellHolder[,] GetInternalStorage( )
		{
			return _grid;
		}

		//https://www.redblobgames.com/grids/hexagons/#rounding
		private static Vector3i CubeRound( Vector3 cubeFloat )
		{
			var rx = (int)Math.Round( cubeFloat.X );
			var ry = (int)Math.Round( cubeFloat.Y );
			var rz = (int)Math.Round( cubeFloat.Z );

			var diffx = Math.Abs( rx - cubeFloat.X );
			var diffy = Math.Abs( ry - cubeFloat.Y );
			var diffz = Math.Abs( rz - cubeFloat.Z );

			if ( diffx > diffy && diffx > diffz )
				rx = -ry - rz;
			else if ( diffy > diffz )
				ry = -rx - rz;
			else
				rz = -rx - ry;

			return new Vector3i(rx, ry, rz);
		}

		#region IEnumerator

		public IEnumerator<HexPos> GetEnumerator( )
		{
			for ( int x = 0; x < _grid.GetLength( 0 ); x++ )
			{
				for ( int y = 0; y < _grid.GetLength( 1 ); y++ )
				{
					var (q, r) = Array2dToHex( x, y );
					yield return new HexPos(q, r);
				}
			}
		}
		IEnumerator IEnumerable.GetEnumerator( )
		{
			return GetEnumerator( );
		}

		#endregion

		[DebuggerDisplay( "{Pos} = {Value}")]
		public class CellHolder
		{
			public readonly HexPos			Pos;
			public			TCell			Value;
			public readonly CellEdges			CellEdges;
			public readonly CellVertices		CellVertices;
			public			Neighbors		Neighbors => new Neighbors( Pos, _grid );

			public			Vector2			Center		=> _grid.GetFaceCenter ( Pos );

			public bool IsContains( Vector2 position )
			{
				return _grid.BlockToHex( (GridPos) position ) == Pos;
			}

			public override string ToString( )
			{
				var neighborsString = new StringBuilder();
				var neighbors = Neighbors;
				foreach ( var neighDir in HexPos.HexDirections )
				{
					var neighborCell = neighbors[ neighDir ];
					if ( neighborCell != null )
					{
						neighborsString.Append( $"{neighDir} = {neighborCell.Pos}, " );
					}
				}

				return $"HexGrid.CellHolder {Pos}, neighbors ({neighborsString.ToString()})";
			}

			public static implicit operator TCell (CellHolder holder)
			{
				return holder.Value;
			}

			internal CellHolder( HexPos position, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Pos = position;
				_grid = grid;

				//Populate edges
				var edges = new EdgeHolder[6];
				for ( var i = 0; i < HexPos.VectorDirections.Length; i++ )
				{
					var dir      = HexPos.VectorDirections[i];
					var neighPos = position + dir;

					CellHolder neigh;
					if(grid.IsContains( neighPos ) && (neigh = grid.GetHolder( neighPos.Q, neighPos.R )) != null)
					{
						var oppositeIndex = ( i + 3 ) % 6;
						edges[i] = neigh.CellEdges[oppositeIndex];
					}
					else
					{
						edges[i] = new EdgeHolder( position, i, grid );
					}
				}
				CellEdges = new CellEdges( edges );

				//Populate vertices
				var vertices = new VertexHolder[6];
				for ( var i = 0; i < HexPos.VectorDirections.Length; i++ )
				{
					var dir      = HexPos.VectorDirections[i];
					var neighPos = position + dir;

					CellHolder neigh;
					if ( grid.IsContains( neighPos ) && ( neigh = grid.GetHolder( neighPos.Q, neighPos.R )) != null )
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;
						var oppositeVert1 = ( i + 4 ) % 6;
						var oppositeVert2 = ( i + 3 ) % 6;

						vertices[vert1] = neigh.CellVertices[oppositeVert1];
						vertices[vert2] = neigh.CellVertices[oppositeVert2];
					}
					else
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;

						if ( vertices[vert1] == null )
						{
							vertices[vert1] = new VertexHolder( position, vert1, grid );
						}

						if ( vertices[vert2] == null )
						{
							vertices[vert2] = new VertexHolder( position, vert2, grid );
						}
					}
				}
				CellVertices = new CellVertices(vertices);
			}

			private readonly HexGrid<TCell, TEdge, TVertex> _grid;
		}

		[DebuggerDisplay( "{Value}" )]
		public class EdgeHolder
		{
			public readonly HexPos Cell1;
			public readonly int Index;
			public readonly HexPos Cell2;
			public readonly HexGrid<TCell, TEdge, TVertex> Grid;

			public VertexHolder Vertex1 =>	Grid.GetHolder(  Cell1 ).CellVertices [ Index ];
			public VertexHolder Vertex2 =>	Grid.GetHolder(  Cell1 ).CellVertices [ (Index + 1) % 6 ];
			
			public TEdge Value;

			public HexPos OppositeCell( HexPos cell )
			{
				if ( Cell1 == cell )
					return Cell2;
				else if ( Cell2 == cell )
					return Cell1;

				throw new ArgumentOutOfRangeException(  );
			}

			internal EdgeHolder( HexPos cell, int index, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Assert.IsTrue( index >= 0 && index < 6);

				Cell1 = cell;
				Index = index;
				Grid = grid;

				Cell2 = cell + HexPos.VectorDirections[index];
				grid._allEdges.Add( this );
			}
		}

		[DebuggerDisplay( "{" + nameof( Value ) + "}" )]
		public class VertexHolder
		{
			public readonly HexPos Cell1;
			/// <summary>
			/// Vertex index relatively <see cref="Cell1"/>
			/// </summary>
			public readonly int Index;

			public readonly HexPos Cell2;
			public readonly HexPos Cell3;

			public readonly HexGrid<TCell, TEdge, TVertex> Grid;

			public Vector2 Position => Grid.GetFaceVerticesPosition(Cell1)[Index];

			public TVertex Value;

			internal VertexHolder( HexPos cell, int index, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Assert.IsTrue( index >= 0 && index < 6);

				Cell1 = cell;
				Index = index;
				Grid = grid;

				Cell2 = cell + HexPos.VectorDirections[index];
				Cell3 = cell + HexPos.VectorDirections[(index + 5) % 6];

				grid._allVertices.Add( this );
			}
		}

		public readonly struct Neighbors : IReadOnlyList<CellHolder>
		{
			public CellHolder this[ HexDir index ]	
			{
				get
				{
					var neighPos = _center + index;
					if ( _grid.IsContains( neighPos ) )
						return _grid[ neighPos ];

					return null;
				}
			}
			
			public CellHolder this[int index] => this[ (HexDir)index ];

			public int Count		=> Enumerable.Count( this );

			public IEnumerator<CellHolder> GetEnumerator( )
			{
				for ( var i = HexDir.QPlus; i <= HexDir.SMinus; i++ )
				{
					var neighCell = this[ i ];
					if ( neighCell != null )
						yield return neighCell;
				}
			}

			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator();
			}
			
			internal Neighbors( HexPos centerPosition, HexGrid<TCell, TEdge,TVertex> grid )
			{
				_center = centerPosition;
				_grid = grid;
			}
			
			private readonly HexPos _center;
			private readonly HexGrid<TCell, TEdge,TVertex> _grid;
		}

		//[DebuggerDisplay( "Edges of {_cell.Pos}")]
		public readonly struct CellEdges : IReadOnlyList<EdgeHolder>
		{
			public readonly EdgeHolder Edge1;
			public readonly EdgeHolder Edge2;
			public readonly EdgeHolder Edge3;
			public readonly EdgeHolder Edge4;
			public readonly EdgeHolder Edge5;
			public readonly EdgeHolder Edge6;

			public static readonly CellEdges Empty = new CellEdges();

			public int Count { get; }

			public EdgeHolder this[ HexDir index ]	
			{
				get {
					switch ( index )
					{
						case HexDir.QPlus :		return Edge1;
						case HexDir.SPlus :		return Edge2;
						case HexDir.RMinus :	return Edge3;
						case HexDir.QMinus :	return Edge4;
						case HexDir.SMinus :	return Edge5;
						case HexDir.RPlus :		return Edge6;
						default: throw new ArgumentOutOfRangeException(nameof(index), index, "Index invalid");
					}
				}
			}

			public EdgeHolder this[int index] => this[ (HexDir) index ];

			public IEnumerator<EdgeHolder> GetEnumerator( )
			{
				if( Edge1 == null )
					yield break;

				yield return Edge1;
				yield return Edge2;
				yield return Edge3;
				yield return Edge4;
				yield return Edge5;
				yield return Edge6;
			}
			internal CellEdges( IReadOnlyList<EdgeHolder> edges )
			{
				if(edges.Count != 6)
					throw new ArgumentOutOfRangeException(nameof(edges), edges.Count, "must be 6" );

				Edge1 = edges[0];
				Edge2 = edges[1];
				Edge3 = edges[2];
				Edge4 = edges[3];
				Edge5 = edges[4];
				Edge6 = edges[5];

				Count = 6;
			}

			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator( );
			}
		}

		//[DebuggerDisplay( "Vertices of {_cell.Pos}")]
		public struct CellVertices : IReadOnlyList<VertexHolder>
		{
			public readonly VertexHolder Vertex1;
			public readonly VertexHolder Vertex2;
			public readonly VertexHolder Vertex3;
			public readonly VertexHolder Vertex4;
			public readonly VertexHolder Vertex5;
			public readonly VertexHolder Vertex6;

			public static readonly CellVertices Empty = new CellVertices();
			
			public int Count { get; }

			public VertexHolder this[int index]		=> this[ (HexDir) index ];
			
			public VertexHolder this[HexDir index]
			{
				get {
					switch ( index )
					{
						case HexDir.QPlus :	 return Vertex1;
						case HexDir.SPlus :	 return Vertex2;
						case HexDir.RMinus : return Vertex3;
						case HexDir.QMinus : return Vertex4;
						case HexDir.SMinus : return Vertex5;
						case HexDir.RPlus :	 return Vertex6;
						default: throw new ArgumentOutOfRangeException(nameof(index), index, "Index invalid");
					}
				}
			}

			public IEnumerator<VertexHolder> GetEnumerator( )
			{
				yield return Vertex1;
				yield return Vertex2;
				yield return Vertex3;
				yield return Vertex4;
				yield return Vertex5;
				yield return Vertex6;
			}

			internal CellVertices( IReadOnlyList<VertexHolder> edges )
			{
				if(edges.Count != 6)
					throw new ArgumentOutOfRangeException(nameof(edges), edges.Count, "must be 6" );

				Vertex1 = edges[0];
				Vertex2 = edges[1];
				Vertex3 = edges[2];
				Vertex4 = edges[3];
				Vertex5 = edges[4];
				Vertex6 = edges[5];

				Count = 6;
			}

			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator( );
			}
		}
	}
}
