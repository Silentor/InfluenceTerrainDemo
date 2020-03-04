using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using UnityEditor;

namespace TerrainDemo.Spatial
{
	/// <summary>
	/// Regular hexagonal grid (top-pointed) optimized to block rasterization
	/// </summary>
	public class HexGrid<TFace, TEdge, TVertex>
	{
		public readonly float Size;
		public readonly float Width;
		public readonly float Height;

		public HexGrid( float hexSide, int gridRadius )
		{
			Size = hexSide;
			Width = Sqrt3 * Size ;
			Height = Size * 2;
			QBasis = new Vector2d(Width,     0);
			RBasis = new Vector2d(Width / 2, 3d /4 *Height);

			_bound = new Bounds2i(GridPos.Zero, gridRadius);
			_faces = new FaceHolder[_bound.Size.X, _bound.Size.Z];
		}

		public TFace this[ HexPos position ]
		{
			get => Get( position.Q, position.R );
			set => Set( position.Q, position.R, value );
		}

		public TFace this[ int q, int r ]
		{
			get => Get( q, r );
			set => Set( q, r, value );
		}

		public Edges GetEdges( HexPos hex )
		{
			if ( IsContains( hex ) )
			{
				var holder = GetHolder( hex.Q, hex.R );
				return new Edges( holder );
			}

			throw new ArgumentOutOfRangeException( nameof(hex), hex, "out of range or null" );
		}

		public Vertices GetVertices( HexPos hex )
		{
			if ( IsContains( hex ) )
			{
				var holder = GetHolder( hex.Q, hex.R );
				return new Vertices( holder );
			}

			throw new ArgumentOutOfRangeException( nameof(hex), hex, "out of range or null" );
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

			return x >= 0 && x < _bound.Size.X && y >= 0 && y < _bound.Size.Z && _faces[x, y] != null;
		}

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

		public Bounds2i GetHexBound( HexPos hex )
		{
			//Get extreme vertices (internal knowledge)
			var vertices = GetHexVerticesPosition( hex );

			var xMax = vertices[0].X;
			var zMin = vertices[2].Y;
			var xMin = vertices[3].X;
			var zMax = vertices[5].Y;

			return new Bounds2i(new GridPos(xMin, zMin), new GridPos(xMax, zMax));
		}

		private readonly FaceHolder[,] _faces;
		private readonly Vector2d	QBasis ;
		private readonly Vector2d	RBasis ;
		private const           float		Sqrt3  = 1.732050807568877f;
		private readonly Bounds2i _bound;

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private void Set( int q, int r, TFace data )
		{
			var (x, y) = HexToArray2d( q, r );
			
			if ( _faces[x, y] != null )
				_faces[x, y].Data = data;
			else
			{
				var newFace = new FaceHolder( new HexPos(q, r), this );
				newFace.Data = data;
				_faces[x, y] = newFace;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private TFace Get( int q, int r )
		{
			var (x, y) = HexToArray2d( q, r );

			if ( _faces[x, y] == null )
			{
				return default;
			}

			return _faces[x, y].Data;
		}
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		private FaceHolder GetHolder( int q, int r )
		{
			var (x, y) = HexToArray2d( q, r );
			return _faces[x, y];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (int x, int y) HexToArray2d( int q, int r )
		{
			var x = q + r / 2	- _bound.Min.Z;
			var y = r			- _bound.Min.Z;
			
			return ( x, y );
		}
		internal FaceHolder[,] GetInternalStorage( )
		{
			return _faces;
		}

		//[DebuggerDisplay( "{Pos} = {Data}")]
		public class FaceHolder
		{
			public readonly HexPos Pos;
			public TFace Data;
			public readonly EdgeHolder[] Edges = new EdgeHolder[6];
			public readonly VertexHolder[] Vertices = new VertexHolder[6];

			internal FaceHolder( HexPos position, HexGrid<TFace, TEdge, TVertex> owner )
			{
				Pos = position;

				//Populate edges
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;
					var neigh    = owner.GetHolder( neighPos.Q, neighPos.R );
					if ( neigh != null )
					{
						var oppositeIndex = ( i + 3 ) % 6;
						Edges[i] = neigh.Edges[oppositeIndex];
					}
					else
					{
						Edges[i] = new EdgeHolder(  );
					}
				}

				//Populate vertices
				for ( var i = 0; i < HexPos.Directions.Length; i++ )
				{
					var dir      = HexPos.Directions[i];
					var neighPos = position + dir;
					var neigh    = owner.GetHolder( neighPos.Q, neighPos.R );
					if ( neigh != null )
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

						if(Vertices[vert1] == null)
							Vertices[vert1] = new VertexHolder(  );
						if(Vertices[vert2] == null)
							Vertices[vert2] = new VertexHolder(  );
					}
				}
			}
		}

		[DebuggerDisplay( "{Data}")]
		public class EdgeHolder
		{
			public TEdge Data;
		}

		[DebuggerDisplay("{Data}")]
		public class VertexHolder
		{
			public TVertex Data;
		}

		[DebuggerDisplay( "Edges of {_face.Pos}")]
		public struct Edges 
		{
			public TEdge this[ int index ]
			{
				get => _face.Edges[index].Data;
				set => _face.Edges[index].Data = value;
			}

			public TEdge this[ HexDir index ]
			{
				get => _face.Edges[(int)index].Data;
				set => _face.Edges[(int)index].Data = value;
			}

			internal Edges( FaceHolder face )
			{
				_face = face;
			}

			private readonly FaceHolder _face;
		}

		[DebuggerDisplay( "Vertices of {_face.Pos}")]
		public struct Vertices
		{
			public TVertex this[ int index ]
			{
				get => _face.Vertices[index].Data;
				set => _face.Vertices[index].Data = value;
			}

			public TVertex this[ HexDir index ]
			{
				get => _face.Vertices[(int)index].Data;
				set => _face.Vertices[(int)index].Data = value;
			}

			internal Vertices( FaceHolder face )
			{
				_face = face;
			}

			private readonly FaceHolder _face;
		}
	}
}
