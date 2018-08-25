using System.Collections;
using System.Collections.Generic;
using TerrainDemo.Macro;
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
        public readonly Vector2i[] Blocks;
        public readonly Bounds2i Bounds;

        public Cell(Macro.Cell macro, Vector2i[] blocks)
        {
            Macro = macro;
            Blocks = blocks;
            Bounds = (Bounds2i) macro.Bounds;
        }
    }
}
