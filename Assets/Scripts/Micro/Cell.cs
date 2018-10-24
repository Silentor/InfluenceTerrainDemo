using System.Collections;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Micro-scale cell entity, based on <see cref="TerrainDemo.Macro.Cell"/>
    /// </summary>
    public class Cell
    {
        public readonly Macro.Cell Macro;
        public readonly Vector2i[] BlockPositions;
        public readonly Vector2i[] VertexPositions;
        public readonly Bounds2i Bounds;

        public Cell(Macro.Cell macro)
        {
            Macro = macro;
            BlockPositions = Rasterization.Polygon2(macro.Contains, macro.Bounds);
            Bounds = (Bounds2i) macro.Bounds;

            var vertices = new List<Vector2i>(BlockPositions.Length * 2);
            foreach (var blockPosition in BlockPositions)
            {
                if(!vertices.Contains(blockPosition))
                    vertices.Add(blockPosition);
                if (!vertices.Contains(blockPosition + Vector2i.Forward))
                    vertices.Add(blockPosition + Vector2i.Forward);
                if (!vertices.Contains(blockPosition + Vector2i.Right))
                    vertices.Add(blockPosition + Vector2i.Right);
                if (!vertices.Contains(blockPosition + Vector2i.One))
                    vertices.Add(blockPosition + Vector2i.One);
            }

            VertexPositions = vertices.ToArray();
        }
    }
}
