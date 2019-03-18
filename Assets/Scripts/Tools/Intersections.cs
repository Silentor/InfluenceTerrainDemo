using System;
using System.Collections.Generic;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class Intersections
    {
        public static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            //Based on http://www.stefanbader.ch/faster-line-segment-intersection-for-unity3dc/
            Vector2 a = p2 - p1;
            Vector2 b = p3 - p4;
            Vector2 c = p1 - p3;

            float alphaNumerator = b.y*c.x - b.x*c.y;
            float alphaDenominator = a.y*b.x - a.x*b.y;
            float betaNumerator = a.x*c.y - a.y*c.x;
            float betaDenominator = alphaDenominator; /*2013/07/05, fix by Deniz*/

            bool doIntersect = true;

            if (alphaDenominator == 0 || betaDenominator == 0)
            {
                doIntersect = false;
            }
            else
            {

                if (alphaDenominator > 0)
                {
                    if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                    {
                        doIntersect = false;
                    }
                }
                else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
                {
                    doIntersect = false;
                }

                if (doIntersect && betaDenominator > 0)
                {
                    if (betaNumerator < 0 || betaNumerator > betaDenominator)
                    {
                        doIntersect = false;
                    }
                }
                else if (betaNumerator > 0 || betaNumerator < betaDenominator)
                {
                    doIntersect = false;
                }
            }

            return doIntersect;
        }


        //public static Vector3? PointTriangleIntersection(Vector3 tr1, Vector3 tr2, Vector3 tr3, Vector3 point)
        //{
        //    //Barycentric coordinates
        //    //http://answers.unity3d.com/questions/383804/calculate-uv-coordinates-of-3d-point-on-plane-of-m.html

        //    //Calculate vectors
        //    var f1 = new Vector2(tr1.x - point.x, tr1.z - point.z);
        //    var f2 = new Vector2(tr2.x - point.x, tr2.z - point.z);
        //    var f3 = new Vector2(tr3.x - point.x, tr3.z - point.z);

        //    //Calculate ratios
        //    var totalArea = GetPseudoDotProduct(f1 - f2, f1 - f3);
        //    var ratio1 = GetPseudoDotProduct(f2, f3)/totalArea;
        //    var ratio2 = GetPseudoDotProduct(f1, f3)/totalArea;
        //    var ratio3 = GetPseudoDotProduct(f1, f2)/totalArea;

        //    //Debug.Assert(Math.Abs(ratio1 + ratio2 + ratio3 - 1) < 0.1f);

        //    if (ratio1 < 0 || ratio2 < 0 || ratio3 < 0)
        //        return null;

        //    var height = tr1.y*ratio1 + tr2.y*ratio2 + tr3.y*ratio3;
        //    return new Vector3(point.x, height, point.z);
        //}

        /// <summary>
        /// Calculate 2D barycentric coord of point in triangle
        /// </summary>
        /// <param name="point"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public static (float u, float v, float w) Barycentric2DCoords(OpenTK.Vector2 point, OpenTK.Vector2 a, OpenTK.Vector2 b, OpenTK.Vector2 c)
        {
            //Based on http://gamedev.stackexchange.com/a/63203
            var v0 = b - a;
            var v1 = c - a;
            var v2 = point - a;
            var invDen = 1 / (v0.X * v1.Y - v1.X * v0.Y);
            var v = (v2.X * v1.Y - v1.X * v2.Y) * invDen;
            var w = (v0.X * v2.Y - v2.X * v0.Y) * invDen;
            var u = 1.0f - v - w;

            return (u, v, w);
        }

        private static float GetPseudoDotProduct(Vector3 p1, Vector3 p2)
        {
            return p1.x*p2.z - p2.x*p1.z;
        }

        // intersect3D_RayTriangle(): find the 3D intersection of a ray with a triangle
        //    Input:  a ray R, and a triangle T
        //    Output: *I = intersection point (when it exists)
        //    Return: -1 = triangle is degenerate (a segment or point)
        //             0 =  disjoint (no intersect)
        //             1 =  intersect in unique point I1
        //             2 =  are in the same plane
        public static int LineTriangleIntersection(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, /*out Vector3 inter*/ out float distance)    //todo needs testing
        {
            //Translated from http://geomalgorithms.com/a06-_intersect-2.html#intersect3D_RayTriangle()

            Vector3 u, v, n;              // triangle vectors
            Vector3 dir, w0, w;           // ray vectors
            float a, b;              // params to calc ray-plane intersect
            Vector3 inter;
            distance = -1;

            // get triangle edge vectors and plane normal
            u = v1 - v0;
            v = v2 - v0;
            n = Vector3.Cross(u, v);              // cross product
            if (n == Vector3.zero)              // triangle is degenerate
                return -1;                      // do not deal with this case

            dir = ray.direction;              // ray direction vector
            w0 = ray.origin - v0;
            a = -Vector3.Dot(n, w0);
            b = Vector3.Dot(n, dir);
            if (Math.Abs(b) < 0.0000001f)
            {     // ray is  parallel to triangle plane
                if (Mathf.Approximately(a, 0))                 // ray lies in triangle plane
                    return 2;
                else return 0;              // ray disjoint from plane
            }

            // get intersect point of ray with triangle plane
            distance = a / b;
            if (distance < 0.0)                    // ray goes away from triangle
                return 0;                   // => no intersect
                                            // for a segment, also test if (r > 1.0) => no intersect

            inter = ray.origin + distance * dir;            // intersect point of ray and plane

            // is I inside T?
            float uu, uv, vv, wu, wv, D;
            uu = Vector3.Dot(u, u);
            uv = Vector3.Dot(u, v);
            vv = Vector3.Dot(v, v);
            w = inter - v0;
            wu = Vector3.Dot(w, u);
            wv = Vector3.Dot(w, v);
            D = uv * uv - uu * vv;

            // get and test parametric coords
            float s, t;
            s = (uv * wv - vv * wu) / D;
            if (s < 0.0 || s > 1.0)         // I is outside T
                return 0;
            t = (uv * wu - uu * wv) / D;
            if (t < 0.0 || (s + t) > 1.0)  // I is outside T
                return 0;

            return 1;                       // I is in T
        }

        /// <summary>
        /// Unity source https://gist.github.com/unitycoder/8d1c2905f2e9be693c78db7d9d03a102
        /// More info and C (GPU compatible) source https://gamedev.stackexchange.com/a/18459
        /// </summary>
        /// <param name="origin">ray</param>
        /// <param name="direction">ray</param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        /// <returns></returns>
        public static float RayAABBIntersection(Vector3 origin, Vector3 direction, Vector3 vmin, Vector3 vmax)
        {
            float t1 = (vmin.x - origin.x) / direction.x;
            float t2 = (vmax.x - origin.x) / direction.x;
            float t3 = (vmin.y - origin.y) / direction.y;
            float t4 = (vmax.y - origin.y) / direction.y;
            float t5 = (vmin.z - origin.z) / direction.z;
            float t6 = (vmax.z - origin.z) / direction.z;

            float aMin = t1 < t2 ? t1 : t2;
            float bMin = t3 < t4 ? t3 : t4;
            float cMin = t5 < t6 ? t5 : t6;

            float aMax = t1 > t2 ? t1 : t2;
            float bMax = t3 > t4 ? t3 : t4;
            float cMax = t5 > t6 ? t5 : t6;

            float fMax = aMin > bMin ? aMin : bMin;
            float fMin = aMax < bMax ? aMax : bMax;

            float t7 = fMax > cMin ? fMax : cMin;
            float t8 = fMin < cMax ? fMin : cMax;

            float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;

            return t9;
        }

        /// <summary>
        /// Unity source https://gist.github.com/unitycoder/8d1c2905f2e9be693c78db7d9d03a102
        /// More info and C (GPU compatible) source https://gamedev.stackexchange.com/a/18459
        /// Some internal knowledge
        /// </summary>
        /// <returns></returns>
        public static (float distance, Vector2i position)? RayBlockIntersection(Ray ray, IEnumerable<(Vector2i positon, Interval heights)> blocks)  //todo consider use IEnumerable parameters, its not neccessary to produce complete volumes array
        {
            var dirFracX = 1 / ray.direction.x;
            var dirFracY = 1 / ray.direction.y;
            var dirFracZ = 1 / ray.direction.z;

            foreach (var block in blocks)
            {
                var (min, max) = BlockInfo.GetWorldBounds(block.positon);
                float t1 = (min.X - ray.origin.x) * dirFracX;
                float t2 = (max.X - ray.origin.x) * dirFracX;
                float t3 = (block.heights.Min - ray.origin.y) * dirFracY;
                float t4 = (block.heights.Max - ray.origin.y) * dirFracY;
                float t5 = (min.Y - ray.origin.z) * dirFracZ;
                float t6 = (max.Y - ray.origin.z) * dirFracZ;

                float aMin = t1 < t2 ? t1 : t2;
                float bMin = t3 < t4 ? t3 : t4;
                float cMin = t5 < t6 ? t5 : t6;

                float aMax = t1 > t2 ? t1 : t2;
                float bMax = t3 > t4 ? t3 : t4;
                float cMax = t5 > t6 ? t5 : t6;

                float fMax = aMin > bMin ? aMin : bMin;
                float fMin = aMax < bMax ? aMax : bMax;

                float t7 = fMax > cMin ? fMax : cMin;
                float t8 = fMin < cMax ? fMin : cMax;

                float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;

                if (t9 > 0)
                    return (t9, block.positon);
            }

            return null;
        }

        public static float RayBlockIntersection(Ray ray, in Vector2i positon, in Blocks block)
        {
            var dirFracX = 1 / ray.direction.x;
            var dirFracY = 1 / ray.direction.y;
            var dirFracZ = 1 / ray.direction.z;


            var (min, max) = BlockInfo.GetWorldBounds(positon);
            float t1 = (min.X - ray.origin.x) * dirFracX;
            float t2 = (max.X - ray.origin.x) * dirFracX;
            float t3 = (block.Height.Base - ray.origin.y) * dirFracY;
            float t4 = (block.Height.Nominal - ray.origin.y) * dirFracY;
            float t5 = (min.Y - ray.origin.z) * dirFracZ;
            float t6 = (max.Y - ray.origin.z) * dirFracZ;

            float aMin = t1 < t2 ? t1 : t2;
            float bMin = t3 < t4 ? t3 : t4;
            float cMin = t5 < t6 ? t5 : t6;

            float aMax = t1 > t2 ? t1 : t2;
            float bMax = t3 > t4 ? t3 : t4;
            float cMax = t5 > t6 ? t5 : t6;

            float fMax = aMin > bMin ? aMin : bMin;
            float fMin = aMax < bMax ? aMax : bMax;

            float t7 = fMax > cMin ? fMax : cMin;
            float t8 = fMin < cMax ? fMin : cMax;

            float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;

            if (t9 > 0)
                return t9;

            return -1;
        }
    }
}
