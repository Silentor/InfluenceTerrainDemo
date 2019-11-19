using System.Collections;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Micro-scale cell entity, based on <see cref="TerrainDemo.Macro.Cell"/>
    /// </summary>
    public class Cell
    {
        public readonly HexPos Id;
        public readonly Macro.Cell Macro;
        public readonly GridPos Center;
        public readonly GridArea BlockPositions;
        public readonly GridPos[] VertexPositions;
        public readonly Bounds2i Bounds;

        public Cell(Macro.Cell macro, MicroMap map)
        {
            Id = macro.HexPoses;
            Macro = macro;
            Center = (GridPos)macro.Center;
            _map = map;
            BlockPositions = Rasterization.ConvexToBlocks(macro.Contains, macro.Bounds);

            Bounds = (Bounds2i) macro.Bounds;

            var vertices = new List<GridPos>(BlockPositions);
            foreach (var blockPosition in BlockPositions)
            {
                if (!vertices.Contains(blockPosition + Vector2i.Forward))
                    vertices.Add(blockPosition + Vector2i.Forward);
                if (!vertices.Contains(blockPosition + Vector2i.Right))
                    vertices.Add(blockPosition + Vector2i.Right);
                if (!vertices.Contains(blockPosition + Vector2i.One))
                    vertices.Add(blockPosition + Vector2i.One);
            }

            VertexPositions = vertices.ToArray();
        }

        public IEnumerable<BlockInfo> GetBlocks()
        {
            foreach (var blockPosition in BlockPositions)
            {
                yield return _map.GetBlock(blockPosition).Value;
            }
        }
        private readonly MicroMap _map;
    }
}
