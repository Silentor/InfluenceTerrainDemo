using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Macro
{
    public class MacroVert
    {
        public const int MaxNeighborsCount = 3;

        public int Id => _vertex.Id;

        public Vector2 Position => _vertex.Position;

        public Cell[] Cells => _cells ?? (_cells = _mesh2.GetAdjacentFaces(this).ToArray());

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
        public MacroVert(MacroMap map, MacroMap.CellMesh mesh2, MacroMap.CellMesh.VertexHolder vertex, HexPos cell1, HexPos cell2, HexPos cell3 )
        {
            _map = map;
            _mesh2 = mesh2;
            
            //_vertex.Data = this;
        }

        public override string ToString() => $"Vert {Id}, cells: {Cells?.ToJoinedString(c => c.HexPos.ToString())}";

        private MacroMap.CellMesh.Vertex _vertex;
        private readonly MacroMap _map;
        private Influence? _influence;
        private Heights? _height;
        private MacroMap.CellMesh _mesh2;
        private Cell[] _cells;
        private MacroEdge[] _edges;


        private Influence CalculateInfluence(Cell[] neighbors)
        {
            return _map.GetInfluence(Position);

            /*
            var result = new double[_biomes.Length];
            var sum = 0d;

            //Collect influence of neighbor cells
            for (var i = 0; i < neighbors.Length; i++)
            {
                result[neighbors[i].Biome.Index] += 1;
                sum++;
            }

            //Normalize
            for (var i = 0; i < result.Length; i++)
                result[i] /= sum;

            //Filter weak cells
            /*
            sum = 0d;
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] <= 1/6d)
                    result[i] = 0;
                else if (result[i] <= 1/3d && neighbors.Length > 3)
                    result[i] = 1 / 6d;
                sum += result[i];
            }


            //Renormalize
            for (var i = 0; i < result.Length; i++)
                result[i] /= sum;
                */
            /*
            return result;
            */
        }
    }
}
