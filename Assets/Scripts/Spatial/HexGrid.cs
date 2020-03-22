using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Experimental.AI;


[assembly: InternalsVisibleToAttribute( "Assembly-CSharp-Editor")] //Unit tests

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Regular hexagonal grid (top-pointed) optimized to block rasterization
	/// </summary>
	public partial class HexGrid<TCell, TEdge, TVertex> : IEnumerable<HexPos>
	{
		public readonly float Size;
		public readonly float Width;
		public readonly float Height;

		public Bound2i Bound => _bound;

		public HexGrid( float hexSide, int gridRadius )
		{
			Size = hexSide;
			Width = Sqrt3 * Size ;
			Height = Size * 2;
			QBasis = new Vector2d(Width,     0);
			RBasis = new Vector2d(Width / 2, 3d /4 *Height);

			_faces = new CelHolder[ gridRadius * 2, gridRadius * 2 ];
			var bound1 = GetHexBound( new HexPos(Array2dToHex( 0, 0 )) );
			var bound2 = GetHexBound( new HexPos(Array2dToHex( gridRadius * 2 - 1, 0 )) );
			var bound3 = GetHexBound( new HexPos(Array2dToHex( 0, gridRadius * 2 - 1 )) );
			var bound4 = GetHexBound( new HexPos(Array2dToHex( gridRadius * 2 - 1, gridRadius * 2 - 1 )) );
			_bound = bound1.Add( bound2 ).Add( bound3 ).Add( bound4 );
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

		public TEdge GetEdgeData( HexPos hex, HexDir direction )
		{
			return GetEdges( hex )[direction].Data;
		}

		public Edges GetEdges( HexPos hex )
		{
			var holder = GetOrCreateHolder( hex.Q, hex.R );
			return new Edges( holder );
		}

		public TVertex GetVertexData( HexPos hex, int direction )
		{
			return GetVertices( hex )[direction].Data;
		}

		public Vertices GetVertices( HexPos hex )
		{
			var holder = GetOrCreateHolder( hex.Q, hex.R );
			return new Vertices( holder );
		}

		public IEnumerable<TCell> GetNeighbors( HexPos hex )
		{
			for ( var i = 0; i < HexPos.Directions.Length; i++ )
			{
				var dir      = HexPos.Directions[i];
				var neighPos		= hex + dir;
				if ( IsContains( neighPos ) )
					yield return this[neighPos];
			}
		}

		public IEnumerable<HexPos> FloodFill(HexPos startFace, Predicate<HexPos> fillCondition = null)
		{
			var distanceEnumerator = new DistanceEnumerator(this, startFace, fillCondition);
			for (var distance = 0; distance < 10; distance++)
			{
				foreach (var position in distanceEnumerator.GetNeighbors(distance))
					yield return position;
			}
		}

		/// <summary>
		/// Get hex coords for given block oords
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

			return x >= 0 && x < _bound.Size.X && y >= 0 && y < _bound.Size.Z;
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
			if(cluster != null)
				foreach ( var hexPos in cluster )
				{
					result.Add( hexPos );
				}

			return result;
		}

#region Layout

		public Vector2 GetHexCenter( HexPos hex )
		{
			return (Vector2)(hex.Q * QBasis + hex.R * RBasis);
		}

		public GridPos GetHexCenterBlock( HexPos hex )
		{
			return (GridPos)GetHexCenter( hex );
		}

		/// <summary>
		/// From (1;1) direction clockwise
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		[NotNull]
		public Vector2[] GetHexVerticesPosition( HexPos hex )
		{
			var result = new Vector2[6];
			var center = GetHexCenter( hex );
			for ( var i = 0; i < 6; i++ )
			{
				var angleDeg =  30 - 60f * i;
				var angleRad = MathHelper.DegreesToRadians( angleDeg );
				result[i] = new Vector2( (center.X + Size * (float)Math.Cos(angleRad)), center.Y + Size * (float)Math.Sin(angleRad) );
			}

			return result;
		}

		public Bound2i GetHexBound( HexPos hex )
		{
			//Get extreme vertices (internal knowledge)
			var vertices = GetHexVerticesPosition( hex );

			var xMax = vertices[0].X;
			var zMin = vertices[2].Y;
			var xMin = vertices[3].X;
			var zMax = vertices[5].Y;

			return new Bound2i(new GridPos(xMin, zMin), new GridPos(xMax, zMax));
		}

		#endregion


		private readonly CelHolder[,] _faces;
		private readonly Vector2d	QBasis ;
		private readonly Vector2d	RBasis ;
		private const           float		Sqrt3  = 1.732050807568877f;
		private readonly Bound2i _bound;

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private CelHolder Set( int q, int r, TCell data )
		{
			var holder = GetOrCreateHolder( q, r );
			holder.Data = data;
			return holder;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private TCell Get( int q, int r )
		{
			var faceHolder = GetHolder( q, r );
			if ( faceHolder != null )
				return faceHolder.Data;

			return default;
		}
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private CelHolder GetHolder( int q, int r )
		{
			CheckHexPosition( q, r );

			var (x, y) = HexToArray2d( q, r );
			return _faces[x, y];
		}

		private CelHolder GetOrCreateHolder( int q, int r )
		{
			CheckHexPosition( q, r );

			var (x, y) = HexToArray2d( q, r );
			var face = _faces[x, y];
			
			if ( face != null )
				return face;

			face = new CelHolder( new HexPos(q, r), this );
			_faces[x, y] = face;
			return face;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal (int x, int y) HexToArray2d( int q, int r )
		{
			var x = q + r / 2	- _bound.Min.X;
			var y = r			- _bound.Min.Z;
			
			return ( x, y );
		}
		
		internal (int q, int r) Array2dToHex( int x, int y )
		{
			var r = y + _bound.Min.Z;
			var q = x - r / 2 + _bound.Min.X;
			
			return ( q, r );
		}
		private void CheckHexPosition( int q, int r )
		{
			var (x, y) = HexToArray2d( q, r );

			if ( x < 0 || x >= _bound.Size.X || y < 0 || y >= _bound.Size.Z ) 
				throw new ArgumentOutOfRangeException( "hex", new HexPos(q, r), 
				                                       "out of range" );
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

		internal CelHolder[,] GetInternalStorage( )
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
			throw new NotImplementedException(  );
		}
		IEnumerator IEnumerable.GetEnumerator( )
		{
			return GetEnumerator( );
		}

		#endregion

		[DebuggerDisplay( "{Pos} = {Data}")]
		public class CelHolder
		{
			public readonly HexPos Pos;
			public TCell Data;
			public readonly EdgeHolder[] Edges = new EdgeHolder[6];
			public readonly VertexHolder[] Vertices = new VertexHolder[6];


			internal CelHolder( HexPos position, HexGrid<TCell, TEdge, TVertex> owner )
			{
				Pos = position;

				//Populate edges
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;

					CelHolder neigh;
					if(owner.IsContains( neighPos ) && (neigh = owner.GetHolder( neighPos.Q, neighPos.R )) != null)
					{
						var oppositeIndex = ( i + 3 ) % 6;
						Edges[i] = neigh.Edges[oppositeIndex];
					}
					else
					{
						Edges[i] = new EdgeHolder( position, i, owner );
					}
				}

				//Populate vertices
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;

					CelHolder neigh;
					if ( owner.IsContains( neighPos ) && ( neigh = owner.GetHolder( neighPos.Q, neighPos.R )) != null )
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;
						var oppositeVert1 = ( i + 4 ) % 6;
						var oppositeVert2 = ( i + 3 ) % 6;

						Vertices[vert1] = neigh.Vertices[oppositeVert1];
						Vertices[vert2] = neigh.Vertices[oppositeVert2];
					}
					else
					{
						var vert1 = i;
						var vert2 = ( i + 1 ) % 6;

						if ( Vertices[vert1] == null )
						{
							Vertices[vert1] = new VertexHolder( position, vert1, owner );
						}

						if ( Vertices[vert2] == null )
						{
							Vertices[vert2] = new VertexHolder( position, vert2, owner );
						}
					}
				}
			}
		}

		[DebuggerDisplay( "{Data}" )]
		public class EdgeHolder
		{
			public readonly HexPos Cell1;
			public readonly int Index;
			public readonly HexPos Cell2;

			public TEdge Data;

			public HexPos OppositeCell( HexPos cell )
			{
				if ( Cell1 == cell )
					return Cell2;
				else if ( Cell2 == cell )
					return Cell1;

				throw new ArgumentOutOfRangeException(  );
			}

			public EdgeHolder( HexPos cell, int index, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Assert.IsTrue( index >= 0 && index < 6);

				Cell1 = cell;
				Index = index;
				_grid = grid;

				Cell2 = cell + HexPos.Directions[index];
			}

			private readonly HexGrid<TCell, TEdge, TVertex> _grid;
		}

		[DebuggerDisplay( "{Data}" )]
		public class VertexHolder
		{
			public readonly HexPos Cell1;
			/// <summary>
			/// Vertex index relatively <see cref="Cell1"/>
			/// </summary>
			public readonly int Index;

			public readonly HexPos Cell2;
			public readonly HexPos Cell3;
			public readonly Vector2 Position;

			public TVertex Data;

			public VertexHolder( HexPos cell, int index, HexGrid<TCell, TEdge, TVertex> grid )
			{
				Assert.IsTrue( index >= 0 && index < 6);

				Cell1 = cell;
				Index = index;
				_grid = grid;

				Cell2 = cell + HexPos.Directions[index];
				Cell3 = cell + HexPos.Directions[(index + 5) % 6];

				Position = grid.GetHexVerticesPosition( cell )[index];
			}

			private readonly HexGrid<TCell, TEdge, TVertex> _grid;
		}

		[DebuggerDisplay( "Edges of {_cel.Pos}")]
		public struct Edges : IReadOnlyList<EdgeHolder>
		{
			//public TEdge this[ int index ]
			//{
			//	get => _cell.Edges[index].Data;
			//	set => _cell.Edges[index].Data = value;
			//}

			public int Count { get; }

			public EdgeHolder this[ HexDir index ]		=>		_cel.Edges[(int)index];

			public IEnumerator<EdgeHolder> GetEnumerator( )
			{
				var edges = _cel.Edges;
				yield return edges[0];
				yield return edges[1];
				yield return edges[2];
				yield return edges[3];
				yield return edges[4];
				yield return edges[5];
			}
			internal Edges( CelHolder cel )
			{
				_cel = cel;
				Count = 6;
			}

			private readonly CelHolder _cel;

			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator( );
			}

			public EdgeHolder this[ int index ]
			{
				get => this[(HexDir) index ];
			}
		}

		[DebuggerDisplay( "Vertices of {_cel.Pos}")]
		public struct Vertices : IReadOnlyList<VertexHolder>
		{
			public int Count { get; }

			public VertexHolder this[ int index ]
			{
				get => _cel.Vertices[index];
				set => _cel.Vertices[index] = value;
			}

			//not applicable
			//public TVertex this[ HexDir index ]
			//{
			//	get => _cell.Vertices[(int)index].Data;
			//	set => _cell.Vertices[(int)index].Data = value;
			//}

			public IEnumerator<VertexHolder> GetEnumerator( )
			{
				var vertices = _cel.Vertices;
				yield return vertices[0];
				yield return vertices[1];
				yield return vertices[2];
				yield return vertices[3];
				yield return vertices[4];
				yield return vertices[5];
			}
			internal Vertices( CelHolder cel )
			{
				_cel = cel;
				Count = 6;
			}

			private readonly CelHolder _cel;
			IEnumerator IEnumerable.GetEnumerator( )
			{
				return GetEnumerator( );
			}
		}
	}
}
