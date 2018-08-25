using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Macro;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

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
                var cellBlocks = Rasterization.Polygon2(macroCell.Contains, macroCell.Bounds);
                var microCell = new Cell(macroCell, cellBlocks.ToArray());
                Cells[i] = microCell;
            }

            _heightMap = new float[Bounds.Size.X + 1, Bounds.Size.Z + 1];
            _blocks = new BlockInfo[Bounds.Size.X, Bounds.Size.Z];
            _influences = new double[Bounds.Size.X, Bounds.Size.Z][];

            Debug.LogFormat("Generated micromap {0} x {1} = {2} blocks", Bounds.Size.X, Bounds.Size.Z, Bounds.Size.X * Bounds.Size.Z);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if(cell.Map != _macromap) throw new ArgumentException();

            return Cells[_macromap.Cells.IndexOf(cell)];
        }

        public void SetInfluence(IEnumerable<Vector2i> positions, IEnumerable<double[]> influences)
        {
            var posEnumerator = positions.GetEnumerator();
            var infEnumerator = influences.GetEnumerator();
            using (posEnumerator)
            {
                using (infEnumerator)
                {
                    while (posEnumerator.MoveNext() & infEnumerator.MoveNext())
                    {
                        var arrayX = posEnumerator.Current.X - Bounds.Min.X;
                        var arrayZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _influences[arrayX, arrayZ] = infEnumerator.Current;
                    }
                }
            }
        }

        public IEnumerable<double[]> GetBlocks(IEnumerable<Vector2i> positions)
        {
            var posEnumerator = positions.GetEnumerator();
            using (posEnumerator)
            {
                if (posEnumerator.MoveNext())
                {
                    var arrayX = posEnumerator.Current.X - Bounds.Min.X;
                    var arrayZ = posEnumerator.Current.Z - Bounds.Min.Z;
                    yield return _influences[arrayX, arrayZ];
                }
            }
        }

        public void SetHeights(IEnumerable<Vector2i> positions, IEnumerable<float> heights)
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

        public double[,][] GetInfluenceChunk()
        {
            return _influences;
        }

        public float[,] GetHeightMap()
        {
            return _heightMap;
        }

        public BlockInfo GetBlock(Vector2i blockPos)
        {
            var localPos = blockPos - Bounds.Min;
            var blockHeight = (_heightMap[localPos.X, localPos.Z] + _heightMap[localPos.X + 1, localPos.Z] +
                              _heightMap[localPos.X + 1, localPos.Z + 1] + _heightMap[localPos.X, localPos.Z + 1]) / 4;
            var blockInfluence = _influences[localPos.X, localPos.Z];

            BlockInfo result;
            if(blockInfluence != null)
                result = new BlockInfo(blockPos, BlockType.Grass, blockHeight, Vector3.up, blockInfluence);
            else
                result = new BlockInfo(blockPos, BlockType.Empty, blockHeight, Vector3.up, null);
            return result;
        }

        private readonly MacroMap _macromap;
        private readonly TriRunner _settings;
        private readonly float[,] _heightMap;
        private readonly BlockInfo[,] _blocks;
        private readonly double[,][] _influences;
    }
}
