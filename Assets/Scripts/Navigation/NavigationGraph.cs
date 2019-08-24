using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Navigation
{
	public class NavGraph : Graph<NavigationCell, NavEdge>, IWeightedGraph<NavigationCell>
	{
		public IEnumerable<(NavigationCell neighbor, float neighborCost)> Neighbors( Locomotor loco, NavigationCell node )
		{
			foreach ( var (edge, neighbor) in GetNeighbors(node) )
			{
				var edgeSlopeCost = loco.GetCost( edge.Slopeness );
				var roughnessCost = neighbor.Rougness;
				var speedCost = neighbor.SpeedModifier;

				var result = edge.Distance * edgeSlopeCost * roughnessCost * speedCost;
				if ( float.IsNaN( result ) )
					continue;

				yield return (neighbor, Math.Max(result, 0));
			}
		}
		public float Heuristic( NavigationCell @from, NavigationCell to )
		{
			return Vector2.Distance( from.Cell.Macro.Center, to.Cell.Macro.Center );
		}
        
	}

	public class NavigationCell : NavigationNodeBase, IEquatable<NavigationCell>
    {
        public readonly Cell Cell;

        public NavigationCell([NotNull] Cell cell, float speedModifier, Vector3 normal, float rougness) : base(speedModifier, normal, rougness)
        {
            Cell = cell ?? throw new ArgumentNullException(nameof(cell));
        }

        public override string ToString()
        {
            return $"<{Cell.Id}>";
        }

        public bool Equals(NavigationCell other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Cell.Equals(other.Cell);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((NavigationCell) obj);
        }

        public override int GetHashCode()
        {
            return Cell.GetHashCode();
        }

        public static bool operator ==(NavigationCell left, NavigationCell right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NavigationCell left, NavigationCell right)
        {
            return !Equals(left, right);
        }
    }

    public abstract class NavigationNodeBase
    {
	    /// <summary>
	    /// Average speed modifier of all blocks
	    /// </summary>
	    public readonly float SpeedModifier;

	    /// <summary>
	    /// Average normal of all blocks
	    /// </summary>
	    public readonly Vector3 Normal;

		/// <summary>
		/// Ratio of hard to pass blocks (steep slope or impassable) to total blocks count
		/// </summary>
		public readonly float Rougness;

	    protected NavigationNodeBase(float speedModifier, Vector3 normal, float rougness)
	    {
		    SpeedModifier = speedModifier;
		    Normal        = normal.Normalized();
		    Rougness      = rougness;
	    }

	    public static NavigationNodeBase CreateMicroCellNavigation( Micro.Cell cell, MicroMap map, NavigationGrid navGrid, TriRunner settings )
	    {
		    var avgSpeedModifier = 0f;
		    var avgNormal        = OpenToolkit.Mathematics.Vector3.Zero;
		    var normalDeviation = 0f;
		    float roughness = 0;

			foreach (var blockPosition in cell.BlockPositions)
		    {
			    //Calculate average movement cost for cell
			    ref readonly var block = ref map.GetBlockRef(blockPosition);
			    avgSpeedModifier += settings.AllBlocksDict[block.Top].SpeedModifier;

			    //Calculate average normal
			    ref readonly var blockData = ref map.GetBlockData(blockPosition);
			    avgNormal += blockData.Normal;

			    ref readonly var navBlock = ref navGrid.GetBlock(blockPosition);
			    switch (navBlock.Normal.Slope)
			    {
				    case Incline.Flat:   roughness += 1; break;
				    case Incline.Small:  roughness += 2; break;
				    case Incline.Medium: roughness += 10; break;
				    case Incline.Steep:  roughness += 100; break;
				    default:             throw new ArgumentOutOfRangeException();
			    }

			}

			avgSpeedModifier /= cell.BlockPositions.Length;
		    avgNormal        =  (avgNormal / cell.BlockPositions.Length).Normalized();
		    roughness /= cell.BlockPositions.Length;

			//float normalDispersion = 0f;

			//foreach (var blockPosition in cell.BlockPositions)
			{
			    //Calculate micro rougness of cell
			    //ref readonly var blockData = ref map.GetBlockData(blockPosition);
			    //var              disp      = Vector3.CalculateAngle(blockData.Normal, avgNormal);
			    //normalDispersion += disp * disp;
		    }

		    //normalDeviation = Mathf.Sqrt(normalDispersion / cell.BlockPositions.Length);

			return new NavigationCell( cell, avgSpeedModifier, avgNormal, roughness );
        }

    }

    public class NavEdge
    {
	    public float Distance;
	    public LocalIncline Slopeness;

	    public readonly NavigationCell From;
	    public readonly NavigationCell To;

	    public NavEdge( NavigationCell from, NavigationCell to )
	    {
		    From = from;
		    To = to;
	    }
    }
}