using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Tools;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Spatial
{
    public class Mesh<TFaceData, TEdgeData, TVertexData> 
        where TFaceData : Mesh<TFaceData, TEdgeData, TVertexData>.IFaceOwner
        where TEdgeData : Mesh<TFaceData, TEdgeData, TVertexData>.IEdgeOwner
        where TVertexData : Mesh<TFaceData, TEdgeData, TVertexData>.IVertexOwner
    {
        public Box2 Bounds { get; }
        public IReadOnlyCollection<TFaceData> Faces => _facesData;
        public IReadOnlyCollection<TEdgeData> Edges => _edgesData;
        public IReadOnlyCollection<TVertexData> Vertices => _verticesData;

        public Mesh(IEnumerable<Vector2[]> facesFromVertices)
        {
            var vertices = new List<Vertex>();
            var edges = new List<Edge>();
            var faces = new List<Face>();

            foreach (var newFace in facesFromVertices)
            {
                //Create vertices
                var faceVertices = newFace.Select(v => GetVertex(v, vertices)).ToArray();

                //Create edges
                var faceEdges = new Edge[faceVertices.Length];
                for (int i = 0; i < faceVertices.Count(); i++)
                {
                    var vert1 = faceVertices[i];
                    var vert2 = faceVertices[(i + 1) % faceVertices.Length];
                    var edge = GetEdge(vert1, vert2, edges);
                    faceEdges[i] = edge;
                }

                //Create face
                var face = new Face(this, faces.Count, faceVertices, faceEdges);
                faces.Add(face);
            }

            //Update mesh elements internals
            foreach (var vertex in vertices)
            {
                var adjacentEdges = edges.Where(e => e.Vertex1 == vertex || e.Vertex2 == vertex).ToArray();
                var neighborVertices = adjacentEdges.Select(e => e.GetOppositeOf(vertex)).ToArray();
                var adjacentFaces = faces.Where(f => f.Vertices.Contains(vertex)).ToArray();
                vertex.Init(adjacentFaces, adjacentEdges, neighborVertices);
            }

            foreach (var edge in edges)
            {
                var adjacentFaces = faces.Where(f => f.Edges.Contains(edge)).ToArray();
                Assert.IsTrue(adjacentFaces.Length >= 1 && adjacentFaces.Length <= 2);
                edge.Init(adjacentFaces[0], adjacentFaces.Length > 1 ? adjacentFaces[1] : null);
            }

            foreach (var face in faces)
            {
                var neighbors = face.Edges.Select(e => e.GetOppositeOf(face)).Where(f => f != null).ToArray();
                face.Init(neighbors);
            }

            _faces = faces.ToArray();
            _edges = edges.ToArray();
            _vertices = vertices.ToArray();

            //Calculate mesh bounds (todo make proper operations on my bounds)
            Bounds = _faces[0].Bounds;
            for (var i = 1; i < _faces.Length; i++)
                Bounds = Combine(Bounds, _faces[i].Bounds);
        }

        private Mesh(IEnumerable<Face> submesh)
        {
            _faces = submesh.ToArray();
            _edges = _faces.SelectMany(f => f.Edges).Distinct().ToArray();
            _vertices = _faces.SelectMany(f => f.Vertices).Distinct().ToArray();

            _facesData = _faces.Select(f => f.Data).ToArray();
            _edgesData = _edges.Select(e => e.Data).ToArray();
            _verticesData = _vertices.Select(v => v.Data).ToArray();

            //Calculate mesh bounds (todo make proper operations on my bounds)
            Bounds = _faces[0].Bounds;
            for (var i = 1; i < _faces.Length; i++)
                Bounds = Combine(Bounds, _faces[i].Bounds);
        }

        public void AssignData(Func<Vertex, TVertexData> assingVertex, 
            Func<Edge, TVertexData, TVertexData, TEdgeData> assignEdge, 
            Func<Face, IEnumerable<TVertexData>, IEnumerable<TEdgeData>, TFaceData> assignFace)
        {
            _verticesData = new TVertexData[_vertices.Length];
            for (var i = 0; i < _vertices.Length; i++)
            {
                var vertex = _vertices[i];
                var vertexData = assingVertex(vertex);
                vertex.Data = vertexData;
                _verticesData[i] = vertexData;
            }

            _edgesData = new TEdgeData[_edges.Length];
            for (var i = 0; i < _edges.Length; i++)
            {
                var edge = _edges[i];
                var vertex1 = edge.Vertex1.Data;
                var vertex2 = edge.Vertex2.Data;
                var edgeData = assignEdge(edge, vertex1, vertex2);
                edge.Data = edgeData;
                _edgesData[i] = edgeData;
            }

            _facesData = new TFaceData[_faces.Length];
            for (var i = 0; i < _faces.Length; i++)
            {
                var face = _faces[i];
                var vertices = face.Vertices.Select(v => v.Data);
                var edges = face.Edges.Select(e => e.Data);
                var faceData = assignFace(face, vertices, edges);
                face.Data = faceData;
                _facesData[i] = faceData;
            }
        }

        /// <summary>
        /// Get nearest (containing) cell for given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        [Pure]
        public Face GetFaceFor(Vector2 position)
        {
            if (!Bounds.Contains(position))
                return null;

            var minDistance = float.MaxValue;
            Face result = null;

            for (int i = 0; i < _faces.Length; i++)
            {
                var cellDistance = Vector2.DistanceSquared(position, _faces[i].Center);
                if (cellDistance < minDistance)
                {
                    minDistance = cellDistance;
                    result = _faces[i];
                }
            }

            return result;
        }

        public TFaceData GetNearestFace(Vector2 position)
        {
            var distance = float.MaxValue;
            var result = _faces.First();
            foreach (var face in _faces)
            {
                if (Vector2.DistanceSquared(position, face.Center) < distance)
                {
                    distance = Vector2.DistanceSquared(position, face.Center);
                    result = face;
                }
            }

            return result.Data;
        }

        public IEnumerable<TFaceData> GetAdjacentFaces(TVertexData vertex)
        {
            return vertex.Vertex.AdjacentFaces.Select(f => f.Data);
        }

        public IEnumerable<TEdgeData> GetAdjacentEdges(TVertexData vertex)
        {
            return vertex.Vertex.AdjacentEdges.Select(e => e.Data);
        }

        public IEnumerable<TEdgeData> GetAdjacentEdges(TFaceData face)
        {
            return face.Face.Edges.Select(e => e.Data);
        }

        public Tuple<TFaceData, TFaceData> GetAdjacentFaces(TEdgeData edge)
        {
            var e = edge.Edge;
            var face1 = e.Face1.Data;
            var face2 = e.Face2 != null ? e.Face2.Data : default(TFaceData);
            return new Tuple<TFaceData, TFaceData>(face1, face2);
        }

        public IEnumerable<TFaceData> FloodFill(TFaceData startFace, Predicate<TFaceData> fillCondition = null)
        {
            var result = new FloodFillEnumerator(this, startFace.Face, fillCondition);
            for (int distance = 0; distance < 10; distance++)
            {
                foreach (var cell in result.GetNeighbors(distance))
                    yield return cell.Data;
            }
        }

        public Submesh GetSubmesh(IEnumerable<TFaceData> submeshFaces)
        {
            return new Submesh(this, submeshFaces.Select(f => f.Face));
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

        /*
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
            }\
        }
        */

        //[DebuggerDisplay("TriCell {Id}({North.Id}, {East.Id}, {South.Id})")]
        protected readonly Face[] _faces;
        protected readonly Edge[] _edges;
        protected readonly Vertex[] _vertices;

        private TFaceData[] _facesData;
        private TEdgeData[] _edgesData;
        private TVertexData[] _verticesData;

        //todo make proper 2d Bound class (BoxBound, CircleBound, etc...)
        protected virtual bool Contains(Face face)
        {
            return face.Mesh == this;
        }

        private Box2 Combine(Box2 first, Box2 second)
        {
            return new Box2(Math.Min(first.Left, second.Left), Math.Max(first.Top, second.Top), Math.Max(first.Right, second.Right), Math.Min(first.Bottom, second.Bottom));
        }

        private Vertex GetVertex(Vector2 position, List<Vertex> currentVertices)
        {
            var result = currentVertices.Find(v => Vector2.DistanceSquared(v.Position, position) < 0.1 * 0.1);
            if (result == null)
            {
                result = new Vertex(this, currentVertices.Count, position);
                currentVertices.Add(result);
            }

            return result;
        }

        private Edge GetEdge(Vertex vert1, Vertex vert2, List<Edge> edges)
        {
            var edge = edges.Find(e => e.IsConnects(vert1, vert2));
            if (edge == null)
            {
                edge = new Edge(this, edges.Count, vert1, vert2);
                edges.Add(edge);
            }

            return edge;
        }


        public class Face : IComparable<Face>
        {
            public readonly int Id;
            public readonly Vector2 Center;
            public readonly Box2 Bounds; //todo replace box bound with containing circle/other primitive?
            public Mesh<TFaceData, TEdgeData, TVertexData> Mesh { get; }

            public IReadOnlyCollection<Face> Neighbors => _neighbors;

            public IReadOnlyCollection<Vertex> Vertices => _vertices;

            public IReadOnlyCollection<Edge> Edges => _edges;

            public TFaceData Data
            {
                get { return _data; }
                set
                {
                    if (!_dataInitialized)
                    {
                        _dataInitialized = true;
                        _data = value;
                    }
                    else
                        throw new InvalidOperationException();
                }
            }


            //public static readonly IdComparer IdIncComparer = new IdComparer();
            //public static readonly CellEqualityComparer CellComparer = new CellEqualityComparer();

            public Face(Mesh<TFaceData, TEdgeData, TVertexData> mesh, int id, [NotNull] Vertex[] vertices, [NotNull] Edge[] edges)
            {
                if (vertices == null) throw new ArgumentNullException(nameof(vertices));
                if (edges == null) throw new ArgumentNullException(nameof(edges));
                Assert.IsTrue(vertices.Distinct().Count() == vertices.Length);
                Assert.IsTrue(edges.Distinct().Count() == edges.Length);

                Mesh = mesh;
                Id = id;
                _vertices = vertices;
                _edges = edges;

                //Build bounding box and calc geometrical center (for bounding circle)
                float top = float.MinValue, bottom = float.MaxValue, left = float.MaxValue, right = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Center += vertices[i].Position;
                    if (vertices[i].Position.X > right) right = vertices[i].Position.X;
                    if (vertices[i].Position.X < left) left = vertices[i].Position.X;
                    if (vertices[i].Position.Y > top) top = vertices[i].Position.Y;
                    if (vertices[i].Position.Y < bottom) bottom = vertices[i].Position.Y;
                }
                Center /= vertices.Length;

                Bounds = new Box2(left, top, right, bottom);

                //Build edges halfplanes for Contains test
                _halfPlanes = new HalfPlane[edges.Length];
                for (int i = 0; i < edges.Length; i++)
                    _halfPlanes[i] = new HalfPlane(edges[i].Vertex1.Position, edges[i].Vertex2.Position, Center);
            }

            public void Init(IEnumerable<Face> neighbors)
            {
                _neighbors = neighbors.ToArray();
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
                return $"TriCell {Center}({_neighbors[0]?.Center.ToString() ?? "?"}, {_neighbors[1]?.Center.ToString() ?? "?"}, {_neighbors[2]?.Center.ToString() ?? "?"}, {_neighbors[3]?.Center.ToString() ?? "?"}, {_neighbors[4]?.Center.ToString() ?? "?"}, {_neighbors[5]?.Center.ToString() ?? "?"})";
            }

            private readonly HalfPlane[] _halfPlanes;
            private readonly Vertex[] _vertices;
            private readonly Edge[] _edges;
            private Face[] _neighbors;
            private TFaceData _data;
            private bool _dataInitialized;

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
            public int CompareTo(Face other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return Id.CompareTo(other.Id);
            }
        }

        public class Edge : IEquatable<Edge>
        {
            public readonly int Id;
            public Vertex Vertex1 { get; }

            public Vertex Vertex2 { get; }

            public Face Face1 { get; private set; }

            public Face Face2 { get; private set; }

            public readonly Mesh<TFaceData, TEdgeData, TVertexData> Owner;

            public TEdgeData Data
            {
                get { return _data; }
                set
                {
                    if (!_dataInitialized)
                    {
                        _dataInitialized = true;
                        _data = value;
                    }
                    else
                        throw new InvalidOperationException();
                }
            }


            public Edge([NotNull] Mesh<TFaceData, TEdgeData, TVertexData> mesh, int id, [NotNull] Vertex vertex1, [NotNull] Vertex vertex2)
            {
                if (mesh == null) throw new ArgumentNullException(nameof(mesh));
                if (vertex1 == null) throw new ArgumentNullException(nameof(vertex1));
                if (vertex2 == null) throw new ArgumentNullException(nameof(vertex2));

                Id = id;
                Vertex1 = vertex1;
                Vertex2 = vertex2;
                Owner = mesh;
            }

            public bool IsConnects(Vertex vert1, Vertex vert2)
            {
                return (vert1 == Vertex1 && vert2 == Vertex2) || (vert1 == Vertex2 && vert2 == Vertex1);
            }

            public Face GetOppositeOf([NotNull] Face face)
            {
                if (face == null) throw new ArgumentNullException(nameof(face));

                if (Face1 == face)
                    return Face2;
                else if (Face2 == face)
                    return Face1;

                throw new ArgumentOutOfRangeException(nameof(face), $"Unknown cell {face}");
            }

            public Vertex GetOppositeOf([NotNull] Vertex vertex)
            {
                if (vertex == null) throw new ArgumentNullException(nameof(vertex));

                if (Vertex1 == vertex)
                    return Vertex2;
                else if (Vertex2 == vertex)
                    return Vertex1;

                throw new ArgumentOutOfRangeException(nameof(vertex), $"Unknown vertex {vertex}");
            }


            public void Init(Face face1, Face face2)
            {
                Face1 = face1;
                Face2 = face2;
            }

            public override string ToString() => $"Edge {Id}, {Face1?.Center.ToString() ?? "?"}|{Face2?.Center.ToString() ?? "?"}, {Vertex1.Id}-{Vertex2.Id}";
            private TEdgeData _data;
            private bool _dataInitialized;

            #region IEquatable

            public bool Equals(Edge other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Owner, other.Owner) && IsConnects(other.Vertex1, other.Vertex2);
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
                    return Owner.GetHashCode() ^ (Vertex1.Id << 16) ^ (Vertex2.Id);
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

        public class Vertex
        {
            public const int MaxNeighborsCount = 4;

            public readonly int Id;
            public readonly Vector2 Position;
            public IReadOnlyCollection<Face> AdjacentFaces => _faces;
            public IReadOnlyCollection<Edge> AdjacentEdges => _edges;
            public IReadOnlyCollection<Vertex> Neighbors => _neighbors;

            public TVertexData Data
            {
                get { return _data; }
                set
                {
                    if (!_dataInitialized)
                    {
                        _dataInitialized = true;
                        _data = value;
                    }
                    else
                        throw new InvalidOperationException();
                }
            }

            /// <summary>
            /// Vertex of macro map
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="id"></param>
            public Vertex(Mesh<TFaceData, TEdgeData, TVertexData> mesh, int id, Vector2 position)
            {
                Id = id;
                Position = position;
                _mesh = mesh;
            }

            public void Init([NotNull] IEnumerable<Face> adjacentFaces, IEnumerable<Edge> adjacentEdges, IEnumerable<Vertex> neighbors)
            {
                if (adjacentFaces == null) throw new ArgumentNullException(nameof(adjacentFaces));

                _faces = adjacentFaces.ToArray();
                _edges = adjacentEdges.ToArray();
                _neighbors = neighbors.ToArray();
                
                Assert.IsTrue(_edges.Count() >= 2);
                Assert.IsTrue(_faces.Length >= 1);
            }

            public override string ToString() => $"Vert {Id}, cells: {AdjacentFaces?.ToJoinedString(c => c.Center.ToString())}";
            private Mesh<TFaceData, TEdgeData, TVertexData> _mesh;

            private Vertex[] _neighbors;
            private Face[] _faces;
            private Edge[] _edges;
            private TVertexData _data;
            private bool _dataInitialized;
        }

        public class FloodFillEnumerator
        {
            private readonly Mesh<TFaceData, TEdgeData, TVertexData> _mesh;
            private readonly List<List<Face>> _neighbors = new List<List<Face>>();
            private readonly Predicate<TFaceData> _fillCondition;

            /// <summary>
            /// Create flood-fill around <see cref="start"> cell
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFillEnumerator(Mesh<TFaceData, TEdgeData, TVertexData> mesh, Face start, Predicate<TFaceData> fillCondition = null)
            {
                Assert.IsTrue(mesh.Contains(start));
                Assert.IsTrue(fillCondition == null || fillCondition(start.Data));

                _mesh = mesh;
                _neighbors.Add(new List<Face> { start });
                _fillCondition = fillCondition;
            }

            /// <summary>
            /// Create flood-fill around <see cref="start"> cells
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFillEnumerator(Mesh<TFaceData, TEdgeData, TVertexData> mesh, Face[] start)
            {
                Assert.IsTrue(start.All(mesh.Contains));

                _mesh = mesh;
                var startStep = new List<Face>();
                startStep.AddRange(start);
                _neighbors.Add(startStep);
            }


            /// <summary>
            /// Get cells from start cell(s) by distance
            /// </summary>
            /// <param name="distance">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
            /// <returns></returns>
            public IEnumerable<Face> GetNeighbors(int distance)
            {
                if (distance == 0)
                    return _neighbors[0];

                if (distance < _neighbors.Count)
                    return _neighbors[distance];

                //Calculate neighbors
                if (distance - 1 < _neighbors.Count)
                {
                    var processedCellsIndex = Math.Max(0, distance - 2);
                    var result = GetNeighbors(_neighbors[distance - 1], _neighbors[processedCellsIndex]);
                    _neighbors.Add(result);
                    return result;
                }
                else
                {
                    //Calculate previous steps (because result of step=n used for step=n+1)
                    for (int i = _neighbors.Count; i < distance; i++)
                        GetNeighbors(i);
                    return GetNeighbors(distance);
                }
            }

            /// <summary>
            /// Get neighbors of <see cref="faces"/> doesnt contained in <see cref="alreadyProcessed"/>
            /// </summary>
            /// <param name="faces"></param>
            /// <param name="alreadyProcessed"></param>
            /// <returns></returns>
            private List<Face> GetNeighbors(List<Face> faces, List<Face> alreadyProcessed)
            {
                var result = new List<Face>();
                foreach (var neigh1 in faces)
                {
                    foreach (var neigh2 in neigh1.Neighbors)
                    {
                        if (_mesh.Contains(neigh2) 
                            && (_fillCondition == null || _fillCondition(neigh2.Data))
                            && !result.Contains(neigh2) && !faces.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
                            result.Add(neigh2);
                    }
                }

                return result;
            }
        }

        public interface IFaceOwner
        {
            Face Face { get; }
        }

        public interface IEdgeOwner
        {
            Edge Edge { get; }
        }

        public interface IVertexOwner
        {
            Vertex Vertex { get; }
        }

        public class Submesh : Mesh<TFaceData, TEdgeData, TVertexData>
        {
            /// <summary>
            /// Get cells that has neighbors outside submesh
            /// </summary>
            /// <value></value>
            public IReadOnlyCollection<TFaceData> BorderFaces
            {
                get
                {
                    if (_borderFacesData == null)
                        _borderFacesData = _borderFaces.Select(f => f.Data).ToArray();
                    return _borderFacesData;
                }
            }

            public IReadOnlyCollection<TEdgeData> BorderEdges
            {
                get
                {
                    if (_borderEdgesData == null)
                        _borderEdgesData = _borderEdges.Select(e => e.Data).ToArray();
                    return _borderEdgesData;
                }
            }

            public Submesh([NotNull] Mesh<TFaceData, TEdgeData, TVertexData> parent, [NotNull] IEnumerable<Face> faces) : base(faces)
            {
                if (parent == null) throw new ArgumentNullException(nameof(parent));
                if (faces == null) throw new ArgumentNullException(nameof(faces));
                Assert.IsTrue(_faces.All(f => f.Mesh == parent));

                _parent = parent;
                Array.Sort(_faces);

                _borderFaces = _faces.Where(f => !f.Neighbors.All(Contains)).ToArray();
                _borderEdges = (from face in _borderFaces
                                from edge in face.Edges
                                where !Contains(edge.GetOppositeOf(face))
                                select edge).Distinct().ToArray();
            }

            /// <summary>
            /// All cells that's not border
            /// </summary>
            /// <returns></returns>
            public IEnumerable<TFaceData> GetInnerCells()
            {
                return Faces.Except(BorderFaces);
            }

            protected override bool Contains(Face face)
            {
                var result = Array.BinarySearch(_faces, face);
                return result >= 0;
            }

            [NotNull]
            private readonly Mesh<TFaceData, TEdgeData, TVertexData> _parent;

            private TFaceData[] _borderFacesData;
            private TEdgeData[] _borderEdgesData;
            private readonly Face[] _borderFaces;
            private readonly Edge[] _borderEdges;
        }
        
    }
}
