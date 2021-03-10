using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Ray = TerrainDemo.Spatial.Ray;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Macro
{
    //[DebuggerDisplay("TriCell {Id}({North.Id}, {East.Id}, {South.Id})")]
    public class Cell : IEquatable<Cell>
    {
        public const int MaxNeighborsCount = 6;
        public const int InvalidCellId = -1;

        public HexPos Position { get; }
        public uint    ZoneId => Zone.Id;

        //public readonly MacroMap Map;
        public readonly Box2 Bound;

        //Vertices

        public IEnumerable<Cell> NeighborsSafe => Neighbors.Where(n => n != null);

        public IEnumerable<Cell> Neighbors => _grid.GetNeighborsValue ( Position );

        public MacroGrid.VerticesData Vertices => _grid.GetVerticesValue( Position );

       
        /// <summary>
        /// Planned height for this cell
        /// </summary>
        public Heights DesiredHeight;

        public Vector2 Center => _grid.GetFaceCenter( Position );

        public Zone Zone { get; }

        public BiomeSettings Biome => Zone.Biome;

        //Ready after Macromap building completed
        #region 3D properties   

        //Actual height based on neighbor cells
		//public OpenToolkit.Mathematics.Vector3 CenterPoint
		//{
		//	get
		//	{
		//		if (!_centerPoint.HasValue)
		//			_centerPoint = new Vector3(Center.X, Map.GetHeight(Center).Nominal, Center.Y);

		//		return _centerPoint.Value;
		//	}
		//}

		//public IReadOnlyList<Vector3> GetCorners()
		//{
		//    if (_corners == null)
		//    {
		//        _corners = new[]
		//        {
		//            new Vector3(Vertices[0].Position.X, Map.GetHeight(Vertices[0].Position).Nominal, Vertices[0].Position.Y),
		//            new Vector3(Vertices[1].Position.X, Map.GetHeight(Vertices[1].Position).Nominal, Vertices[1].Position.Y),
		//            new Vector3(Vertices[2].Position.X, Map.GetHeight(Vertices[2].Position).Nominal, Vertices[2].Position.Y),
		//            new Vector3(Vertices[3].Position.X, Map.GetHeight(Vertices[3].Position).Nominal, Vertices[3].Position.Y),
		//            new Vector3(Vertices[4].Position.X, Map.GetHeight(Vertices[4].Position).Nominal, Vertices[4].Position.Y),
		//            new Vector3(Vertices[5].Position.X, Map.GetHeight(Vertices[5].Position).Nominal, Vertices[5].Position.Y),
		//        };
		//    }

		//    return _corners;
		//}


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

		public Cell( HexPos position, Zone zone, MacroGrid macroGrid )
		{
			Position = position;
	        Zone     = zone;
	        _grid    = macroGrid;
	        Bound   = macroGrid.GetFaceBound( position );
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

        public bool Contains(Vector2 position )
        {
	        return _grid.BlockToHex( (GridPos) position ) == Position;
        }

        //public Vector3? Raycast(Ray ray)
        //{
        //    var corners = GetCorners();

        //    for (int i = 0; i < corners.Count; i++)
        //    {
        //        var v1 = corners[i];
        //        var v2 = corners[(i + 1) % corners.Count];

        //        if (Intersections.LineTriangleIntersection(ray, v1, v2, CenterPoint, out var distance) == 1)
        //            return ray.GetPoint(distance);
        //    }

        //    return null;
        //}

        public override string ToString()
        {
	        var n = Neighbors.ToArray( );
            return $"TriCell {Position}({n[0]?.Position.ToString() ?? "?"}, {n[1]?.Position.ToString() ?? "?"}, {n[2]?.Position.ToString() ?? "?"}, {n[3]?.Position.ToString() ?? "?"}, {n[4]?.Position.ToString() ?? "?"}, {n[5]?.Position.ToString() ?? "?"})";
        }

        private          Zone                              _zone;
        private          double[]                          _influence;
        private          OpenToolkit.Mathematics.Vector3?  _centerPoint;
        private          OpenToolkit.Mathematics.Vector3[] _corners;
        private readonly MacroGrid                         _grid;


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

        #region IEquatable

        public bool Equals(Cell other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Position, other.Position);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Cell) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public static bool operator ==(Cell left, Cell right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
