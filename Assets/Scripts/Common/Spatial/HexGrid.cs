using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using UnityEngine.Assertions;


[assembly: InternalsVisibleToAttribute( "Assembly-CSharp-Editor")] //Unit tests

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Regular hexagonal grid (top-pointed)
	/// </summary>
	public partial class HexGrid<TCell, TEdge, TVertex> : IEnumerable<HexPos>
	{
		public readonly float Size;
		public readonly float Width;
		public readonly float Height;

		/// <summary>
		/// Block bound of grid
		/// </summary>
		public Box2 Bound => _bound;

		public int CellsCount => _faces.Length;

		public HexGrid( float hexSide, int gridSide )
		{
			if ( gridSide <= 0 ) throw new ArgumentOutOfRangeException( nameof( gridSide ) );
			if ( hexSide  <= 0 ) throw new ArgumentOutOfRangeException( nameof( hexSide ) );

			Size = hexSide;
			Width = (float)(Sqrt3 * Size) ;
			Height = Size * 2;
			QBasis = new Vector2d(Width,     0);
			RBasis = new Vector2d(Width / 2, 3d /4 *Height);

			_faces = new CellHolder[ gridSide, gridSide ];
			var minX = gridSide / 3;
			var minZ = gridSide / 3;
			_gridBound = new Bound2i( new GridPos(-minX, -minZ), gridSide, gridSide );

			var bound1 = GetFaceBound( new HexPos(Array2dToHex( 0, 0 )) );
			var bound2 = GetFaceBound( new HexPos(Array2dToHex( gridSide * 2 - 1, 0 )) );
			var bound3 = GetFaceBound( new HexPos(Array2dToHex( 0, gridSide * 2 - 1 )) );
			var bound4 = GetFaceBound( new HexPos(Array2dToHex( gridSide * 2 - 1, gridSide * 2 - 1 )) );
			_bound = bound1.Inflated( bound2 ).Inflated( bound3 ).Inflated( bound4 );
		}

		public TCell this[ HexPos position ]
		{
			get => Get( position.Q, position.R );
			set => Set( position.Q, position.R, value );
		}

		public TCell this[ int q, int r ]
		{
			get => Get( q, r );
			set => Set( q, r, value );
		}

		public IReadOnlyList<TCell> GetCellsValue( )
		{
			return new CellsValue(_faces);
		}

		//public IEnumerable<CellHolder> GetNeighbors( HexPos hex )
		//{
		//	for ( var i = 0; i < HexPos.Directions.Length; i++ )
		//	{
		//		var dir      = HexPos.Directions[i];
		//		var neighPos		= hex + dir;
		//		if ( IsContains( neighPos ) )
		//			yield return GetOrCreateHolder( neighPos.Q, neighPos.R );
		//	}
		//}
		public IEnumerable<TCell> GetNeighborsValue( HexPos hex )
		{
			for ( var i = 0; i < HexPos.Directions.Length; i++ )
			{
				var dir      = HexPos.Directions[i];
				var neighPos = hex + dir;
				if ( IsContains( neighPos ) )
					yield return GetOrCreateHolder( neighPos.Q, neighPos.R ).Value;
			}
		}
		private Edges GetEdges( HexPos pos )
		{
			var holder = GetHolder( pos );
			if(holder != null)
				return holder.Edges;

			return Edges.Empty;
		}

		public EdgesData GetEdgesValue( HexPos pos )
		{
			var holder = GetOrCreateHolder( pos.Q, pos.R );
			if(holder != null)
				return new EdgesData( holder.Edges );

			throw new ArgumentOutOfRangeException(nameof(pos), pos, "Not in hexgrid");
		}

		public VerticesData GetVerticesValue( HexPos pos )
		{
			var holder = GetOrCreateHolder( pos.Q, pos.R );
			if(holder != null)
				return new VerticesData( holder.Vertices );

			throw new ArgumentOutOfRangeException(nameof(pos), pos, "Not in hexgrid");
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

		/// <summary>
		/// Get hex coords for given block coords
		/// Based on https://www.redblobgames.com/grids/hexagons/more-pixel-to-hex.html (Branchless method)
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public HexPos BlockToHex( GridPos pos )
		{
			var x = (pos.X + 0.5d)  / Width;
			var z = -(pos.Z + 0.5d) / Width;
		
			var temp = Math.Floor(x + Sqrt3 * z + 1);
			var q    = Math.Floor((Math.Floor(2 * x + 1) + temp)                           / 3);
			var r    = Math.Floor((temp                  + Math.Floor(-x + Sqrt3 * z + 1)) / 3);

			return new HexPos((int)q, -(int)r);
		}

		public bool IsContains( HexPos pos )
		{
			var (x, y) = HexToArray2d( pos.Q, pos.R );

			return x >= 0 && x < _gridBound.Size.X && y >= 0 && y < _gridBound.Size.Z;
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
				result[i] = new Vector2( (center.X + Size * (float)Math.Cos(angleRad)), center.Y + Size * (float)Math.Sin(angleRad) );
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


		private readonly CellHolder[,] _faces;
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
			return _faces[x, y];
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
			var face = _faces[x, y];
			
			if ( face != null )
				return face;

			face = new CellHolder( new HexPos(q, r), this );
			_faces[x, y] = face;
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
			for ( var i = 0; i < HexPos.Directions.Length; i++ )
			{
				var dir      = HexPos.Directions[i];
				var neighPos = hex + dir;
				if ( IsContains( neighPos ) )
					yield return neighPos;
			}
		}

		internal CellHolder[,] GetInternalStorage( )
		{
			return _faces;
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
			for ( int x = 0; x < _faces.GetLength( 0 ); x++ )
			{
				for ( int y = 0; y < _faces.GetLength( 1 ); y++ )
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
			public readonly Edges			Edges;
			public readonly Vertices		Vertices;

			public			Vector2			Center		=> _grid.GetFaceCenter ( Pos );

			public bool IsContains( Vector2 position )
			{
				return _grid.BlockToHex( (GridPos) position ) == Pos;
			}

			internal CellHolder( HexPos position, HexGrid<TCell, TEdge, TVertex> owner )
			{
				Pos = position;
				_grid = owner;

				//Populate edges
				var edges = new EdgeHolder[6];
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;

					CellHolder neigh;
					if(owner.IsContains( neighPos ) && (neigh = owner.GetHolder( neighPos.Q, neighPos.R )) != null)
					{
						var oppositeIndex = ( i + 3 ) % 6;
						edges[i] = neigh.Edges[oppositeIndex];
					}
					else
					{
						edges[i] = new EdgeHolder( position, i, owner );
					}
				}
				Edges = new Edges( edges );

				//Populate vertices
				var vertices = new VertexHolder[6];
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;

					CellHolder neigh;
					if ( owner.IsContains( neighPos ) && ( neigh = owner.GetHolder( neighPos.Q, neighPos.R )) != null )
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;
						var oppositeVert1 = ( i + 4 ) % 6;
						var oppositeVert2 = ( i + 3 ) % 6;

						vertices[vert1] = neigh.Vertices[oppositeVert1];
						vertices[vert2] = neigh.Vertices[oppositeVert2];
					}
					else
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;

						if ( vertices[vert1] == null )
						{
							vertices[vert1] = new VertexHolder( position, vert1, owner );
						}

						if ( vertices[vert2] == null )
						{
							vertices[vert2] = new VertexHolder( position, vert2, owner );
						}
					}
				}
				Vertices = new Vertices(vertices);
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

			public VertexHolder Vertex1 =>	Grid.GetHolder(  Cell1 ).Vertices [ Index ];
			public VertexHolder Vertex2 =>	Grid.GetHolder(  Cell1 ).Vertices [ (Index + 1) % 6 ];
			
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

				Cell2 = cell + HexPos.Directions[index];
			}
		}

		[DebuggerDisplay( "{" + nameof( Data ) + "}" )]
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

			public TVertex Data;

			internal VertexHolder( HexPos cell, int index, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Assert.IsTrue( index >= 0 && index < 6);

				Cell1 = cell;
				Index = index;
				Grid = grid;

				Cell2 = cell + HexPos.Directions[index];
				Cell3 = cell + HexPos.Directions[(index + 5) % 6];
			}
		}

		public readonly struct CellsValue : IReadOnlyList<TCell>
		{
			public IEnumerator<TCell> GetEnumerator( )
			{
				for ( int i = 0; i < _faces.GetUpperBound( 0 ); i++ )
					for ( int j = 0; j < _faces.GetUpperBound( 1 ); j++ )
					{
						if ( _faces[i, j] != null )
							yield return _faces[i, j].Value;
						else
							yield return default;
					}
			}
			IEnumerator IEnumerable.  GetEnumerator( )
			{
				return GetEnumerator( );
			}
			public int Count => _faces.Length;

			public TCell this[ int index ]
			{
				get
				{
					if( index < 0 || index >= Count )
						throw new ArgumentOutOfRangeException(nameof(index), index, "cell index");

					var i = index % _faces.GetLength( 0 );
					var j = index / _faces.GetLength( 0 );

					return _faces[i, j] != null ? _faces[i, j].Value : default;
				}
			}

			internal CellsValue( CellHolder[,] faces  )
			{
				_faces = faces;
			}

			private readonly CellHolder[,] _faces;
		}

		//[DebuggerDisplay( "Edges of {_cell.Pos}")]
		public readonly struct Edges : IReadOnlyList<EdgeHolder>
		{
			public readonly EdgeHolder Edge1;
			public readonly EdgeHolder Edge2;
			public readonly EdgeHolder Edge3;
			public readonly EdgeHolder Edge4;
			public readonly EdgeHolder Edge5;
			public readonly EdgeHolder Edge6;

			public static readonly Edges Empty = new Edges();

			public int Count { get; }

			public EdgeHolder this[ HexDir index ]	=>		this[(int)index];

			public EdgeHolder this[int index]
			{
				get {
					switch ( index )
					{
						case 0 : return Edge1;
						case 1 : return Edge2;
						case 2 : return Edge3;
						case 3 : return Edge4;
						case 4 : return Edge5;
						case 5 : return Edge6;
						default: throw new ArgumentOutOfRangeException(nameof(index), index, "Index invalid");
					}
				}
			}

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
			internal Edges( IReadOnlyList<EdgeHolder> edges )
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

		//[DebuggerDisplay( "Edges of {_cell.Pos}")]
		public readonly struct EdgesData : IReadOnlyList<TEdge>
		{
			public int Count => _edges.Count;

			public TEdge this[HexDir index]
			{
				get => _edges [ index ].Value;
				set => _edges [ index ].Value = value;
			}

			public TEdge this[int index]
			{
				get => _edges[ index ].Value;
				set => _edges[ index ].Value = value;
			}

			public IEnumerator<TEdge> GetEnumerator()
			{
				return _edges.Select( e => e.Value ).GetEnumerator(  );
			}

			internal EdgesData(Edges edges)
			{
				_edges = edges;
			}

			private readonly Edges _edges;

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		//[DebuggerDisplay( "Vertices of {_cell.Pos}")]
		public struct Vertices : IReadOnlyList<VertexHolder>
		{
			public readonly VertexHolder Vertex1;
			public readonly VertexHolder Vertex2;
			public readonly VertexHolder Vertex3;
			public readonly VertexHolder Vertex4;
			public readonly VertexHolder Vertex5;
			public readonly VertexHolder Vertex6;

			public int Count { get; }

			public VertexHolder this[int index]
			{
				get {
					switch ( index )
					{
						case 0 : return Vertex1;
						case 1 : return Vertex2;
						case 2 : return Vertex3;
						case 3 : return Vertex4;
						case 4 : return Vertex5;
						case 5 : return Vertex6;
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

			internal Vertices( IReadOnlyList<VertexHolder> edges )
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

		public struct VerticesData : IReadOnlyList<TVertex>
		{
			public int Count => _vertices.Count;

			public TVertex this[int index]
			{
				get => _vertices[index].Data;
				set => _vertices[index].Data = value;
			}

			internal VerticesData(Vertices vertices)
			{
				_vertices = vertices;
			}

			private readonly Vertices _vertices;

			public IEnumerator<TVertex> GetEnumerator ( )
			{
				yield return _vertices [ 0 ].Data;
				yield return _vertices [ 1 ].Data;
				yield return _vertices [ 2 ].Data;
				yield return _vertices [ 3 ].Data;
				yield return _vertices [ 4 ].Data;
				yield return _vertices [ 5 ].Data;
			}

			IEnumerator IEnumerable.GetEnumerator ( )
			{
				return GetEnumerator ( );
			}
		}
	}
}
