using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
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

        private readonly MacroMap _macromap;
        private readonly MicroMap _micromap;
        private readonly Dictionary<Coord, NavigationCell> _cells = new Dictionary<Coord, NavigationCell>();

        public NavigationMap(MacroMap macromap, MicroMap micromap, TriRunner settings)
        {
            var timer = Stopwatch.StartNew();

            _macromap = macromap;
            _micromap = micromap;

            var blockProperties = new Dictionary<BlockType, float>();
            foreach (var blockSettings in settings.AllBlocks)
            {
                blockProperties[blockSettings.Block] = blockSettings.SpeedModifier;
            }

            //Prepare navigation map
            foreach (var micromapCell in _micromap.Cells)
            {
                var avgSpeedModifier = 0f;
                var avgNormal = Vector3.Zero;
                
                foreach (var blockPosition in micromapCell.BlockPositions)
                {
                    //Calculate average movement cost for cell
                    ref readonly var block = ref _micromap.GetBlockRef(blockPosition);
                    avgSpeedModifier += blockProperties[block.Top];

                    //Calculate average normal
                    ref readonly var blockData = ref _micromap.GetBlockData(blockPosition);
                    avgNormal += blockData.Normal;
                }

                avgSpeedModifier /= micromapCell.BlockPositions.Length;
                avgNormal = (avgNormal / micromapCell.BlockPositions.Length).Normalized();

                float normalDispersion = 0f;
                foreach (var blockPosition in micromapCell.BlockPositions)
                {
                    //Calculate micro rougness of cell
                    ref readonly var blockData = ref _micromap.GetBlockData(blockPosition);
                    var disp = Vector3.CalculateAngle(blockData.Normal, avgNormal);
                    normalDispersion += disp * disp;
                }

                var normalDeviation = Mathf.Sqrt(normalDispersion / micromapCell.BlockPositions.Length);

                _cells.Add(micromapCell.Id, new NavigationCell(micromapCell, avgSpeedModifier, avgNormal, normalDeviation));
            }

            timer.Stop();

            UnityEngine.Debug.Log($"Prepared navigation map in {timer.ElapsedMilliseconds} msec");
        }
    }

    public class NavigationCell : IEquatable<NavigationCell>
    {
        public readonly Micro.Cell Cell;

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

        public NavigationCell([NotNull] Cell cell, float speedModifier, Vector3 normal, float rougness)
        {
            Cell = cell ?? throw new ArgumentNullException(nameof(cell));
            SpeedModifier = speedModifier;
            Normal = normal;
            Rougness = rougness;
        }

        public bool Equals(NavigationCell other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Cell, other.Cell);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NavigationCell) obj);
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
}
