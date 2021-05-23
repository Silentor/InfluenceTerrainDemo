using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using Cell = TerrainDemo.Micro.Cell;
using Vector2 = UnityEngine.Vector2;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Storage for navigational data of land (macro and micro level). 
	/// </summary>
	public class NavigationMap
	{
		public readonly NavGraph NavGraph;
		public readonly Pathfinder Pathfinder;
		public readonly NavigationGrid NavGrid;

		public NavigationMap(MacroMap macromap, MicroMap micromap, TriRunner settings)
		{
			var timer = Stopwatch.StartNew();

			_micromap = micromap;

			NavGraph = new NavGraph(macromap, micromap, settings);
			foreach ( var microcell in micromap.Cells )
			{
				NavGraph[microcell.Id] = CreateNodeFromMicroCell( microcell, micromap, settings );
			}
			
			
			
			
			NavGrid = new NavigationGrid( micromap );
			
			
			
			timer.Stop();

			Pathfinder = new Pathfinder(this, _micromap, settings);

			UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec, macrograph nodes {NavGraph.Count}, macrograph edges {NavGraph.EdgesCount}");
		}

		public NavNode GetNavNode(GridPos position)
		{
			var node = NavGraph.Nodes.FirstOrDefault( n => n.Area.Contains( position ) );
			var node = NavGraph.Nodes.FirstOrDefault( n => n.Area.Contains( position ) );
			return node;
		}

		public Path CreatePath(GridPos from, GridPos to, Actor actor)
		{
			var fromNode = GetNavNode( from );
			var toNode = GetNavNode( to );

			//var pathKey = new PathKey(fromNode, toNode, actor.Locomotor.LocoType);
			//if ( _sharedPathes.TryGetValue( pathKey, out var sharedPath ) )
			//{
			//	UnityEngine.Debug.Log( $"Path {pathKey} for {actor} was finded at cache" );

			//	var result = new Path(from, to, actor, fromNode, toNode, sharedPath, this);
			//	return result;
			//}
			//else
			{
				var newPath = Pathfinder.GetMacroRoute(fromNode, toNode, actor);

				//Prepare shared path
				if(newPath.Route.Count > 0)
					newPath.Route.RemoveAt( 0 );
				if(newPath.Route.Count > 0 && newPath.Route.Last() == toNode)
					newPath.Route.RemoveAt( newPath.Route.Count - 1 );

				var segments = newPath.Route.Select( n => new Path.Segment( n, new GridPos[0] ) ).ToList( );
				var newSharedPath = new PathCacheEntry( segments, newPath.ElapsedTimeMs, newPath.CostsDebug);
				//_sharedPathes[pathKey] = newSharedPath;
				
				

				var result = new Path(from, to, actor, fromNode, toNode, newSharedPath, this);

				UnityEngine.Debug.Log( $"Created new path {result} for {actor}" );

				return result;
			}
		}

		//private readonly Dictionary<PathKey, PathCacheEntry> _sharedPathes = new Dictionary<PathKey, PathCacheEntry>();
		private readonly MicroMap _micromap;
		
		
		
		private struct PathKey : IEquatable<PathKey>
		{
			private readonly NavNode FromNode;
			private readonly NavNode ToNode;
			private readonly BaseLocomotor.Type LocoType;

			public PathKey(NavNode fromNode, NavNode node, BaseLocomotor.Type locoType )
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
		public readonly IReadOnlyDictionary<NavNode, float> CostsDebug;

		public PathCacheEntry( List<Path.Segment> segments, uint elapsedTime, IReadOnlyDictionary<NavNode, float> costsDebug )
		{
			Segments = segments;
			ElapsedTime = elapsedTime;
			CostsDebug = costsDebug;
		}
	}

	//public class NavigationCellBorderNode : NavNode, IEquatable<NavigationCellBorderNode>
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
