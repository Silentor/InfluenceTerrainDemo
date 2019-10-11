using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using Vector2 = UnityEngine.Vector2;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Storage for navigational data of land
	/// </summary>
	public class NavigationMap
	{
		public IReadOnlyDictionary<HexPos, NavigationCell> Nodes => _nodes;
		public readonly NavGraph MacroGraph;
		public readonly Pathfinder Pathfinder;
		public readonly NavigationGrid NavGrid;

		public NavigationMap(MacroMap macromap, MicroMap micromap, TriRunner settings)
		{
			var timer = Stopwatch.StartNew();

			_macromap = macromap;
			_micromap = micromap;

			NavGrid = new NavigationGrid( micromap );

			MacroGraph = new NavGraph();
			var navCells = new List<NavigationCell>();

			//Prepare navigation graph
			foreach (var micromapCell in micromap.Cells)
			{
				var navCell = (NavigationCell)NavigationNodeBase.CreateMicroCellNavigation(micromapCell, micromap, NavGrid, settings);
				MacroGraph.AddNode(navCell);
				navCells.Add(navCell);
				_nodes[micromapCell.Id] = navCell;
			}

			foreach (var fromCell in MacroGraph.Nodes)
			{
				foreach (var neighbor in fromCell.Cell.Macro.NeighborsSafe)
				{
					var toCell = navCells.Find(nc => nc.Cell.Macro == neighbor);
					MacroGraph.AddEdge(fromCell, toCell, new NavEdge(fromCell, toCell));
				}
			}

			foreach ( var edge in MacroGraph.Edges )
			{
				var from     = edge.from.Cell.Macro.CenterPoint;
				var to       = edge.to.Cell.Macro.CenterPoint;
				var distance = Vector2.Distance( @from.Xz, to.Xz );
				var slopeRatio    = ( to.Y - from.Y ) / distance;
				edge.edge.Slopeness = SlopeRatioToLocalIncline( slopeRatio );
				edge.edge.Distance  = distance;
			}

			timer.Stop();

			Pathfinder = new Pathfinder(this, _micromap, settings);

			UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec, macrograph nodes {MacroGraph.NodesCount}, macrograph edges {MacroGraph.EdgesCount}");
		}

		public NavigationCell GetNavNode(GridPos position)
		{
			var microCell = _micromap.GetCell(position);
			return Nodes[microCell.Id];
		}

		public Path CreatePath(GridPos from, GridPos to, Actor actor)
		{
			var fromNode = GetNavNode( from );
			var toNode = GetNavNode( to );

			var pathKey = new PathKey(fromNode.Cell.Id, toNode.Cell.Id, actor.Locomotor.LocoType);
			if ( _sharedPathes.TryGetValue( pathKey, out var sharedPath ) )
			{
				UnityEngine.Debug.Log( $"Path {pathKey} for {actor} was finded at cache" );

				var result = new Path(from, to, actor, fromNode, toNode, sharedPath, this);
				return result;
			}
			else
			{
				var newPath = Pathfinder.GetMacroRoute(fromNode, toNode, actor);

				//Prepare shared path
				newPath.Route.RemoveAt( 0 );
				if(newPath.Route.Last() == toNode)
					newPath.Route.RemoveAt( newPath.Route.Count - 1 );

				var segments = newPath.Route.Select( n => new Path.Segment( n, new GridPos[0] ) ).ToList( );
				var newSharedPath = new PathCacheEntry( segments, newPath.ElapsedTimeMs, newPath.CostsDebug);
				_sharedPathes[pathKey] = newSharedPath;
				
				UnityEngine.Debug.Log( $"Created new path {pathKey} for {actor}" );

				var result = new Path(from, to, actor, fromNode, toNode, newSharedPath, this);
				return result;
			}
		}

		private readonly Dictionary<HexPos, NavigationCell> _nodes = new Dictionary<HexPos, NavigationCell>();
		private readonly Dictionary<PathKey, PathCacheEntry> _sharedPathes = new Dictionary<PathKey, PathCacheEntry>();

		private readonly MicroMap _micromap;
		private readonly MacroMap _macromap;


		internal static readonly float MostlyFlat  = MathHelper.DegreesToRadians(10);
		internal static readonly float SmallSlope  = MathHelper.DegreesToRadians(40);
		internal static readonly float MediumSlope = MathHelper.DegreesToRadians(70);
		internal static readonly float SteepSlope  = MathHelper.DegreesToRadians(90);


		private static readonly float MostyFlatSin = (float)Math.Sin(MostlyFlat);
		private static readonly float SmallSlopeSin = (float)Math.Sin(SmallSlope);
		private static readonly float MediumSlopeSin = (float)Math.Sin(MediumSlope);
		private static readonly float SteepSlopeSin = (float)Math.Sin(SteepSlope);

		internal static LocalIncline SlopeRatioToLocalIncline( float slopeRatio )
		{
			if ( Math.Abs( slopeRatio ) < MostyFlatSin )
				return LocalIncline.Flat;
			else if ( slopeRatio > 0 )
			{
				if ( slopeRatio < SmallSlopeSin )
					return LocalIncline.SmallUphill;
				else if ( slopeRatio < MediumSlopeSin )
					return LocalIncline.MediumUphill;
				else
					return LocalIncline.SteepUphill;
			}
			else
			{
				slopeRatio = -slopeRatio;
				if (slopeRatio < SmallSlopeSin)
					return LocalIncline.SmallDownhill;
				else if (slopeRatio < MediumSlopeSin)
					return LocalIncline.MediumDownhill;
				else
					return LocalIncline.SteepDownhill;
			}
		}

		internal static Incline AngleToIncline( float angleRad )
		{
			if (angleRad < NavigationMap.MostlyFlat)
				return Incline.Flat;
			else if (angleRad < NavigationMap.SmallSlope)
				return Incline.Small;
			else if (angleRad < NavigationMap.MediumSlope)
				return Incline.Medium;
			else
				return Incline.Steep;
		}

		private struct PathKey : IEquatable<PathKey>
		{
			private readonly HexPos FromNode;
			private readonly HexPos ToNode;
			private readonly BaseLocomotor.Type LocoType;

			public PathKey( HexPos fromNode, HexPos node, BaseLocomotor.Type locoType )
			{
				FromNode = fromNode;
				ToNode = node;
				LocoType = locoType;
			}
			public bool Equals( PathKey other )
			{
				return FromNode.Equals( other.FromNode ) && ToNode.Equals( other.ToNode ) && LocoType == other.LocoType;
			}
			public override bool Equals( object obj )
			{
				return obj is PathKey other && Equals( other );
			}
			public override int GetHashCode( )
			{
				unchecked
				{
					var hashCode = FromNode.GetHashCode( );
					hashCode = ( hashCode * 397 ) ^ ToNode.GetHashCode( );
					hashCode = ( hashCode * 397 ) ^ (int) LocoType;
					return hashCode;
				}
			}
			public static bool operator ==( PathKey left, PathKey right )
			{
				return left.Equals( right );
			}
			public static bool operator !=( PathKey left, PathKey right )
			{
				return !left.Equals( right );
			}

			public override string ToString( )
			{
				return $"{FromNode}-{ToNode} ({LocoType})";
			}
		}
	}

	public class PathCacheEntry
	{
		public readonly List<Path.Segment> Segments;
		public readonly uint ElapsedTime;	
		public readonly IReadOnlyDictionary<NavigationCell, float> CostsDebug;

		public PathCacheEntry( List<Path.Segment> segments, uint elapsedTime, IReadOnlyDictionary<NavigationCell, float> costsDebug )
		{
			Segments = segments;
			ElapsedTime = elapsedTime;
			CostsDebug = costsDebug;
		}
	}

	//public class NavigationCellBorderNode : NavigationNodeBase, IEquatable<NavigationCellBorderNode>
	//{
	// public MacroEdge Edge { get; }
	// public readonly Vector2i Position;

	// public NavigationCellBorderNode( Macro.MacroEdge edge ) : base( 1, Vector3.UnitY, 0.5f )
	// {
	//  Edge = edge;
	//  Position = (Vector2i)(( edge.Vertex1.Position + edge.Vertex2.Position ) / 2);
	// }
	//    public bool Equals( NavigationCellBorderNode other )
	//    {
	//        if ( ReferenceEquals( null, other ) ) return false;
	//        if ( ReferenceEquals( this, other ) ) return true;

	//        return Equals( Edge, other.Edge );
	//    }
	//    public override bool Equals( object obj )
	//    {
	//        if ( ReferenceEquals( null, obj ) ) return false;
	//        if ( ReferenceEquals( this, obj ) ) return true;
	//        if ( obj.GetType( ) != this.GetType( ) ) return false;

	//        return Equals( (NavigationCellBorderNode) obj );
	//    }
	//    public override int GetHashCode( )
	//    {
	//        return ( Edge != null ? Edge.GetHashCode( ) : 0 );
	//    }
	//    public static bool operator ==( NavigationCellBorderNode left, NavigationCellBorderNode right )
	//    {
	//        return Equals( left, right );
	//    }
	//    public static bool operator !=( NavigationCellBorderNode left, NavigationCellBorderNode right )
	//    {
	//        return !Equals( left, right );
	//    }
	//}


}
