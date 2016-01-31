using System;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class Intersections
    {
        public static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            http: //www.stefanbader.ch/faster-line-segment-intersection-for-unity3dc/
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
        public static int LineTriangleIntersection(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 inter)    //todo needs testing
        {
            //Translated from http://geomalgorithms.com/a06-_intersect-2.html#intersect3D_RayTriangle()

            Vector3 u, v, n;              // triangle vectors
            Vector3 dir, w0, w;           // ray vectors
            float r, a, b;              // params to calc ray-plane intersect
            inter = Vector3.zero;

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
            r = a / b;
            if (r < 0.0)                    // ray goes away from triangle
                return 0;                   // => no intersect
                                            // for a segment, also test if (r > 1.0) => no intersect

            inter = ray.origin + r * dir;            // intersect point of ray and plane

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
    }
}
