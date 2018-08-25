using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Macro
{
    //[DebuggerDisplay("TriCell {Id}({North.Id}, {East.Id}, {South.Id})")]
    public class Cell
    {
        public const int MaxNeighborsCount = 6;
        public const int InvalidZoneId = -1;
        public const int InvalidCellId = -1;

        public int Id => _face.Id;
        public readonly Vector2i Position;
        public int ZoneId = InvalidZoneId;
        public readonly MacroMap Map;
        public Box2 Bounds => _face.Bounds;

        //Vertices

        public IEnumerable<MacroVert> Vertices2 => _vertices;
        public IReadOnlyList<MacroVert> Vertices3 => _vertices;

        public IEnumerable<Cell> NeighborsSafe => _neighbors.Where(n => n != null);

        public IEnumerable<Cell> Neighbors => _neighbors;
        

        public IEnumerable<MacroEdge> Edges => _edges;
       
        //Heights
        public float Height;

        public float[] MicroHeights { get; private set; }

        public readonly Vector2 Center;

        public Zone Zone
        {
            get { return _zone ?? (_zone = Map.Zones.First(z => z.Id == ZoneId)); }
        }

        //From cell "top" clockwise
        public static IEnumerable<Sides> AllSides
        {
            get
            {
                yield return Sides.ZPositive;           
                yield return Sides.YPositive;
                yield return Sides.XPositive;
                yield return Sides.ZNegative;
                yield return Sides.YNegative;
                yield return Sides.XNegative;
            }
        }

        //From cell "top" clockwise
        //      Z+
        // X-  ---  Y+ 
        //    /   \
        //    \   /
        // Y-  ---  X+ 
        //      Z-
        public static readonly Vector2i[] Directions =
        {
            new Vector2i(0, 1), new Vector2i(1, 1), new Vector2i(1, 0), new Vector2i(0, -1), new Vector2i(-1, -1), new Vector2i(-1, 0),
        };

        public TriBiome Biome;

        //public static readonly IdComparer IdIncComparer = new IdComparer();
        //public static readonly CellEqualityComparer CellComparer = new CellEqualityComparer();

        /*
        public Cell this[Sides side]
        {
            get { return _neighbors[(int) side]; }
            //set { _neighbors[(int) side] = value; }
        }
        */

        public Cell(MacroMap map, Vector2i position, MacroMap.CellMesh.Face face, IEnumerable<MacroVert> vertices, IEnumerable<MacroEdge> edges)
        {
            Assert.IsTrue(face.Vertices.Count() == MaxNeighborsCount);
            Assert.IsTrue(face.Edges.Count() == MaxNeighborsCount);

            _face = face;
            _vertices = face.Vertices.Select(v => vertices.First(v2 => v.Id == v2.Id)).ToArray();
            _edges = face.Edges.Select(e => edges.First(e2 => e.Id == e2.Id)).ToArray();
            Map = map;
            Position = position;
        }   

        public void Init(IEnumerable<Cell> neighbors)
        {
            Assert.IsTrue(neighbors.Count() == Cell.MaxNeighborsCount);

            _neighbors = neighbors.ToArray();

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

        public IEnumerable<Vector3> GetHeightPoints()
        {
            return new Vector3[Cell.MaxNeighborsCount];

            /*
            yield return new Vector3(SWPos.X, H1, SWPos.Y);
            yield return new Vector3(NWPos.X, H2, NWPos.Y);
            yield return new Vector3(NEPos.X, H3, NEPos.Y);
            yield return new Vector3(SEPos.X, H4, SEPos.Y);
            */
        }

        public override string ToString()
        {
            return $"TriCell {Position}({_neighbors[0]?.Position.ToString() ?? "?"}, {_neighbors[1]?.Position.ToString() ?? "?"}, {_neighbors[2]?.Position.ToString() ?? "?"}, {_neighbors[3]?.Position.ToString() ?? "?"}, {_neighbors[4]?.Position.ToString() ?? "?"}, {_neighbors[5]?.Position.ToString() ?? "?"})";
        }

        private readonly MacroMap.CellMesh.Face _face;
        private Zone _zone;
        private double[] _influence;
        private readonly MacroVert[] _vertices;
        private readonly MacroEdge[] _edges;
        private Cell[] _neighbors;

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
}
