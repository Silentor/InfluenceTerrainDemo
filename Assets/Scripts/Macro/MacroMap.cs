using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenTK;
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
        public readonly List<TriZoneGenerator> Generators = new List<TriZoneGenerator>();

        public MacroMap(TriRunner settings, Random random)
        {
            _settings = settings;
            _random = random;
            _bounds = settings.LandBounds;
            _side = settings.Side;

            GenerateGrid();

            Heights = new float[Vertices.Count];
            float top, bottom, left, right;
            top = right = float.MinValue;
            bottom = left = float.MaxValue;

            foreach (var vert in Vertices)
            {
                if (vert.Coords.X < left)
                    left = vert.Coords.X;
                else if (vert.Coords.X > right)
                    right = vert.Coords.X;
                if (vert.Coords.Y < bottom)
                    bottom = vert.Coords.Y;
                else if (vert.Coords.Y > top)
                    top = vert.Coords.Y;
            }
            Bounds = new Box2(left, top, right, bottom);

            EmptyInfluence = new double[_settings.Biomes.Length];
        }

        public double[] GetInfluence(Vector2 worldPosition)
        {
            //return GetNNInfluence(worldPosition);
            return GetIDWInfluence(worldPosition);
            //return GetRBFInfluence(worldPosition); uncontrollable overshoot :(
            //return GetBicubicInfluence(worldPosition);
        }

        public float GetHeight(Vector2 worldPosition)
        {
            CellWeightDebug dummy;
            return GetHeight(worldPosition, out dummy);
        }

        public float GetHeight(Vector2 worldPosition, out CellWeightDebug debug)
        {
            float height = GetIDWHeight(worldPosition, out debug);
            return height;
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

        public IEnumerable<Cell> FloodFill(Cell startCell, Predicate<Cell> fillCondition = null)
        {
            var result = new FloodFillEnumerator(this, startCell, fillCondition);
            for (int i = 0; i < 10; i++)
            {
                foreach (var cell in result.GetNeighbors(i))
                    yield return cell;
            }
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

        private Box2 _bounds;
        private readonly float _side;

        private alglib.kdtree _idwInfluence;
        private int[] _nearestCellsTags = new int[0];
        private readonly double[] EmptyInfluence;
        private CellMesh _mesh;

        private void GenerateGrid()
        {
            Debug.LogFormat("Generating grid of macrocells");

            Vertices.Clear();
            Edges.Clear();
            Cells.Clear();

            var mesh = new CellMesh( );
            var faces = new List<CellMesh.Face>();
            var vertices = new List<CellMesh.Vertice>();
            var edges = new List<CellMesh.Edge>();
            var cellCandidates = new List<CellCandidate>();

            var noise = new FastNoise(_random.Seed);
            noise.SetFractalOctaves(1);
            noise.SetFrequency(0.01);

            var unprocessedCells = new List<CellCandidate>();
            unprocessedCells.Add(new CellCandidate(Vector2i.Zero, Vector2.Zero));

            //Iteratively process flood-fill cell generation algorithm
            while (unprocessedCells.Count > 0)
            {
                var candidateCell = unprocessedCells[0];
                unprocessedCells.RemoveAt(0);

                //Check if cell in land bound
                var vertPositions = CalcVertsPosition(candidateCell.Center, _settings.Side);

                if (!_settings.LandBounds.Contains(candidateCell.Center)
                    || !vertPositions.All(v => _settings.LandBounds.Contains(v)))
                    continue;

                //Candidate within land bounds, create actual cell: 1) vertices, 2) edges, 3) cell
                var cellVertices = vertPositions.Select(p => GetVertice(p, vertices, mesh)).ToArray();

                var cellEdges = new CellMesh.Edge[Cell.MaxNeighborsCount];
                for (int i = 0; i < cellVertices.Length; i++)
                {
                    var vert1 = cellVertices[i];
                    var vert2 = cellVertices[(i + 1) % cellVertices.Length];
                    var edge = GetEdge(vert1, vert2, edges, mesh);
                    cellEdges[i] = edge;
                }

                var face = new CellMesh.Face(mesh, candidateCell.Center, faces.Count, cellVertices, cellEdges);
                faces.Add(face);
                candidateCell.Face = face;
                cellCandidates.Add(candidateCell);

                //Prepare neighbors for adding
                const double triangleHeight = 1.732050807568877 / 2d; //https://en.wikipedia.org/wiki/Equilateral_triangle#Principal_properties
                var neighborsCenters = CalcNeighborCellsPosition(face.Center, triangleHeight * _side * 2);
                for (int i = 0; i < neighborsCenters.Length; i++)
                {
                    var position = candidateCell.Position + Cell.Directions[i];
                    if (!unprocessedCells.Exists(c => c.Position == position) &&
                        !cellCandidates.Exists(c => c.Position == position))
                        unprocessedCells.Add(new CellCandidate(position, neighborsCenters[i]));
                }
            }

            mesh.Set(faces, edges, vertices);

            //Make MacroMap elements
            Vertices.AddRange(vertices.Select(v => new MacroVert(this, v.Id, v, _settings)));
            Edges.AddRange(edges.Select(e => new MacroEdge(this, e, Vertices)));
            Cells.AddRange(cellCandidates.Select(c => new Cell(this, c.Position, c.Face, Vertices, Edges)));

            foreach (var face in faces)
            {
                face.Data = Cells.First(c => c.Id == face.Id);
            }
           

            //Init all cells
            foreach (var cell in Cells)
            {
                var neighbors = Cell.Directions.Select(dir => Cells.Find(c => c.Position == cell.Position + dir));
                cell.Init(neighbors);
            }

            //Init all edges
            foreach (var edge in Edges)
            {
                var neighborCells = Cells.Where(c => c.Edges.Contains(edge)).ToArray();

                Assert.IsTrue(neighborCells.Length > 0 && neighborCells.Length <= 2);

                edge.Init(neighborCells[0], neighborCells.Length > 1 ? neighborCells[1] : null);
            }

            //Init all vertices
            foreach (var vertex in Vertices)
            {
                var neighborEdges = Edges.Where(e => e.Vertex1 == vertex || e.Vertex2 == vertex).ToArray();
                var neighborCells = Cells.Where(c => c.Vertices2.Contains(vertex)).ToArray();

                Assert.IsTrue(neighborEdges.Length >= 2 && neighborEdges.Length <= 3, $"vertex {vertex}");
                Assert.IsTrue(neighborCells.Length >= 1 && neighborCells.Length <= 3);

                vertex.Init(neighborCells, neighborEdges);
            }

            _mesh = new CellMesh();

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

        private float GetIDWHeight(Vector2 worldPosition, out CellWeightDebug debug)
        {
            PrepareLandIDW();

            const float searchRadius = 25f;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);

            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            debug = new CellWeightDebug();
            debug.Position = worldPosition;

            //Calculate height in the point
            float[] cellsHeights = new float[nearestCellsCount];
            double[] cellsWeights = new double[nearestCellsCount];
            debug.Cells = new CellWeightInfo[nearestCellsCount];
            debug.Radius = searchRadius;
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

                debug.Cells[i] = new CellWeightInfo()
                {
                    Id = Cells[_nearestCellsTags[i]].Position,
                    Height = cellsHeights[i],
                    Weight = cellsWeights[i] / weightsSum
                };
            }

            return (float) result;
        }

        private double[] GetIDWInfluence(Vector2 worldPosition)
        {
            PrepareLandIDW();

            const float searchRadius = 25;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);
            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            var result = new double[_settings.Biomes.Length];

            //Sum up zones influence
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];

                var zoneWeight = IDWLocalShepard(cell.Center, worldPosition, searchRadius);

                for (int j = 0; j < result.Length; j++)
                {
                    result[j] += cell.Zone.Influence[j] * zoneWeight;
                }
            }

            //Normalize
            var sum = 0d;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] > 0)
                    sum += result[i];
                else
                    result[i] = 0;
            }

            Assert.IsTrue(sum != 0, "for position " + worldPosition);

            for (int i = 0; i < result.Length; i++)
                result[i] /= sum;

            return result;
        }

        private double IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            if (interpolatePoint == point)
                return 1;

            double d = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(d <= searchRadius);

            //DEBUG
            var linear = Mathf.InverseLerp((float) searchRadius, 0, (float) d);
            //return linear;
            //return OutQuad(linear);
            //return InOutCosine(linear);
            //return SmoothStep(linear);
            //return SmootherStep(linear);
            //return SmoothestStep(linear);
            //return InCubic(linear);
            return InQuad(linear);
            //DEBUG

            var a = searchRadius - d;
            if (a < 0) a = 0;
            var b = a / (searchRadius * d);
            //return b * b;
            return Math.Pow(b, 1.2);
        }

        private double IDWLocalShepard2(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            double d = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(d <= searchRadius);

            var linear = Mathf.InverseLerp((float)searchRadius, 0, (float)d);
            //return linear;
            //return InOutCosine(linear);
            //return InQuad(linear);
            //return OutCubic(linear);
            //return SmoothestStep(linear);
            //return SmootherStep(linear);
            return SmoothStep(linear);
            //return OutQuad(linear);
            //return InExpo(linear);
            //return SmoothCustom1(linear);
            //return InCubic(linear);
            //return InQuintic(linear);
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


        private CellMesh.Vertice GetVertice(Vector2 position, List<CellMesh.Vertice> vertices, CellMesh mesh)
        {
            var vert = vertices.Find(v => Vector2.DistanceSquared(v.Coords, position) < 0.01 * 0.01);
            if (vert == null)
            {
                vert = new CellMesh.Vertice(mesh, vertices.Count, position);
                vertices.Add(vert);
            }

            return vert;
        }

        private CellMesh.Edge GetEdge(CellMesh.Vertice vert1, CellMesh.Vertice vert2, List<CellMesh.Edge> edges, CellMesh mesh)
        {
            var edge = edges.Find(e => e.IsConnects(vert1, vert2));
            if (edge == null)
            {
                edge = new CellMesh.Edge(mesh, edges.Count, vert1, vert2);
                edges.Add(edge);
            }

            return edge;
        }

        public class CellMesh : TerrainDemo.Voronoi.Mesh<Cell>
        {
        }

        /// <summary>
        /// For searching cells using flood-fill algorithm
        /// </summary>
        public class FloodFillEnumerator
        {
            private readonly MacroMap _mesh;
            private readonly Predicate<Cell> _searchFor;
            private readonly List<List<Cell>> _neighbors = new List<List<Cell>>();

            /// <summary>
            /// Create flood-fill around <see cref="start"> cell
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFillEnumerator(MacroMap mesh, Cell start, Predicate<Cell> searchFor = null)
            {
                Assert.IsTrue(mesh.Cells.Contains(start));
                Assert.IsTrue(searchFor == null || searchFor(start));

                _mesh = mesh;
                _searchFor = searchFor;
                _neighbors.Add(new List<Cell> { start });
            }

            /// <summary>
            /// Create flood-fill around <see cref="start"> cells
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFillEnumerator(MacroMap mesh, Cell[] start, Predicate<Cell> searchFor = null)
            {
                Assert.IsTrue(start.All(c => mesh.Cells.Contains(c)));
                Assert.IsTrue(searchFor == null || start.All(c => searchFor(c)));

                _mesh = mesh;
                _searchFor = searchFor;
                var startStep = new List<Cell>();
                startStep.AddRange(start);
                _neighbors.Add(startStep);
            }


            /// <summary>
            /// Get neighbors of cell(s)
            /// </summary>
            /// <param name="step">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
            /// <returns></returns>
            public IEnumerable<Cell> GetNeighbors(int step)
            {
                if (step == 0)
                    return _neighbors[0];

                if (step < _neighbors.Count)
                    return _neighbors[step];

                //Calculate neighbors
                if (step - 1 < _neighbors.Count)
                {
                    var processedCellsIndex = Math.Max(0, step - 2);
                    var result = GetNeighbors(_neighbors[step - 1], _neighbors[processedCellsIndex]);
                    _neighbors.Add(result);
                    return result;
                }
                else
                {
                    //Calculate previous steps (because result of step=n used for step=n+1)
                    for (int i = _neighbors.Count; i < step; i++)
                        GetNeighbors(i);
                    return GetNeighbors(step);
                }
            }

            /// <summary>
            /// Get neighbors of <see cref="prevNeighbors"/> doesnt contained in <see cref="alreadyProcessed"/>
            /// </summary>
            /// <param name="prevNeighbors"></param>
            /// <param name="alreadyProcessed"></param>
            /// <returns></returns>
            private List<Cell> GetNeighbors(List<Cell> prevNeighbors, List<Cell> alreadyProcessed)
            {
                var result = new List<Cell>();
                foreach (var neigh1 in prevNeighbors)
                {
                    foreach (var neigh2 in neigh1.NeighborsSafe)
                    {
                        if ((_searchFor == null || _searchFor(neigh2))          //check search for condition
                            && !result.Contains(neigh2) && !prevNeighbors.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
                            result.Add(neigh2);
                    }
                }

                return result;
            }
        }

        public struct CellWeightDebug
        {
            public CellWeightInfo[] Cells;
            public float Radius;
            public Vector2 Position;
        }

        public struct CellWeightInfo
        {
            public Vector2i Id;
            public double Weight;
            public float Height;
        }

        public struct CellCandidate
        {
            public readonly Vector2i Position;
            public readonly Vector2 Center;
            public CellMesh.Face Face;

            public CellCandidate(Vector2i position, Vector2 center)
            {
                Position = position;
                Center = center;
                Face = null;
            }
        }
    }
}
