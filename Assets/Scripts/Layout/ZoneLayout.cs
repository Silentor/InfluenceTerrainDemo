using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Desciption of Zone layout
    /// </summary>
    public struct ZoneLayout
    {
        public readonly Vector2 Center;
        public readonly ZoneType Type;
        public readonly Cell Cell;

        /// <summary>
        /// World bounds
        /// </summary>
        public readonly Bounds2i Bounds;

        public readonly Bounds2i ChunkBounds;

        public IEnumerable<ZoneLayout> Neighbors { get { return _neighbors; } }

        public static readonly IEqualityComparer<ZoneLayout> TypeComparer = new ZoneTypeComparer();

        public ZoneLayout(ZoneType type, Cell cell)
        {
            Cell = cell;
            Center = cell.Center;
            Type = type;
            Bounds = (Bounds2i)cell.Bounds;
            ChunkBounds = new Bounds2i(Chunk.GetPosition(Bounds.Min), Chunk.GetPosition(Bounds.Max));
            _neighbors = new ZoneLayout[0];
        }


        /// <summary>
        /// Rasterize zone to blocks (triangles method)
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Vector2i> GetBlocks()
        {
            for (int i = 1; i < Cell.Vertices.Length - 1; i++)
                foreach (var pos in FillTriangle(Cell.Vertices[0], Cell.Vertices[i], Cell.Vertices[i + 1]))
                    if (Bounds.Contains(pos))
                        yield return pos;
        }

        /// <summary>
        /// Rasterize zone to blocks (polygon method)
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Vector2i> GetBlocks2()
        {
            var edges = new List<Vector2i>();
            foreach (var edge in Cell.Edges)
                edges.AddRange(DDA(edge.Vertex1, edge.Vertex2));

            edges = edges.Distinct().ToList();
            edges.Sort();

            for (int i = 0; i < edges.Count;)
            {
                if (i == edges.Count - 1)
                {
                    yield return edges.Last();
                    yield break;
                }

                var z1 = edges[i].Z;
                var j = i;
                while (j < edges.Count - 1 && edges[j + 1].Z == z1)
                    j++;

                for (var x = edges[i].X; x <= edges[j].X; x++)
                    yield return new Vector2i(x, z1);

                i = j + 1;
            }

            //foreach (var vector2I in edges)
            //{
            //    yield return vector2I;
            //}
        }

        private readonly ZoneLayout[] _neighbors;

        public static bool operator ==(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell == z2.Cell;
        }

        public static bool operator !=(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell != z2.Cell;
        }

        private IEnumerable<Vector2i> FillTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            //Based on http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html

            var v1i = (Vector2i)v1;
            var v2i = (Vector2i)v2;
            var v3i = (Vector2i)v3;

            if (v1i.Z > v2i.Z)
            {
                Swap(ref v1, ref v2);
                Swap(ref v1i, ref v2i);
            }
            if (v1i.Z > v3i.Z)
            {
                Swap(ref v1, ref v3);
                Swap(ref v1i, ref v3i);
            }
            if (v2i.Z > v3i.Z)
            {
                Swap(ref v2, ref v3);
                Swap(ref v2i, ref v3i);
            }

            if (v1i.Z == v3i.Z) yield break;

            if (v1i.Z == v2i.Z)
                foreach (var pos in FillTopFlatTriangle(v1i, v2i, v3i))
                    yield return pos;

            else if (v2i.Z == v3i.Z)
                foreach (var pos in FillBottomFlatTriangle(v1i, v2i, v3i))
                    yield return pos;
            else
            {
                var v4 = new Vector2(v1.x + (v2.y - v1.y) / (v3.y - v1.y) * (v3.x - v1.x), v2.y);
                foreach (var pos in FillBottomFlatTriangle(v1i, v2i, (Vector2i) v4))
                    yield return pos;
                foreach (var pos in FillTopFlatTriangle(v2i, (Vector2i)v4, v3i))
                    yield return pos;
            }
        }

        private static void Swap<T>(ref T first, ref T second)
        {
            var temp = first;
            first = second;
            second = temp;
        }

        private IEnumerable<Vector2i> FillBottomFlatTriangle(Vector2i v1, Vector2i v2, Vector2i v3)
        {
            if (v2.X > v3.X)
            {
                Swap(ref v2, ref v3);
            }

            var invslope1 = (double)(v2.X - v1.X) / (v2.Z - v1.Z);
            var invslope2 = (double)(v3.X - v1.X) / (v3.Z - v1.Z);

            var curx1 = (double)v1.X;
            var curx2 = (double)v1.X;

            for (var scanlineY = v1.Z; scanlineY <= v2.Z; scanlineY++)
            {
                var x2 = (int)curx2;
                for (var x = (int) curx1; x <= x2; x++)
                    yield return new Vector2i(x, scanlineY);
                curx1 += invslope1;
                curx2 += invslope2;
            }
        }

        private IEnumerable<Vector2i> FillTopFlatTriangle(Vector2i v1, Vector2i v2, Vector2i v3)
        {
            if (v1.X > v2.X)
            {
                Swap(ref v2, ref v1);
            }

            var invslope1 = (double)(v3.X - v1.X) / (v3.Z - v1.Z);
            var invslope2 = (double)(v3.X - v2.X) / (v3.Z - v2.Z);

            var curx1 = (double)v3.X;
            var curx2 = (double)v3.X;

            for (var scanlineY = v3.Z; scanlineY > v1.Z; scanlineY--)
            {
                curx1 -= invslope1;
                curx2 -= invslope2;
                var x2 = (int)curx2;
                for (var x = (int) curx1; x <= x2; x++)
                    yield return new Vector2i(x, scanlineY);
            }
        }

        private IEnumerable<Vector2i> BresenhamInt(Vector2i p1, Vector2i p2)
        {
            //Based http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf

            var changed = false;
            var x = p1.X;
            var y = p1.Z;
            var dx = Math.Abs(p2.X - p1.X);
            var dy = Math.Abs(p2.Z - p1.Z);
            var signx = Math.Sign(p2.X - p1.X);
            var signy = Math.Sign(p2.Z - p1.Z);
            if (dy > dx)
            {
                Swap(ref dx, ref dy);
                changed = true;
            }
            var e = 2 * dy - dx;
            for (var i = 1; i <= dx; i++)
            {
                yield return new Vector2i(x, y);
                while (e >= 0)
                {
                    if (changed)
                        x = x + 1;
                    else
                        y = y + 1;
                    e = e - 2 * dx;
                }
                if (changed)
                    y += signy;
                else
                    x += signx;
                e = e + 2 * dy;
            }
        }

        public static IEnumerable<Vector2i> DDA(Vector2i p1, Vector2i p2)
        {
            //Base on http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf
            //Todo replace by Bresenham algorithm

            float len = Mathf.Max(Mathf.Abs(p2.X - p1.X), Mathf.Abs(p2.Z - p1.Z));

            //Short path
            if (Mathf.Approximately(len, 0))
            {
                yield return p1;
                yield break;
            }

            // calculate increments
            var dx = (p2.X - p1.X) / len;
            var dy = (p2.Z - p1.Z) / len;

            // start point
            float x = p1.X;
            float y = p1.Z;

            for (var i = 0; i < len; i++)
            {
                yield return new Vector2i(x, y);
                x = x + dx;
                y = y + dy;
            }

            // set final pixel
            yield return p2;
        }

        public static IEnumerable<Vector2i> DDA(Vector2 p1, Vector2 p2)
        {
            //Base on http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf
            //Todo replace by Bresenham algorithm

            float len = Mathf.Max(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y));

            //Short path
            if (Mathf.Approximately(len, 0))
            {
                yield return (Vector2i)p1;
                yield break;
            }

            // calculate increments
            var dx = (p2.x - p1.x) / len;
            var dy = (p2.y - p1.y) / len;

            // start point
            float x = p1.x;
            float y = p1.y;

            for (var i = 0; i < len; i++)
            {
                yield return new Vector2i(x, y);
                x = x + dx;
                y = y + dy;
            }

            // set final pixel
            yield return (Vector2i)p2;
        }


        private class ZoneTypeComparer : IEqualityComparer<ZoneLayout>
        {
            public bool Equals(ZoneLayout x, ZoneLayout y)
            {
                return x.Type == y.Type;
            }

            public int GetHashCode(ZoneLayout obj)
            {
                return (int)obj.Type;
            }
        }
    }
}
