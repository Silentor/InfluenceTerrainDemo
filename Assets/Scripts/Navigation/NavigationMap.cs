using System.Collections.Generic;
using System.Diagnostics;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Storage for navigational data of land
	/// </summary>
	public class NavigationMap
	{
		public IReadOnlyDictionary<Coord, NavigationCell> Nodes => _nodes;
		public readonly NavGraph MacroGraph;

		public NavigationMap(MacroMap macromap, MicroMap micromap, TriRunner settings)
		{
			var timer = Stopwatch.StartNew();

			_macromap = macromap;
			_micromap = micromap;
			MacroGraph = new NavGraph();
			var navCells = new List<NavigationCell>();

			//Prepare navigation graph
			foreach (var micromapCell in micromap.Cells)
			{
				var navCell = (NavigationCell)NavigationNodeBase.CreateMicroCellNavigation(micromapCell, micromap, settings);
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
				var slope    = ( to.Y - from.Y ) / distance;
				edge.edge.Slopeness = slope;
				edge.edge.Distance  = distance;
			}

			timer.Stop();

			Pathfinder = new Pathfinder(this, settings);

			UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec, macrograph nodes {MacroGraph.NodesCount}, macrograph edges {MacroGraph.EdgesCount}");
		}

		public NavigationCell GetNavNode(Vector2i position)
		{
			var microCell = _micromap.GetCell(position);
			return Nodes[microCell.Id];
		}

		public Path CreatePath(Vector2i from, Vector2i to, Actor actor)
		{
			return new Path(from, to, actor, this);
		}

		private readonly Dictionary<Coord, NavigationCell> _nodes = new Dictionary<Coord, NavigationCell>();
		private readonly MicroMap _micromap;

		private readonly MacroMap _macromap;
		public readonly Pathfinder Pathfinder;
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
