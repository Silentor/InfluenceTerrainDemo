using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Macro
{
    //[DebuggerDisplay("TriCell {Id}({North.Id}, {East.Id}, {South.Id})")]
    public class Cell : MacroMap.CellMesh.IFaceOwner
    {
        public const int MaxNeighborsCount = 6;
        public const int InvalidCellId = -1;

        public int Id => _face.Id;
        public readonly Coord Coords;
        public int ZoneId = Zone.InvalidId;
        public readonly MacroMap Map;
        public Box2 Bounds => _face.Bounds;

        //Vertices

        public IReadOnlyList<MacroVert> Vertices => _vertices;

        public IEnumerable<Cell> NeighborsSafe => _neighbors.Where(n => n != null);

        public IEnumerable<Cell> Neighbors => _neighbors;
        

        public IEnumerable<MacroEdge> Edges => _edges;
       
        /// <summary>
        /// Planned height for this cell
        /// </summary>
        public Heights DesiredHeight;

        public float[] MicroHeights { get; private set; }

        public Vector2 Center => _face.Center;

        public Zone Zone
        {
            get { return _zone ?? (_zone = Map.Zones.First(z => z.Id == ZoneId)); }
        }

        public BiomeSettings Biome => Zone?.Biome;

        //Ready after Macromap building completed
        #region 3D properties   

        ///Actual height based on neighbor cells 
        public Vector3 CenterPoint
        {
            get
            {
                if (!_centerPoint.HasValue)
                    _centerPoint = new Vector3(Center.X, Map.GetHeight(Center).Nominal, Center.Y);

                return _centerPoint.Value;
            }
        }

        public IReadOnlyList<Vector3> GetCorners()
        {
            if (_corners == null)
            {
                _corners = new[]
                {
                    new Vector3(Vertices[0].Position.X, Map.GetHeight(Vertices[0].Position).Nominal, Vertices[0].Position.Y),
                    new Vector3(Vertices[1].Position.X, Map.GetHeight(Vertices[1].Position).Nominal, Vertices[1].Position.Y),
                    new Vector3(Vertices[2].Position.X, Map.GetHeight(Vertices[2].Position).Nominal, Vertices[2].Position.Y),
                    new Vector3(Vertices[3].Position.X, Map.GetHeight(Vertices[3].Position).Nominal, Vertices[3].Position.Y),
                    new Vector3(Vertices[4].Position.X, Map.GetHeight(Vertices[4].Position).Nominal, Vertices[4].Position.Y),
                    new Vector3(Vertices[5].Position.X, Map.GetHeight(Vertices[5].Position).Nominal, Vertices[5].Position.Y),
                };
            }

            return _corners;
        }


        #endregion


        //public static readonly IdComparer IdIncComparer = new IdComparer();
        //public static readonly CellEqualityComparer CellComparer = new CellEqualityComparer();

        /*
        public Cell this[Sides side]
        {
            get { return _neighbors[(int) side]; }
            //set { _neighbors[(int) side] = value; }
        }
        */

        public Cell(MacroMap map, Coord coords, MacroMap.CellMesh.Face face, IEnumerable<MacroVert> vertices, IEnumerable<MacroEdge> edges)
        {
            Assert.IsTrue(face.Vertices.Count() == MaxNeighborsCount);
            Assert.IsTrue(face.Edges.Count() == MaxNeighborsCount);

            _face = face;
            _vertices = vertices.ToArray();
            _edges = edges.ToArray();
            Map = map;
            Coords = coords;
        }   

        public void Init(IEnumerable<Cell> neighbors)
        {
            _neighbors = neighbors.ToArray();

            Assert.IsTrue(_neighbors.Count() == Cell.MaxNeighborsCount);
            //Do not accept same neigbours for different sides
            Assert.IsTrue(NeighborsSafe.Count() == NeighborsSafe.Distinct().Count());
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
            return _face.Contains(point);
        }

        public Vector3? IsIntersected(Ray ray)
        {
            var corners = GetCorners();

            for (int i = 0; i < corners.Count; i++)
            {
                var v1 = corners[i];
                var v2 = corners[(i + 1) % corners.Count];

                Vector3 intersectionPoint;
                if (Intersections.LineTriangleIntersection(ray, v1, v2, CenterPoint, out intersectionPoint) == 1)
                {
                    return intersectionPoint;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return $"TriCell {Coords}({_neighbors[0]?.Coords.ToString() ?? "?"}, {_neighbors[1]?.Coords.ToString() ?? "?"}, {_neighbors[2]?.Coords.ToString() ?? "?"}, {_neighbors[3]?.Coords.ToString() ?? "?"}, {_neighbors[4]?.Coords.ToString() ?? "?"}, {_neighbors[5]?.Coords.ToString() ?? "?"})";
        }

        private readonly MacroMap.CellMesh.Face _face;
        private Zone _zone;
        private double[] _influence;
        private readonly MacroVert[] _vertices;
        private readonly MacroEdge[] _edges;
        private Cell[] _neighbors;
        private Vector3? _centerPoint;
        private Vector3[] _corners;

        

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
        Mesh<Cell, MacroEdge, MacroVert>.Face Mesh<Cell, MacroEdge, MacroVert>.IFaceOwner.Face
        {
            get { return _face; }
        }
    }
}
