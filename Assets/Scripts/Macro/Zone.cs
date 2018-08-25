using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Tri;

namespace TerrainDemo.Macro
{
    public class Zone
    {
        private readonly MacroMap _mesh;
        public readonly Cell[] Cells;

        /// <summary>
        /// Border cells of Zone
        /// </summary>
        public readonly Cell[] Border;

        public readonly int Id;

        public IEnumerable<MacroEdge> Edges { get; private set; }

        public TriBiome Biome { get; private set; }

        public readonly double[] Influence;

        public Zone(MacroMap mesh, IEnumerable<Cell> cells, int id, TriBiome biome, TriRunner settings)
        {
            _mesh = mesh;
            Cells = cells.ToArray();
            Id = id;
            Biome = biome;

            Border = Cells.Where(c => c.Neighbors.Any(n => n == null || !Cells.Contains(n))).ToArray();
            var edges = from b in Border
                from e in b.Edges
                where e.GetOppositeOf(b) == null || !Cells.Contains(e.GetOppositeOf(b))
                select e;
            Edges = edges.Distinct().ToArray();
            Influence = new double[settings.Biomes.Length];
            Influence[biome.Index] = 1;

        }

        
    }
}
