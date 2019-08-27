using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public IReadOnlyDictionary<Coord, NavigationCell> Nodes => _nodes;
		public readonly NavGraph MacroGraph;
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

			Pathfinder = new Pathfinder(this, settings);

			UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec, macrograph nodes {MacroGraph.NodesCount}, macrograph edges {MacroGraph.EdgesCount}");
		}

		public NavigationCell GetNavNode(GridPos position)
		{
			var microCell = _micromap.GetCell(position);
			return Nodes[microCell.Id];
		}

		public Path CreatePath(GridPos from, GridPos to, Actor actor)
		{
			return new Path(from, to, actor, this);
		}

		private readonly Dictionary<Coord, NavigationCell> _nodes = new Dictionary<Coord, NavigationCell>();
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
