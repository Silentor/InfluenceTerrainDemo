using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Settings;

namespace TerrainDemo.Macro
{
    public class Zone : IReadOnlyCollection<Cell>
    {
        public const int InvalidId = -1;

        public readonly int Id;

        public readonly MacroMap.CellMesh.Cluster Cluster;

        public readonly Influence Influence;
        public int Count => Cluster.Count;

        public IReadOnlyCollection<Cell> Cells => this;

        /// <summary>
        /// Border cells of Zone
        /// </summary>
        public IEnumerable<Cell> Border => Cluster.GetBorderCells(  ).Select( hx => Cluster[hx] );

        public IEnumerable<MacroEdge> Edges => Cluster.GetBorderEdges( ).Select( e => e.Data );

        public BiomeSettings Biome { get; private set; }

        public Zone(MacroMap mesh, MacroMap.CellMesh.Cluster cells, int id, BiomeSettings biome, TriRunner settings)
        {
            _mesh = mesh;
            Cluster = cells;
            Id = id;
            Biome = biome;

            Influence = new Influence(id);
        }

        public IEnumerator<Cell> GetEnumerator( )
        {
	        return Cluster.Select( pos => Cluster.Grid[pos] ).GetEnumerator( );
        }
        public override string ToString()
        {
            return $"Zone {Id}({Biome})";
        }
        IEnumerator IEnumerable.GetEnumerator( )
        {
	        return GetEnumerator( );
        }

        private readonly MacroMap _mesh;
    }
}
