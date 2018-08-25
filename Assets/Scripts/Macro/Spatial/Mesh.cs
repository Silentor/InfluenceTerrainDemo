using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Libs.SimpleJSON;
using TerrainDemo.Macro;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngineInternal;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Voronoi
{
    public class Mesh<TFaceData> : Network<Mesh<TFaceData>, Mesh<TFaceData>.Face, TFaceData>
    {
        public Box2 Bounds { get; private set; }
        public IEnumerable<Face> Faces => _faces;
        public IEnumerable<Edge> Edges => _edges;
        public IEnumerable<Vertice> Vertices => _vertices;

        public Mesh()
        {
            //Bounds = bounds;
        }

        public void Set(IEnumerable<Face> faces, IEnumerable<Edge> edges, IEnumerable<Vertice> vertices)
        {
            _faces = faces.ToArray();
            _edges = edges.ToArray();
            _vertices = vertices.ToArray();

            foreach (var face in _faces)
            {
                var neighbors = _edges.Select(e => e.GetOppositeOf(face)).Where(f => f != null);
                face.Init(neighbors);
            }

            //Init all edges
            foreach (var edge in _edges)
            {
                var neighborCells = _faces.Where(c => c.Edges.Contains(edge)).ToArray();

                Assert.IsTrue(neighborCells.Length > 0 && neighborCells.Length <= 2);

                edge.Init(neighborCells[0], neighborCells.Length > 1 ? neighborCells[1] : null);
            }

            //Init all vertices
            foreach (var vertex in _vertices)
            {
                var neighborEdges = _edges.Where(e => e.Vertex1 == vertex || e.Vertex2 == vertex).ToArray();
                var neighborCells = _faces.Where(c => c.Vertices.Contains(vertex)).ToArray();

                Assert.IsTrue(neighborEdges.Length >= 2 && neighborEdges.Length <= 3, $"vertex {vertex}");
                Assert.IsTrue(neighborCells.Length >= 1 && neighborCells.Length <= 3);

                vertex.Init(neighborCells, neighborEdges);
            }


            //Calculate mesh bounds
            Bounds = _faces[0].Bounds;
            for (int i = 1; i < _faces.Length; i++)
                Bounds = Combine(Bounds, _faces[i].Bounds);
        }

        /// <summary>
        /// Get nearest (containing) cell for given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        [Pure]
        public Face GetCellFor(Vector2 position)
        {
            if (!Bounds.Contains(position))
                return null;

            var minDistance = float.MaxValue;
            Face result = null;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var cellDistance = Vector2.DistanceSquared(position, _nodes[i].Center);
                if (cellDistance < minDistance)
                {
                    minDistance = cellDistance;
                    result = _nodes[i];
                }
            }

            return result;
        }

        /*
        /// <summary>
        /// Enumerate cell neighbors in breath-first manner (optimized)
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public IEnumerable<Cell> GetNeighbors([NotNull] Cell center)
        {
            if (center == null) throw new ArgumentNullException("center");

            foreach (var neighbor in center.Neighbors)
                yield return neighbor;

            foreach (var neighbor in center.Neighbors2)
                yield return neighbor;

            var processed = new Cell[center.Neighbors.Count + center.Neighbors2.Count + 1];
            center.Neighbors.CopyTo(processed, 0);
            center.Neighbors2.CopyTo(processed, center.Neighbors.Count);
            processed[center.Neighbors.Count + center.Neighbors2.Count] = center;

            var fill = FloodFill(processed);
            for (int i = 1; i < 100; i++)                //100 + 2 steps - sanity number, probably very large map can contains more
            {
                var neighbors = fill.GetNeighbors(i);
                if (neighbors.Any())
                    foreach (var neighbor in neighbors)
                        yield return neighbor;
                else yield break;
            }

            throw new NotImplementedException("Too deep");
        }
        */

        /// <summary>
        /// Enumerate cell neighbors in breath-first manner
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public IEnumerable<Face> GetNeighbors([NotNull] Face center, Predicate<Face> allowFill = null)
        {
            if (center == null) throw new ArgumentNullException("center");

            var fill = FloodFill(center, allowFill);
            for (int i = 1; i < 100; i++)                //100 steps - sanity number, probably very large map can contains more
            {
                var neighbors = fill.GetNeighbors(i);
                if (neighbors.Any())
                    foreach (var neighbor in neighbors)
                        yield return neighbor;
                else yield break;
            }
        }

        //[DebuggerDisplay("TriCell {Id}({North.Id}, {East.Id}, {South.Id})")]
        public class Face : BaseNode
        {
            public readonly Vector2 Position;
            public readonly Box2 Bounds; //todo replace box bound with containing circle/other primitive?

            //Vertices

            public IEnumerable<Vertice> Vertices => _vertices;
            //public IReadOnlyList<Vertice> Vertices3 => _vertices;

            //public IEnumerable<Face> NeighborsSafe => _neighbors.Where(n => n != null);


            public IEnumerable<Edge> Edges => _edges;

            public readonly Vector2 Center;

            //public static readonly IdComparer IdIncComparer = new IdComparer();
            //public static readonly CellEqualityComparer CellComparer = new CellEqualityComparer();

            public Face(Mesh<TFaceData> map, Vector2 position, int id, [NotNull] Vertice[] vertices, [NotNull] Edge[] edges) : base(map, id)
            {
                if (vertices == null) throw new ArgumentNullException(nameof(vertices));
                if (edges == null) throw new ArgumentNullException(nameof(edges));
                Assert.IsTrue(vertices.Distinct().Count() == vertices.Length);
                Assert.IsTrue(edges.Distinct().Count() == edges.Length);

                _vertices = vertices;
                _edges = edges;
                Position = position;

                //Build bounding box and calc center
                float top = float.MinValue, bottom = float.MaxValue, left = float.MaxValue, right = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Center += vertices[i].Coords;
                    if (vertices[i].Coords.X > right) right = vertices[i].Coords.X;
                    if (vertices[i].Coords.X < left) left = vertices[i].Coords.X;
                    if (vertices[i].Coords.Y > top) top = vertices[i].Coords.Y;
                    if (vertices[i].Coords.Y < bottom) bottom = vertices[i].Coords.Y;
                }
                Center /= vertices.Length;

                Bounds = new Box2(left, top, right, bottom);

                //Build edges halfplanes for Contains test
                _halfPlanes = new HalfPlane[edges.Length];
                for (int i = 0; i < edges.Length; i++)
                    _halfPlanes[i] = new HalfPlane(edges[i].Vertex1.Coords, edges[i].Vertex2.Coords, Center);
            }

            public void Init(IEnumerable<Face> neighbors)
            {
                _neighbors = neighbors.ToArray();
                Assert.IsTrue(_neighbors.Length > 2);
                Assert.IsTrue(_neighbors.Distinct().Count() == _neighbors.Length);
            }

            /*
            /// <summary>
            /// From https://stackoverflow.com/a/20861130
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            public bool Contains(Vector2 point)
            {
                var s = V1.Y * V3.X - V1.X * V3.Y + (V3.Y - V1.Y) * point.X + (V1.X - V3.X) * point.Y;
                var t = V1.X * V2.Y - V1.Y * V2.X + (V1.Y - V2.Y) * point.X + (V2.X - V1.X) * point.Y;

                if ((s < 0) != (t < 0))
                    return false;   

                var A = -V2.Y * V3.X + V1.Y * (V3.X - V2.X) + V1.X * (V2.Y - V3.Y) + V2.X * V3.Y;
                if (A < 0.0)
                {
                    s = -s;
                    t = -t;
                    A = -A;
                }
                return s > 0 && t > 0 && (s + t) <= A;
            }
            */

            /// Quad-cell version
            /// <param name="point"></param>
            /// <returns></returns>
            public bool Contains(Vector2 point)
            {
                if (!Bounds.Contains(point))
                    return false;

                for (int i = 0; i < _halfPlanes.Length; i++)
                    if (!_halfPlanes[i].Contains(point))
                        return false;

                return true;
            }

            public override string ToString()
            {
                return $"TriCell {Position}({_neighbors[0]?.Position.ToString() ?? "?"}, {_neighbors[1]?.Position.ToString() ?? "?"}, {_neighbors[2]?.Position.ToString() ?? "?"}, {_neighbors[3]?.Position.ToString() ?? "?"}, {_neighbors[4]?.Position.ToString() ?? "?"}, {_neighbors[5]?.Position.ToString() ?? "?"})";
            }

            private Zone _zone;
            private readonly HalfPlane[] _halfPlanes;
            private double[] _influence;
            private readonly Vertice[] _vertices;
            private readonly Edge[] _edges;
            private Face[] _neighbors;

            public enum Sides
            {
                ZPositive = 0,
                YPositive,
                XPositive,
                ZNegative,
                YNegative,
                XNegative
            }

            /*
            /// <summary>
            /// Compare cells id
            /// </summary>
            public class IdComparer : IComparer<Cell>
            {
                public int Compare(Cell x, Cell y)
                {
                    if (x.Id < y.Id)
                        return -1;
                    else if (x.Id > y.Id)
                        return 1;
                    else return 0;
                }
            }
            */
            /*
            public class CellEqualityComparer : IEqualityComparer<Cell>
            {
                public bool Equals(Cell x, Cell y)
                {
                    if (x == null && y == null) return true;
                    if (x == null && y != null) return false;
                    if (x != null && y == null) return false;
                    return x.Id == y.Id && x.Map == y.Map;
                }

                public int GetHashCode(Cell obj)
                {
                    return obj.Map.GetHashCode() ^ obj.Id.GetHashCode();
                }
            }
            */
        }

        public class Edge : BaseNode, IEquatable<Edge>
        {
            public Vertice Vertex1 { get; }

            public Vertice Vertex2 { get; }

            public Face Cell1 { get; private set; }

            public Face Cell2 { get; private set; }

            public Edge([NotNull] Mesh<TFaceData> mesh, int id, [NotNull] Vertice vertex1, [NotNull] Vertice vertex2) : base(mesh, id)
            {
                if (mesh == null) throw new ArgumentNullException(nameof(mesh));
                if (vertex1 == null) throw new ArgumentNullException(nameof(vertex1));
                if (vertex2 == null) throw new ArgumentNullException(nameof(vertex2));

                _mesh = mesh;
                _id = id;
                Vertex1 = vertex1;
                Vertex2 = vertex2;
            }

            public bool IsConnects(Vertice vert1, Vertice vert2)
            {
                return (vert1 == Vertex1 && vert2 == Vertex2) || (vert1 == Vertex2 && vert2 == Vertex1);
            }

            public Face GetOppositeOf([NotNull] Face face)
            {
                if (face == null) throw new ArgumentNullException(nameof(face));

                if (Cell1 == face)
                    return Cell2;
                else if (Cell2 == face)
                    return Cell1;

                throw new ArgumentOutOfRangeException(nameof(face), "Unknown cell");
            }

            public void Init(Face cell1, Face cell2)
            {
                Cell1 = cell1;
                Cell2 = cell2;
            }

            public override string ToString() => $"Edge {_id}, {Cell1?.Center.ToString() ?? "?"}|{Cell2?.Center.ToString() ?? "?"}, {Vertex1.Id}-{Vertex2.Id}";

            private readonly Mesh<TFaceData> _mesh;
            private readonly int _id;

            #region IEquatable

            public bool Equals(Edge other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(_mesh, other._mesh) && IsConnects(other.Vertex1, other.Vertex2);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Edge)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return _mesh.GetHashCode() ^ (Vertex1.Id << 16) ^ (Vertex2.Id);
                }
            }

            public static bool operator ==(Edge left, Edge right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Edge left, Edge right)
            {
                return !Equals(left, right);
            }

            #endregion
        }

        public class Vertice : BaseNode
        {
            public const int MaxNeighborsCount = 4;

            public readonly Vector2 Coords;
            public Face[] Faces { get; private set; }
            public Edge[] Edges { get; private set; }

            /// <summary>
            /// Vertex of macro map
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="id"></param>
            public Vertice(Mesh<TFaceData> mesh, int id, Vector2 coords) : base(mesh, id)
            {
                Coords = coords;
            }

            public void Init([NotNull] IEnumerable<Face> cells, IEnumerable<Edge> edges)
            {
                if (cells == null) throw new ArgumentNullException(nameof(cells));


                Faces = cells.ToArray();
                Edges = edges.ToArray();

                Assert.IsTrue(Faces.Length >= 1 && Faces.Length <= 3);
            }

            public override string ToString() => $"Vert {Id}, cells: {Faces?.ToJoinedString(c => c.Center.ToString())}";
        }



        public class Submesh : Mesh<TFaceData>
        {
            public Submesh([NotNull] Mesh<TFaceData> parent, [NotNull] IEnumerable<Face> cells) : base()
            {
                if (parent == null) throw new ArgumentNullException("parent");
                if (cells == null) throw new ArgumentNullException("cells");

                _parent = parent;
                _nodes.AddRange(cells);
                Assert.IsTrue(Faces.All(c => c.Parent == parent));
            }

            public override bool Contains(Face face)
            {
                var result = _nodes.BinarySearch(face, BaseNode.IdIncComparer);
                return result >= 0;
            }

            /// <summary>
            /// Get cells that has neighbors outside submesh
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Face> GetBorderCells()
            {
                if (_borderFaces == null)
                    _borderFaces = Faces.Where(c => !c.Neighbors.All(Contains)).ToArray();
                return _borderFaces;
            }

            /// <summary>
            /// All cells that's not border
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Face> GetInnerCells()
            {
                return Faces.Where(c => c.Neighbors.All(Contains));
            }

            public FloodFiller FloodFill([NotNull] Face[] start, Predicate<Face> searchFor = null)
            {
                if (start == null) throw new ArgumentNullException("start");
                Assert.IsTrue(start.All(c => Faces.Contains(c)));

                if (searchFor != null)
                    return _parent.FloodFill(start, cell => Contains(cell) && searchFor(cell));
                else
                    return _parent.FloodFill(start, Contains);
            }

            public override FloodFiller FloodFill(Face start, Predicate<Face> searchFor = null)
            {
                if (start == null) throw new ArgumentNullException("start");
                Assert.IsTrue(Contains(start));

                if (searchFor != null)
                    return _parent.FloodFill(start, cell => Contains(cell) && searchFor(cell));
                else
                    return _parent.FloodFill(start, Contains);
            }

            [NotNull]
            private readonly Mesh<TFaceData> _parent;

            private Face[] _borderFaces;
        }

        private Face[] _faces;
        private Edge[] _edges;
        private Vertice[] _vertices;

        //todo make proper 2d Bound class (BoxBound, CircleBound, etc...)
        private Box2 Combine(Box2 first, Box2 second)
        {
            return new Box2(Math.Min(first.Left, second.Left), Math.Max(first.Top, second.Top), Math.Max(first.Right, second.Right), Math.Min(first.Bottom, second.Bottom));
        }
    }
}
