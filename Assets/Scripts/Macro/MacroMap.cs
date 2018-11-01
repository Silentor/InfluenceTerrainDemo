using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenTK;
using TerrainDemo.Generators;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = OpenTK.Vector4;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Сетка макроячеек, вершин и сторон
    /// </summary>
    public class MacroMap
    {
        public readonly Box2 Bounds;
        public readonly List<Cell> Cells = new List<Cell>();
        public readonly List<MacroVert> Vertices = new List<MacroVert>();
        public readonly List<MacroEdge> Edges = new List<MacroEdge>();
        public readonly float[] Heights;
        public readonly List<Zone> Zones = new List<Zone>();
        public readonly List<BaseZoneGenerator> Generators = new List<BaseZoneGenerator>();

        public MacroMap(TriRunner settings, Random random)
        {
            _settings = settings;
            _random = random;
            _bounds = settings.LandBounds;
            _side = settings.Side;

            GenerateGrid();

            Heights = new float[Vertices.Count];

            Bounds = _mesh.Bounds;

            EmptyInfluence = new double[_settings.Biomes.Length];

            _influenceTurbulance = new FastNoise(random.Seed);
            _influenceTurbulance.SetFrequency(settings.InfluencePerturbFreq);
            _influenceTurbulancePower = settings.InfluencePerturbPower;
        }

        public Influence GetInfluence(Vector2 worldPosition)
        {
            return GetIDWInfluence(worldPosition);
        }

        public float GetHeight(Vector2 worldPosition)
        {
            return GetIDWHeight(worldPosition);
        }

        public Cell GetCellAt(Vector2 position)
        {
            if (!Bounds.Contains(position))
                return null;

            foreach (var cell in Cells)
            {
                if (cell.Contains(position))
                    return cell;
            }

            return null;
        }

        /*
        public Cell Intersect(Ray ray)
        {
            foreach (var cell in Cells)
            {
                
            }
        }
        */

        public IEnumerable<Cell> FloodFill(Cell startCell, Predicate<Cell> fillCondition = null)
        {
            return _mesh.FloodFill(startCell, fillCondition);
        }

        public CellMesh.Submesh GetSubmesh(IEnumerable<Cell> cells)
        {
            return _mesh.GetSubmesh(cells);
        }

        public string InfluenceToString(double[] influence)
        {
            return $"[{influence.ToJoinedString(i => i.ToString("F2"))}]";
        }

        public string InfluenceToBiomesString(double[] influence)
        {
            var biomes = from biome in _settings.Biomes
                where influence[biome.Index] > 0
                select $"{biome.name}: {influence[biome.Index]:G3}";
            return string.Join(", ", biomes.ToArray());
        }

        private readonly TriRunner _settings;
        private readonly Random _random;
        private readonly FastNoise _influenceTurbulance;

        private Box2 _bounds;
        private readonly float _side;

        private alglib.kdtree _idwInfluence;
        private int[] _nearestCellsTags = new int[0];
        private readonly double[] EmptyInfluence;
        private readonly List<Tuple<Cell, float>> _getInfluenceBuffer = new List<Tuple<Cell, float>>();
        private CellMesh _mesh;
        private readonly float _influenceTurbulancePower;

        private void GenerateGrid()
        {
            Debug.LogFormat("Generating grid of macrocells");

            var processedCells = new List<CellCandidate>();
            var unprocessedCells = new List<CellCandidate>();
            unprocessedCells.Add(new CellCandidate(new Coord(0, 0), Vector2.Zero, CalcVertsPosition(Vector2.Zero, _settings.Side)));

            //Iteratively process flood-fill cell generation algorithm
            while (unprocessedCells.Count > 0)
            {
                var candidateCell = unprocessedCells[0];
                unprocessedCells.RemoveAt(0);

                //Check if cell in land bound
                if (!_settings.LandBounds.Contains(candidateCell.Center)
                    || !candidateCell.Vertices.All(v => _settings.LandBounds.Contains(v)))
                    continue;

                processedCells.Add(candidateCell);

                //Prepare neighbors for adding
                const double triangleHeight = 1.732050807568877 / 2d; //https://en.wikipedia.org/wiki/Equilateral_triangle#Principal_properties
                var neighborsCenters = CalcNeighborCellsPosition(candidateCell.Center, triangleHeight * _side * 2);
                for (int i = 0; i < neighborsCenters.Length; i++)
                {
                    var position = candidateCell.Coords.Translated(Coord.Directions[i]);
                    if (!unprocessedCells.Exists(c => c.Coords == position) &&
                        !processedCells.Exists(c => c.Coords == position))
                    {
                        unprocessedCells.Add(new CellCandidate(position, neighborsCenters[i], CalcVertsPosition(neighborsCenters[i], _settings.Side)));
                    }
                }
            }

            _mesh = new CellMesh(processedCells.Select(pc => pc.Vertices));

            //Make MacroMap elements
            Vertices.Clear();
            Edges.Clear();
            Cells.Clear();

            _mesh.AssignData(
                delegate(Mesh<Cell, MacroEdge, MacroVert>.Vertex vertex)
                {
                    var vert = new MacroVert(this, _mesh, vertex, _settings);
                    Vertices.Add(vert);
                    return vert;
                },
                delegate(Mesh<Cell, MacroEdge, MacroVert>.Edge edge, MacroVert vert1, MacroVert vert2)
                {
                    var macroEdge = new MacroEdge(this, _mesh, edge, vert1, vert2);
                    Edges.Add(macroEdge);
                    return macroEdge;
                },
                delegate(CellMesh.Face face, IEnumerable<MacroVert> vertices, IEnumerable<MacroEdge> edges)
                {
                    var faceCoord = processedCells[face.Id].Coords;
                    var cell = new Cell(this, faceCoord, face, vertices, edges);
                    Cells.Add(cell);
                    return cell;
                });

            //Init all cells
            foreach (var cell in Cells)
            {
                var neighbors = Coord.Directions.Select(dir => Cells.Find(c => c.Coords == cell.Coords.Translated(dir)));
                cell.Init(neighbors);
            }

            Debug.LogFormat("Generated macromap of {0} vertices, {1} cells, {2} edges", Vertices.Count, Cells.Count, Edges.Count);
        }

        private void PrepareLandIDW()
        {
            if (_idwInfluence == null)
            {
                var positions = new double[Cells.Count, 2];
                var tags = new int[Cells.Count];

                for (int i = 0; i < Cells.Count; i++)
                {
                    positions[i, 0] = Cells[i].Center.X;
                    positions[i, 1] = Cells[i].Center.Y;
                    tags[i] = i;
                }

                alglib.kdtreebuildtagged(positions, tags, Cells.Count, 2, 0, 2, out _idwInfluence);
            }
        }

        private float GetIDWHeight(Vector2 worldPosition)
        {
            PrepareLandIDW();

            const float searchRadius = 25f;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);

            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            //Calculate height in the point
            float[] cellsHeights = new float[nearestCellsCount];
            double[] cellsWeights = new double[nearestCellsCount];
            double weightsSum = 0;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];

                cellsHeights[i] = cell.Height;
                cellsWeights[i] = IDWLocalShepard2(cell.Center, worldPosition, searchRadius);
                weightsSum += cellsWeights[i];
            }

            double result = 0;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                result += cellsHeights[i] * (cellsWeights[i] / weightsSum);
            }

            return (float) result;
        }

        private Influence GetIDWInfluence(Vector2 worldPosition)
        {
            PrepareLandIDW();

            //Turbulate position
            worldPosition = new Vector2(
                worldPosition.X + (float)_influenceTurbulance.GetValue(worldPosition.X, worldPosition.Y) * _influenceTurbulancePower, 
                worldPosition.Y + (float)_influenceTurbulance.GetValue(worldPosition.X + 1000, worldPosition.Y - 1000) * _influenceTurbulancePower);

            const float searchRadius = 25;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);
            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            //Short path - no cells in search radius, return influence of nearest cell
            if (nearestCellsCount == 0)
            {
                nearestCellsCount = alglib.kdtreequeryknn(_idwInfluence, new double[] {worldPosition.X, worldPosition.Y}, 1, true);
                if (nearestCellsCount > 0)
                {
                    var cell = Cells[_nearestCellsTags[0]];
                    return cell.Zone.Influence;
                }

                throw new InvalidOperationException("Cant calculate influence, no cells found");
            }

            _getInfluenceBuffer.Clear();

            //Sum up zones influence
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];
                var cellWeight = IDWLocalShepard(cell.Center, worldPosition, searchRadius);

                //Cells from that bring very low weight
                if(cellWeight < 0.01d)
                    break;

                _getInfluenceBuffer.Add(new Tuple<Cell, float>(cell, (float)cellWeight));
            }

            return new Influence(_getInfluenceBuffer);
        }

        private double IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            Assert.IsTrue(searchRadius > 0);

            if (interpolatePoint == point)
                return 1;

            double distance = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(distance <= searchRadius);

            var ratio = 1 - distance / searchRadius;        //Inverse lerp
            return ratio * ratio * ratio * ratio;           //In quintic
        }

        private double IDWLocalShepard2(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            double distance = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(distance <= searchRadius);

            var ratio = 1 - distance / searchRadius;        //Inverse lerp
            return SmoothStep(ratio);
            //return InQuad(ratio);
        }


        // Slightly slower than linear interpolation but much smoother
        public double InOutCosine(double x)
        {
            double ft = x * Math.PI;
            double f = (1 - Math.Cos(ft)) * 0.5;
            return f;
        }

        public double OutQuad(double x)
        {
            return x * (2 - x);
        }

        public double InCubic(double x)
        {
            return x * x * x;
        }

        public double InQuintic(double x)
        {
            return x * x * x * x;
        }

        public double OutCubic(double x)
        {
            return (x - 1) * (x - 1) * (x - 1) + 1;
        }


        public double InQuad(double x)
        {
            return x * x;
        }

        public double InExpo(double x)
        {
            return Math.Pow(2, 10 *(x-1));
        }

        public double SmoothStep(double x)
        {
            return 3 * x * x - 2 * x * x * x;
        }

        public double SmootherStep(double x)
        {
            return x * x * x * (6 * x * x - 15 * x + 10);
        }

        public double SmoothestStep(double x)
        {
            return x * x * x * x * (-20 * x * x * x + 70 * x * x - 84 * x + 35);
        }

        // Much slower than cosine and linear interpolation, but very smooth
        // v1 = a, v2 = b
        // v0 = point before a, v3 = point after b
        private Vector4 InterpolateCubic(Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3, float x)
        {
            var p = (v3 - v2) - (v0 - v1);
            var q = (v0 - v1) - p;
            var r = v2 - v0;
            var s = v1;
            return p * x * x * x + q * x * x + r * x + s;
        }

        private Vector4 InfluenceToVector4(double[] influence)
        {
            if(influence == null)
                return Vector4.Zero;

            var result = Vector4.Zero;
            var sum = 0f;
            for (int i = 0; i < influence.Length && i < 4; i++)
            {
                result[i] = (float)influence[i];
                sum += result[i];
            }

            if (sum > 0)
                result /= sum;

            return result;
        }

        private double[] Vector4ToInfluence(Vector4 influence)
        {
            var result = new double[_settings.Biomes.Length];
            var sum = 0d;
            for (int i = 0; i < result.Length && i < 4; i++)
            {
                if (influence[i] < 0)
                    influence[i] = 0;
                result[i] = influence[i];
                sum += result[i];
            }

            if (sum > 0)
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] /= sum;
                }

            return result;
        }

        //Clockwise from "right" vertice (angle = 0)
        private Vector2[] CalcVertsPosition(Vector2 center, double radius)
        {
            var result = new Vector2[6];
            for (int i = 0; i < Cell.MaxNeighborsCount; i++)
            {
                const double deg2rad = Math.PI / 180;
                var angle = deg2rad * (360 - i * 360d / Cell.MaxNeighborsCount);
                result[i] = new Vector2((float)(center.X + radius * Math.Cos(angle)), (float)(center.Y + radius * Math.Sin(angle)));
            }

            return result;
        }

        private Vector2[] CalcNeighborCellsPosition(Vector2 centerCell, double radius)
        {
            var result = new Vector2[Cell.MaxNeighborsCount];
            for (int i = 0; i < Cell.MaxNeighborsCount; i++)
            {
                const double deg2rad = Math.PI / 180;
                var angle = deg2rad * (90 - (i * 360d / Cell.MaxNeighborsCount));          //Start with Z+ axis (top)
                result[i] = new Vector2((float)(centerCell.X + radius * Math.Cos(angle)), (float)(centerCell.Y + radius * Math.Sin(angle)));
            }

            return result;
        }

        public class CellMesh : Mesh<Cell, MacroEdge, MacroVert>
        {
            public CellMesh(IEnumerable<Vector2[]> facesFromVertices) : base(facesFromVertices)
            {
            }
        }

        private struct CellCandidate
        {
            public readonly Coord Coords;
            public readonly Vector2 Center;
            public readonly Vector2[] Vertices;

            public CellCandidate(Coord coords, Vector2 center, Vector2[] vertices)
            {
                Coords = coords;
                Center = center;
                Vertices = vertices;
            }
        }
    }
}
