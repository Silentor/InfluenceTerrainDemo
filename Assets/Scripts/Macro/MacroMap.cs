using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenTK;
using TerrainDemo.Generators;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Quaternion = OpenTK.Quaternion;
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
            _side = settings.CellSide;

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

        public Heights GetHeight(Vector2 worldPosition)
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

        public ValueTuple<Cell, Vector3> Raycast(Ray ray)
        {
            foreach (var cell in Cells.OrderBy(c => Vector3.SqrMagnitude(c.CenterPoint - ray.origin)))
            {
                var intersection = cell.Raycast(ray);
                if (intersection.HasValue)
                {
                    return new ValueTuple<Cell, Vector3>(cell, intersection.Value);
                }
            }

            return new ValueTuple<Cell, Vector3>(null, Vector3.zero);
        }

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
        private readonly float _influenceTurbulancePower;
        private Box2 _bounds;
        private readonly float _side;

        private alglib.kdtree _idwInfluence;
        private int[] _nearestCellsTags = new int[0];
        private readonly double[] EmptyInfluence;
        private readonly List<(Cell, float)> _getInfluenceBuffer = new List<(Cell, float)>();
        private CellMesh _mesh;

        private void GenerateGrid()
        {
            Debug.LogFormat("Generating grid of macrocells");

            var gridPerturbator = new FastNoise(unchecked (_settings.Seed + 10));
            gridPerturbator.SetFrequency(_settings.GridPerturbFreq);

            var processedCells = new List<CellCandidate>();
            var unprocessedCells = new List<CellCandidate>();
            unprocessedCells.Add(new CellCandidate(new Coord(0, 0), Vector2.Zero, CalcVertsPosition(Vector2.Zero, _settings.CellSide)));

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
                    var coord = candidateCell.Coords.Translated(Coord.Directions[i]);
                    if (!unprocessedCells.Exists(c => c.Coords == coord) &&
                        !processedCells.Exists(c => c.Coords == coord))
                    {
                        unprocessedCells.Add(new CellCandidate(coord, neighborsCenters[i], CalcVertsPosition(neighborsCenters[i], _settings.CellSide)));
                    }
                }
            }

            //Perturb grid vertices
            foreach (var cell in processedCells)
                for (var i = 0; i < cell.Vertices.Length; i++)
                    cell.Vertices[i] = PerturbGridPoint(cell.Vertices[i], gridPerturbator);

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

        private Vector2 PerturbGridPoint(Vector2 point, FastNoise gridPerturbator)
        {
            var xPerturb = (float)gridPerturbator.GetSimplex(point.X, point.Y) * _settings.GridPerturbPower;
            var zPerturb = (float)gridPerturbator.GetSimplex(point.X + 1000, point.Y - 1000) * _settings.GridPerturbPower;
            return point + new Vector2(xPerturb, zPerturb);
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

        private Heights GetIDWHeight(Vector2 worldPosition)
        {
            PrepareLandIDW();

            const float searchRadius = 25f;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);

            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            //Calculate height in the point
            Vector3d[] cellsHeights = new Vector3d[nearestCellsCount];
            double[] cellsWeights = new double[nearestCellsCount];
            double weightsSum = 0;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];

                cellsHeights[i] = (Vector3d)cell.DesiredHeight;
                cellsWeights[i] = IDWLocalShepard2(cell.Center, worldPosition, searchRadius);
                weightsSum += cellsWeights[i];
            }

            var result = Vector3d.Zero;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                result += cellsHeights[i] * (cellsWeights[i] / weightsSum);
            }

            return (Heights)result;
        }

        private Influence GetIDWInfluence(Vector2 worldPosition)
        {
            PrepareLandIDW();

            //Turbulate position
            worldPosition = new Vector2(
                worldPosition.X + (float)_influenceTurbulance.GetSimplex(worldPosition.X, worldPosition.Y) * _influenceTurbulancePower, 
                worldPosition.Y + (float)_influenceTurbulance.GetSimplex(worldPosition.X + 1000, worldPosition.Y - 1000) * _influenceTurbulancePower);

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

                _getInfluenceBuffer.Add((cell, (float)cellWeight));
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
            return Interpolation.SmoothStep(ratio);
            //return InQuad(ratio);
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
                result[i] = 
                    new Vector2((float) (center.X + radius * Math.Cos(angle)), (float) (center.Y + radius * Math.Sin(angle)));
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
