using System.Collections.Generic;
using TerrainDemo.Tools;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Macro
{
    public class MacroVert
    {
        public Vector2 Position => _vertex.Position;

        public Cell Cell1 => _vertex.Grid [ _vertex.Cell1 ];
        public Cell Cell2 => _vertex.Grid [ _vertex.Cell2 ];
        public Cell Cell3 => _vertex.Grid [ _vertex.Cell3 ];

        public IEnumerable<Cell> Cells
        {
	        get
	        {
		        yield return Cell1;
		        yield return Cell2;
		        yield return Cell3;
	        }
        }


        public Influence Influence
        {
            get
            {
                if (!_influence.HasValue)
                    _influence = CalculateInfluence(null);
                return _influence.Value;
            }
        }

        public Heights Height
        {
            get
            {
                if (!_height.HasValue)
                    _height = _map.GetHeight(Position);
                return _height.Value;
            }
        }

        /// <summary>
        /// Vertex of macro map
        /// </summary>
        /// <param name="map"></param>
        /// <param name="id"></param>
        public MacroVert(MacroMap map, MacroMap.CellMesh.VertexHolder vertex, TriRunner settings)
        {
            _map = map;
            _vertex = vertex;
        }

        public override string ToString() => $"Vert, cells: {Cells?.ToJoinedString(c => c.HexPos.ToString())}";

        private readonly MacroMap.CellMesh.VertexHolder _vertex;
        private readonly MacroMap _map;
        private Influence? _influence;
        private Heights? _height;
        
        private Influence CalculateInfluence(Cell[] neighbors)
        {
            return _map.GetInfluence(Position);
        }
    }
}
