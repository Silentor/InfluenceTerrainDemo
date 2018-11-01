using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using TerrainDemo.Spatial;
using UnityEngine;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tools
{
    public static class Rasterization
    {
        public static IEnumerable<Vector2i> DDA(Vector2i p1, Vector2i p2, bool conservative)
        {
            return DDA(new Vector2(p1.X + 0.5f, p1.Z + 0.5f), new Vector2(p2.X + 0.5f, p2.Z + 0.5f), conservative);
        }

        /// <summary>
        /// Conservative rasterizing still need improvement
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="conservative"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2i> DDA(Vector2 p1, Vector2 p2, bool conservative)
        {
            //Based on http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf
            double len = Mathf.Max(Mathf.Abs(p2.X - p1.X), Mathf.Abs(p2.Y - p1.Y));

            //Short path
            if (Math.Abs(len) < 0.000001)
            {
                yield return (Vector2i)p1;
                yield break;
            }

            // calculate increments
            var dx = (p2.X - p1.X) / len;
            var dy = (p2.Y - p1.Y) / len;

            // start point
            double x = p1.X;
            double y = p1.Y;

            //Additional blocks for conservative rasterization
            var addX = new Vector2i(Math.Sign(dx), 0);
            var addZ = new Vector2i(0, Math.Sign(dy));

            var result = (Vector2i)p1;
            for (var i = 0; i < len; i++)
            {
                var newResult = new Vector2i(x, y);

                //Add conservative blocks todo improve, calculate only one additional block
                if (conservative && newResult.X != result.X && newResult.Z != result.Z)
                {
                    yield return result + addX;
                    yield return result + addZ;
                }

                yield return newResult;
                result = newResult;

                x = x + dx;
                y = y + dy;
            }

            // set final pixel
            if (result != (Vector2i) p2)
                yield return (Vector2i) p2;
        }

        public static IEnumerable<Vector3i> DDA(Vector3 p1, Vector3 p2)
        {
            //Based on http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf
            float len = Mathf.Max(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y), Mathf.Abs(p2.z - p1.z));

            //Short path
            if (Mathf.Approximately(len, 0))
            {
                yield return (Vector3i)p1;
                yield break;
            }

            // calculate increments
            var dx = (p2.x - p1.x) / len;
            var dy = (p2.y - p1.y) / len;
            var dz = (p2.z - p1.z) / len;

            // start point
            float x = p1.x;
            float y = p1.y;
            float z = p1.z;

            Vector3i result = (Vector3i)p1;
            for (var i = 0; i < len; i++)
            {
                result = new Vector3i(x, y, z);
                yield return result;
                x = x + dx;
                y = y + dy;
                z = z + dz;
            }

            // set final pixel
            if(result != (Vector3i)p2)
                yield return (Vector3i)p2;
        }

        /// <summary>
        /// Issues in some quadrants, need speed test against DDA
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2i> BresenhamInt(Vector2i p1, Vector2i p2)
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

        public static IEnumerable<Vector2i> lineNoDiag(int x0, int y0, int x1, int y1)
        {
            int xDist = Math.Abs(x1 - x0);
            int yDist = -Math.Abs(y1 - y0);
            int xStep = (x0 < x1 ? +1 : -1);
            int yStep = (y0 < y1 ? +1 : -1);
            int error = xDist + yDist;

            yield return new Vector2i(x0, y0);

            while (x0 != x1 || y0 != y1)
            {
                if (2 * error - yDist > xDist - 2 * error)
                {
                    // horizontal step
                    error += yDist;
                    x0 += xStep;
                }
                else
                {
                    // vertical step
                    error += xDist;
                    y0 += yStep;
                }

                yield return new Vector2i(x0, y0);
            }
        }

        public static IEnumerable<Vector2i> Bresenham2(int x0, int y0, int x1, int y1)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);

            var sx = -1;
            var sy = -1;

            if (x0 < x1)
                sx = 1;
            if (y0 < y1)
                sy = 1;

            var err = dx - dy;
            int e2;

            while (true)
            {
                yield return new Vector2i(x0, y0);

                if (x0 == x1 && y0 == y1) yield break;

                e2 = 2 * err;

                if (dy > dx)
                {
                    if (e2 > -dy)
                    {
                        err = err - dy;
                        x0 = x0 + sx;
                    }
                    else if (e2 < dx)
                    {
                        err = err + dx;
                        y0 = y0 + sy;
                    }
                }
                else
                {
                    if (e2 < dx)
                    {
                        err = err + dx;
                        y0 = y0 + sy;
                    }
                    else if (e2 > -dy)
                    {
                        err = err - dy;
                        x0 = x0 + sx;
                    }
                }
            }
        }

        public static IEnumerable<Vector2i> Line(float x1, float y1, float x2, float y2)
        {
            int x = Mathf.FloorToInt(x1);
            int y = Mathf.FloorToInt(y1);
            double slope = (x2 - x1) / (y2 - y1);
            if (y2 >= y1)
            {
                while (y < y2)
                {
                    int r = (int)Math.Floor(slope * (y - y1) + x1);
                    do
                    {
                        yield return new Vector2i(x, y);
                        ++x;
                    } while (x < r);
                    yield return new Vector2i(x, y);
                    ++y;
                }
            }
            else
            {
                while (y > y2)
                {
                    int r = (int)Math.Floor(slope * (y - y1) + x1);
                    do
                    {
                        yield return new Vector2i(x, y);
                        ++x;
                    } while (x < r);
                    yield return new Vector2i(x, y);
                    --y;
                }
            }
        }

        public static IEnumerable<Vector2i> Polygon(Macro.Cell cell)
        {
            var edges = new List<Vector2i>();
            foreach (var edge in cell.Edges)
                edges.AddRange(DDA(edge.Vertex1.Position, edge.Vertex2.Position, false));

            var bounds = (Bounds2i)cell.Bounds;

            Debug.Log(bounds);

            edges = edges.Where(e => bounds.Contains(e)).Distinct().ToList();
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
        }

        /// <summary>
        /// Bounds scan algorithm for convex polygon. Not very fast but accurate. As fast as bounding box is tight
        /// </summary>
        /// <returns></returns>
        public static Vector2i[] Polygon2(Predicate<Vector2> contains, Box2 bounds)
        {
            var result = new List<Vector2i>();
            var blockBounds = (Bounds2i) bounds;

            //Scan bound from top to bottom
            for (int z = blockBounds.Min.Z; z <= blockBounds.Max.Z; z++)
            {
                int? leftContainingPos = null;
                int? rightContainingPos = null;

                //Find left and right blocks in cell
                for (int x = blockBounds.Min.X; x <= blockBounds.Max.X; x++)
                {
                    if (contains(new Vector2(x + 0.5f, z + 0.5f)))
                    {
                        leftContainingPos = x;
                        break;
                    }
                }

                if (leftContainingPos != null)
                {
                    for (int x = blockBounds.Max.X; x >= blockBounds.Min.X; x--)
                    {
                        if (contains(new Vector2(x + 0.5f, z + 0.5f)))
                        {
                            rightContainingPos = x;
                            break;
                        }
                    }
                }

                if (leftContainingPos.HasValue && rightContainingPos.HasValue)
                {
                    //Add block pos from left to right
                    for (int x = leftContainingPos.Value; x <= rightContainingPos.Value; x++)
                    {
                        result.Add(new Vector2i(x, z));
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Do not accurately rasterize, need invesigation
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2i> Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
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
                var v4 = new Vector2(v1.X + (v2.Y - v1.Y) / (v3.Y - v1.Y) * (v3.X - v1.X), v2.Y);
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

        private static IEnumerable<Vector2i> FillBottomFlatTriangle(Vector2i v1, Vector2i v2, Vector2i v3)
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

        private static IEnumerable<Vector2i> FillTopFlatTriangle(Vector2i v1, Vector2i v2, Vector2i v3)
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
    }
}

