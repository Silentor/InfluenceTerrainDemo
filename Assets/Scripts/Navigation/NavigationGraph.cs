using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Cell = TerrainDemo.Micro.Cell;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Macro level navigation: A* compartible graph of macro cells. NavNode = macro cell, NavEdge - 6 edges of macro cell
	/// todo implement more detailed NavGraph - NavNode = each edge of macro cell, so we can create a much more detailed macro path. But amount of nodes/edges is much more then in former case
	/// </summary>
	public class NavGraph : Graph<NavNode, NavEdge>, IAStarGraph<NavNode>
	{
		public NavGraph( MacroMap macromap, MicroMap micromap, TriRunner settings )
		{
			var navNodes = new List<NavNode>();

			//Prepare navigation graph
			foreach (var micromapCell in micromap.Cells)
			{
				var navCell = CreateNodeFromMicroCell(micromapCell, micromap, settings);
				AddNode(navCell);
				navNodes.Add(navCell);
			}

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

		private static NavNode CreateNodeFromMicroCell(Cell cell, MicroMap map, TriRunner settings)
		{
			var   materialCost    = 0f;
			var   avgNormal       = Vector3.Zero;
			var   normalDeviation = 0f;
			float roughness       = 0;
			var totalBlocks = 0;

			foreach (var blockPosition in cell.BlockPositions)
			{
				//Calculate average material cost for cell
				ref readonly var block = ref map.GetBlockRef(blockPosition);
				materialCost += settings.AllBlocksDict[block.Top].MaterialCost;

				//Calculate average normal
				ref readonly var blockData = ref map.GetBlockData(blockPosition);
				avgNormal += blockData.Normal;
				totalBlocks++;
			}

			materialCost /= totalBlocks;
			avgNormal    =  (avgNormal / totalBlocks).Normalized();
			roughness    /= totalBlocks;

			//float normalDispersion = 0f;

			//foreach (var blockPosition in cell.BlockPositions)
			{
				//Calculate micro rougness of cell
				//ref readonly var blockData = ref map.GetBlockData(blockPosition);
				//var              disp      = Vector3.CalculateAngle(blockData.Normal, avgNormal);
				//normalDispersion += disp * disp;
			}

			//normalDeviation = Mathf.Sqrt(normalDispersion / cell.BlockPositions.Length);

			var center = cell.Macro.Center;
			ref readonly var centerData = ref map.GetBlockData(cell.Center);

			return new NavNode(materialCost, avgNormal, roughness, new Vector3(center.X, centerData.Height, center.Y), cell.BlockPositions, cell.Id.ToString (  ));
		}
	}

	
    public class NavNode : IEquatable<NavNode>
    {
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
		public readonly GridPos Position;

		public readonly GridArea Area;

		internal NavNode(float materialCost, Vector3 normal, float rougness, Vector3 position, GridArea area, string debugName)
	    {
		    MaterialCost = materialCost;
		    Normal        = normal.Normalized();
		    Rougness      = rougness;
		    Position3d		= position;
		    Position		= (GridPos) position;
		    Area = area;
		    _debugName		= debugName;
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
}