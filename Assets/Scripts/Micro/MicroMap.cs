using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Chunk-based grid-map of land, based on <see cref="MacroMap"/>
    /// </summary>
    public class MicroMap
    {
        public readonly Bounds2i Bounds;
        public readonly Cell[] Cells;

        public MicroMap(MacroMap macromap, TriRunner settings)
        {
            Bounds = (Bounds2i) macromap.Bounds;

            _macromap = macromap;
            _settings = settings;

            Cells = new Cell[macromap.Cells.Count];
            for (var i = 0; i < macromap.Cells.Count; i++)
            {
                var macroCell = macromap.Cells[i];
                var microCell = new Cell(macroCell, this);
                Cells[i] = microCell;
            }

            _heightMap = new Heights[Bounds.Size.X + 1, Bounds.Size.Z + 1];
            _blocks = new Blocks[Bounds.Size.X, Bounds.Size.Z];

            Debug.LogFormat("Generated micromap {0} x {1} = {2} blocks", Bounds.Size.X, Bounds.Size.Z, Bounds.Size.X * Bounds.Size.Z);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if(cell.Map != _macromap) throw new ArgumentException();

            return Cells[_macromap.Cells.IndexOf(cell)];
        }

        public void SetHeights(IEnumerable<Vector2i> positions, IEnumerable<Heights> heights)
        {
            var posEnumerator = positions.GetEnumerator();
            var infEnumerator = heights.GetEnumerator();
            using (posEnumerator)
            {
                using (infEnumerator)
                {
                    while (posEnumerator.MoveNext() & infEnumerator.MoveNext())
                    {
                        var arrayX = posEnumerator.Current.X - Bounds.Min.X;
                        var arrayZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _heightMap[arrayX, arrayZ] = infEnumerator.Current;
                    }
                }
            }
        }

        public void SetBlocks(IEnumerable<Vector2i> positions, IEnumerable<Blocks> blocks)
        {
            var posEnumerator = positions.GetEnumerator();
            var blockEnumerator = blocks.GetEnumerator();
            using (posEnumerator)
            {
                using (blockEnumerator)
                {
                    while (posEnumerator.MoveNext() & blockEnumerator.MoveNext())
                    {
                        var localX = posEnumerator.Current.X - Bounds.Min.X;
                        var localZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _blocks[localX, localZ] = blockEnumerator.Current;
                    }
                }
            }
        }

        public Heights[,] GetHeightMap()
        {
            return _heightMap;
        }

        public Blocks[,] GetBlockMap()
        {
            return _blocks;
        }

        public BlockInfo GetBlock(Vector2i blockPos)
        {
            if (!Bounds.Contains(blockPos))
                return null;

            var localPos = blockPos - Bounds.Min;
            var result = new BlockInfo(blockPos, _blocks[localPos.X, localPos.Z], 
                _heightMap[localPos.X, localPos.Z],
                _heightMap[localPos.X, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z]);
            return result;
        }

        /*
        /// <summary>
        /// Clockwise
        /// </summary>
        /// <param name="blockPos"></param>
        /// <returns></returns>
        public ValueTuple<MicroHeight, MicroHeight, MicroHeight, MicroHeight> GetBlockVertices(Vector2i blockPos)
        {
            var localPos = blockPos - Bounds.Min;

            return new ValueTuple<MicroHeight, MicroHeight, MicroHeight, MicroHeight>(
                _heightMap[localPos.X, localPos.Z],
                _heightMap[localPos.X, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z]);
        }
        */

        private readonly MacroMap _macromap;
        private readonly TriRunner _settings;
        private readonly Heights[,] _heightMap;
        private readonly Blocks[,] _blocks;
        //private readonly double[,][] _influences;
    }
}
