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

        public readonly uint Id;

        public readonly MacroGrid.Cluster Cluster;

        public readonly Influence Influence;
        public int Count => Cluster.Count;

        public IReadOnlyCollection<Cell> Cells => this;

        /// <summary>
        /// Border cells of Zone
        /// </summary>
        public IEnumerable<Cell> Border => Cluster.GetBorderCells(  ).Select( hx => Cluster[hx] );

        public IEnumerable<MacroEdge> Edges => Cluster.GetBorderEdges( );

        public BiomeSettings Biome { get; private set; }

        public Zone( uint id, MacroGrid.Cluster cells, BiomeSettings biome )
        {
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

        //private readonly MacroMap _mesh;
    }
}
