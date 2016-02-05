using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class Rasterization
    {
        public static IEnumerable<Vector2i> DDA(Vector2i p1, Vector2i p2, bool conservative)
        {
            return DDA(new Vector2(p1.X + 0.5f, p1.Z + 0.5f), new Vector2(p2.X + 0.5f, p2.Z + 0.5f), conservative);
        }

        public static IEnumerable<Vector2i> DDA(Vector2 p1, Vector2 p2, bool conservative)
        {
            //Based on http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf
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

            //Additional blocks for conservative rasterization
            var addX = new Vector2i(Mathf.Sign(dx), 0);
            var addZ = new Vector2i(0, Mathf.Sign(dy));

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

