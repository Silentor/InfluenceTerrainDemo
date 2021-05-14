using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Cell = TerrainDemo.Micro.Cell;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Macro level navigation: A* compartible graph of macro cells. NavNode = macro cell, NavEdge - 6 edges of macro cell
	/// todo implement more detailed NavGraph - NavNode = each edge of macro cell, so we can create a much more detailed macro path. But amount of nodes/edges is much more then in former case
	/// </summary>
	public class NavGraph : HexGrid<NavNode, NavEdge, NavVertex>
	{
		public NavGraph( MacroMap macromap, MicroMap micromap, TriRunner settings ) : base( settings.CellSide, (int)settings.LandSize )
		{
			var navNodes = new List<NavNode>();

			// //Prepare navigation graph
			// foreach (var micromapCell in micromap.Cells)
			// {
			// 	var navCell = CreateNodeFromMicroCell(micromapCell, micromap, settings);
			// 	this[ micromapCell.Id] =  navCell;
			// 	navNodes.Add(navCell);
			// }

			перенести в NavigationGraph и переделать под HexEdge (этот эдж разделяемый)
			
			for ( var i = 0; i < navNodes.Count; i++ )
			{
				var fromCell = micromap.Cells [ i ];
				var fromNode = navNodes [ i ];
				foreach ( var neighbor in fromCell.Macro.NeighborsSafe )
				{
					var toIndex = macromap.Cells.IndexOf(neighbor);
					var toNode = navNodes[toIndex];
					AddEdge ( fromNode, toNode, new NavEdge ( fromNode, toNode ) );
				}
			}

			foreach (var edge in Edges)
			{
				var from       = edge.from.Position3d;
				var to         = edge.to.Position3d;
				var distance   = Vector2.Distance(@from.Xz, to.Xz);
				var slopeRatio = (to.Y - from.Y) / distance;
				edge.edge.Slopeness = CreateIncline.FromSlope( slopeRatio );
				edge.edge.Distance  = distance;
			}
		}

		public IEnumerable<(NavNode neighbor, float neighborCost)> Neighbors( BaseLocomotor loco, NavNode node )
		{
			foreach ( var (edge, neighbor) in GetNeighbors(node) )
			{
				var edgeSlopeCost = loco.GetInclineCost( edge.Slopeness );
				var roughnessCost = loco.GetRoughnessCost( edge.Roughness );
				var speedCost = neighbor.MaterialCost;

				var result = edge.Distance * edgeSlopeCost * roughnessCost * speedCost;
				if ( float.IsNaN( result ) )
					continue;

				yield return (neighbor, Math.Max(result, 0));
			}
		}
		public float Heuristic( [NotNull] NavNode @from, [NotNull] NavNode to )
		{
			return Vector3.Distance( from.Position3d, to.Position3d );
		}

		
	}

	
    public class NavNode : IEquatable<NavNode>
    {
	    public readonly HexPos Position;
    
	    /// <summary>
	    /// Average mat cost of all node blocks
	    /// </summary>
	    public readonly float MaterialCost;

	    /// <summary>
	    /// Average normal of all node blocks
	    /// </summary>
	    public readonly Vector3 Normal; //?

		/// <summary>
		/// 0 - smooth as silk, 1 - extreme micro relief
		/// </summary>
		public readonly float Rougness;

		/// <summary>
		/// Nav node center point
		/// </summary>
		public readonly Vector3 Position3d;     //Mostly for debug visualization

		/// <summary>
		/// Nav node center point
		/// </summary>
		public readonly GridPos CenterPosition;		//todo consider hide position, NavNode more like a navigable area

		public readonly GridArea Area;

		internal NavNode( HexPos position, float materialCost, Vector3 normal, float rougness, Vector3 centerPosition, GridArea area, string debugName)
		{
			Position       = position;
		    MaterialCost   = materialCost;
		    Normal         = normal.Normalized();
		    Rougness       = rougness;
		    Position3d     = centerPosition;
		    CenterPosition = (GridPos) centerPosition;
		    Area           = area;
		    _debugName     = debugName;
	    }

	    public override string ToString()
	    {
		    return _debugName;
	    }

	    public bool Equals(NavNode other)
	    {
		    if (other is null) return false;
		    return  ReferenceEquals(this, other);
	    }

	    private readonly String _debugName;
    }

    public class NavEdge
    {
	    public float				Distance;
	    public LocalIncline			Slopeness;
	    public float				Roughness				=> To.Rougness;

	    public readonly NavNode From;
	    public readonly NavNode To;

	    internal NavEdge( NavNode from, NavNode to )
	    {
		    From = from;
		    To   = to;
	    }
    }

    public class NavVertex
    {
	    
    }
}