using System.Collections.Generic;
using System.Linq;
using OpenTK;
using TerrainDemo.Settings;

namespace TerrainDemo.Macro
{
    public class Zone
    {
        public const int InvalidId = -1;

        public readonly int Id;
        public readonly MacroMap.CellMesh.Submesh Submesh;

        public IReadOnlyCollection<Cell> Cells => Submesh.Faces;

        /// <summary>
        /// Border cells of Zone
        /// </summary>
        public IReadOnlyCollection<Cell> Border => Submesh.BorderFaces;

        public IReadOnlyCollection<MacroEdge> Edges => Submesh.BorderEdges;

        public BiomeSettings Biome { get; private set; }

        public readonly Influence Influence;

        public Zone(MacroMap mesh, MacroMap.CellMesh.Submesh cells, int id, BiomeSettings biome, TriRunner settings)
        {
            _mesh = mesh;
            Submesh = cells;
            Id = id;
            Biome = biome;

            Influence = new Influence(id);
        }

        private readonly MacroMap _mesh;
    }
}
