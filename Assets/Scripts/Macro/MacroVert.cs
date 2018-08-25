using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Macro
{
    public class MacroVert
    {
        public const int MaxNeighborsCount = 3;

        public readonly int Id;
        public readonly Vector2 Coords;
        public Cell[] Cells { get; private set; }
        public MacroEdge[] Edges { get; private set; }

        public double[] Influence => _influence ?? (_influence = CalculateInfluence(Cells));

        public float Height
        {
            get
            {
                if (!_height.HasValue)
                    _height = _mesh.GetHeight(Coords);
                return _height.Value;
            }
        }

        /// <summary>
        /// Vertex of macro map
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="id"></param>
        public MacroVert(MacroMap mesh, int id, MacroMap.CellMesh.Vertice vertice, TriRunner settings)
        {
            _mesh = mesh;
            _vertice = vertice;
            Id = id;
            Coords = _vertice.Coords;
            _biomes = settings.Biomes;
        }

        public void Init([NotNull] IEnumerable<Cell> cells, IEnumerable<MacroEdge> edges)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            Assert.IsTrue(cells.Count() <= MaxNeighborsCount);
            Assert.IsTrue(edges.Count() <= MaxNeighborsCount);

            Cells = cells.ToArray();
            Edges = edges.ToArray();
        }

        public override string ToString() => $"Vert {Id}, cells: {Cells?.ToJoinedString(c => c.Position.ToString())}";

        private MacroMap.CellMesh.Vertice _vertice;
        private readonly MacroMap _mesh;
        private TriBiome[] _biomes;
        private double[] _influence;
        private float? _height;


        private double[] CalculateInfluence(Cell[] neighbors)
        {
            return _mesh.GetInfluence(Coords);

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
