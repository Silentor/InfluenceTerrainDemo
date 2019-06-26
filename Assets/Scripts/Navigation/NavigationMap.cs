using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Cell = TerrainDemo.Micro.Cell;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Storage for navigational data of land
    /// </summary>
    public class NavigationMap
    {
        public IReadOnlyDictionary<Coord, NavigationCell> Cells => _cells;
		public Graph<NavigationCellBase, bool> MacroMap = new Graph<NavigationCellBase, bool>(  );

        private readonly MacroMap _macromap;
        private readonly MicroMap _micromap;
        private readonly Dictionary<Coord, NavigationCell> _cells = new Dictionary<Coord, NavigationCell>();

        public NavigationMap(MacroMap macromap, MicroMap micromap, TriRunner settings)
        {
            var timer = Stopwatch.StartNew();

            _macromap = macromap;
            _micromap = micromap;
			MacroMap = new Graph<NavigationCellBase, bool>(  );

            foreach ( var micromapCell in micromap.Cells )
            {
	            var navCell = NavigationCellBase.CreateMicroCellNavigation( micromapCell, micromap, settings );
	            MacroMap.AddNode( navCell );
            }


            timer.Stop();

            UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec");
        }
    }

    public class NavigationCell : NavigationCellBase, IEquatable<NavigationCell>
    {
        public readonly Cell Cell;

        public NavigationCell([NotNull] Cell cell, float speedModifier, Vector3 normal, float rougness) : base(speedModifier, normal, rougness)
        {
            Cell = cell ?? throw new ArgumentNullException(nameof(cell));
        }

        public override string ToString()
        {
            return $"{Cell.Id}";
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

    public abstract class NavigationCellBase
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
	    /// Standart deviation of block's normals (in radians)
	    /// </summary>
	    public readonly float Rougness;

	    protected NavigationCellBase(float speedModifier, Vector3 normal, float rougness)
	    {
		    SpeedModifier = speedModifier;
		    Normal        = normal.Normalized();
		    Rougness      = rougness;
	    }

	    public static NavigationCellBase CreateMicroCellNavigation( Micro.Cell cell, MicroMap map, TriRunner settings )
	    {
		    var avgSpeedModifier = 0f;
		    var avgNormal        = Vector3.Zero;
		    var normalDeviation = 0f;

		    foreach (var blockPosition in cell.BlockPositions)
		    {
			    //Calculate average movement cost for cell
			    ref readonly var block = ref map.GetBlockRef(blockPosition);
			    avgSpeedModifier += settings.AllBlocksDict[block.Top].SpeedModifier;

			    //Calculate average normal
			    ref readonly var blockData = ref map.GetBlockData(blockPosition);
			    avgNormal += blockData.Normal;
		    }

		    avgSpeedModifier /= cell.BlockPositions.Length;
		    avgNormal        =  (avgNormal / cell.BlockPositions.Length).Normalized();

		    float normalDispersion = 0f;
		    foreach (var blockPosition in cell.BlockPositions)
		    {
			    //Calculate micro rougness of cell
			    ref readonly var blockData = ref map.GetBlockData(blockPosition);
			    var              disp      = Vector3.CalculateAngle(blockData.Normal, avgNormal);
			    normalDispersion += disp * disp;
		    }

		    normalDeviation = Mathf.Sqrt(normalDispersion / cell.BlockPositions.Length);

			return new NavigationCell( cell, avgSpeedModifier, avgNormal, normalDeviation );
        }

    }


}
