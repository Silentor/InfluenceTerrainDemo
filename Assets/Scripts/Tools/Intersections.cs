using System;
using System.Collections.Generic;
using System.Numerics;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Ray = TerrainDemo.Spatial.Ray;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

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

            float alphaNumerator = b.Y * c.X - b.X * c.Y;
            float alphaDenominator = a.Y * b.X - a.X * b.Y;
            float betaNumerator = a.X * c.Y - a.Y * c.X;
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
        public static (float u, float v, float w) Barycentric2DCoords(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
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

        public static (float u, float v, float w) Barycentric2DCoordsOptimized_00_01_11(Vector2 localPoint)
        {
            //Based on http://gamedev.stackexchange.com/a/63203
            //var v0 = b - a;       0, 1
            //var v1 = c - a;       1, 1    
            var v2 = localPoint;
            const float invDen = 1f / (0 * 1 - 1 * 1);
            var v = (v2.X - v2.Y) * invDen;
            var w = (-v2.X) * invDen;
            var u = 1.0f - v - w;

            return (u, v, w);
        }

        public static (float u, float v, float w) Barycentric2DCoordsOptimized_00_11_10(Vector2 localPoint)
        {
            //Based on http://gamedev.stackexchange.com/a/63203
            //var v0 = b - a;       1, 1
            //var v1 = c - a;       1, 0
            var v2 = localPoint;
            const float invDen = 1f / (1 * 0 - 1 * 1);
            var v = (-v2.Y) * invDen;
            var w = (v2.Y - v2.X) * invDen;
            var u = 1.0f - v - w;

            return (u, v, w);
        }

        public static (float u, float v, float w) Barycentric2DCoordsOptimized_00_01_10(Vector2 localPoint)
        {
            //Based on http://gamedev.stackexchange.com/a/63203
            //var v0 = b - a;           0, 1
            //var v1 = c - a;           1, 0
            var v2 = localPoint;
            const float invDen = 1f / (0 * 0 - 1 * 1);
            var v = (-v2.Y) * invDen;
            var w = (-v2.X) * invDen;
            var u = 1.0f - v - w;

            return (u, v, w);
        }

        public static (float u, float v, float w) Barycentric2DCoordsOptimized_01_11_10(Vector2 localPoint)
        {
            //Based on http://gamedev.stackexchange.com/a/63203
            //var v0 = b - a;           1, 0
            //var v1 = c - a;           1, -1    
            var v2 = localPoint - Vector2.UnitY;
            const float invDen = 1f / (1 * (-1) - 1 * 0);
            var v = (-v2.X - v2.Y) * invDen;
            var w = (v2.Y) * invDen;
            var u = 1.0f - v - w;

            return (u, v, w);
        }

        private static float GetPseudoDotProduct(Vector3 p1, Vector3 p2)
        {
            return p1.X * p2.Z - p2.X * p1.Z;
        }

        // intersect3D_RayTriangle(): find the 3D intersection of a ray with a triangle
        //    Input:  a ray R, and a triangle T
        //    Output: *I = intersection point (when it exists)
        //    Return: -1 = triangle is degenerate (a segment or point)
        //             0 =  disjoint (no intersect)
        //             1 =  intersect in unique point I1
        //             2 =  are in the same plane
        [Obsolete]
        public static int LineTriangleIntersection(in Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, /*out Vector3 inter*/ out float distance)    //todo needs testing
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
            if (n == Vector3.Zero)              // triangle is degenerate
                return -1;                      // do not deal with this case

            dir = ray.Direction;              // ray direction vector
            w0 = ray.Origin - v0;
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

            inter = ray.Origin + distance * dir;            // intersect point of ray and plane

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
        /// Fast GPU optimized code
        /// http://iquilezles.org/www/articles/intersectors/intersectors.htm
        /// </summary>
        /// <param name="ro"></param>
        /// <param name="rd"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float RayTriangleIntersection(in Ray ray, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var v1v0 = v1 - v0;
            var v2v0 = v2 - v0;
            var rov0 = ray.Origin - v0;

            var n = Vector3.Cross(v1v0, v2v0);
            var q = Vector3.Cross(rov0, ray.Direction);
            float d = 1.0f / Vector3.Dot(ray.Direction, n);
            float u = d * Vector3.Dot(-q, v2v0);             //u - barycentric coord
            float v = d * Vector3.Dot(q, v1v0);              //v - barycentric coord
            float t = d * Vector3.Dot(-n, rov0);             //t - distance to intersection point

            if (u < 0.0 || u > 1.0 || v < 0.0 || (u + v) > 1.0)
                t = -1.0f;

            //return (t, u, v);
            return t;
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
            float t1 = (vmin.X - origin.X) / direction.X;
            float t2 = (vmax.X - origin.X) / direction.X;
            float t3 = (vmin.Y - origin.Y) / direction.Y;
            float t4 = (vmax.Y - origin.Y) / direction.Y;
            float t5 = (vmin.Z - origin.Z) / direction.Z;
            float t6 = (vmax.Z - origin.Z) / direction.Z;

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

        public static float RayBlockIntersection(in Ray ray, in GridPos positon, in Blocks block)
        {
            var dirFracX = 1 / ray.Direction.X;
            var dirFracY = 1 / ray.Direction.Y;
            var dirFracZ = 1 / ray.Direction.Z;

            var (min, max) = BlockInfo.GetWorldBounds(positon);
            float t1 = (min.X - ray.Origin.X) * dirFracX;
            float t2 = (max.X - ray.Origin.X) * dirFracX;
            //float t3 = (block.Height.Base - ray.Origin.Y) * dirFracY;
            //float t4 = (block.Height.Nominal - ray.Origin.Y) * dirFracY;
            float t3 = (-1 - ray.Origin.Y) * dirFracY;      //DEBUG
            float t4 = (1 - ray.Origin.Y) * dirFracY;       //DEBUG
            float t5 = (min.Y - ray.Origin.Z) * dirFracZ;
            float t6 = (max.Y - ray.Origin.Z) * dirFracZ;

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

        /// <summary>
        /// Unity source https://gist.github.com/unitycoder/8d1c2905f2e9be693c78db7d9d03a102
        /// More info and C (GPU compatible) source https://gamedev.stackexchange.com/a/18459
        /// Some internal knowledge
        /// </summary>
        /// <returns></returns>
        public static (float distance, GridPos position)? RayBlockIntersection(in Ray ray, IEnumerable<(GridPos positon, Interval heights)> blocks)  //todo consider use IEnumerable parameters, its not neccessary to produce complete volumes array
        {
            var dirFracX = 1 / ray.Direction.X;
            var dirFracY = 1 / ray.Direction.Y;
            var dirFracZ = 1 / ray.Direction.Z;

            foreach (var block in blocks)
            {
                var (min, max) = BlockInfo.GetWorldBounds(block.positon);
                float t1 = (min.X - ray.Origin.X) * dirFracX;
                float t2 = (max.X - ray.Origin.X) * dirFracX;
                float t3 = (block.heights.Min - ray.Origin.Y) * dirFracY;
                float t4 = (block.heights.Max - ray.Origin.Y) * dirFracY;
                float t5 = (min.Y - ray.Origin.Z) * dirFracZ;
                float t6 = (max.Y - ray.Origin.Z) * dirFracZ;

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

        public static IEnumerable<(GridPos blockPosition, float distance, Side2d normal)> GridIntersections(Vector2 start, Vector2 finish, int blockLimit = 1000)
        {
            var xIncrement = Math.Sign(finish.X - start.X);
            var yIncrement = Math.Sign(finish.Y - start.Y);

            var startBlock = (GridPos)start;
            var finishBlock = (GridPos)finish;

            if (startBlock == finishBlock)
                yield break;

            //var k = (finish.Y - start.Y) / (finish.X - start.X);        //Угловой коэфициент прямой
            //Combined division severely impact accuracy for fractions like 1/3
            var k1 = finish.Y - start.Y;
            var k2 = finish.X - start.X;

            //Debug.Log($"Start block {startBlock}, finish {finishBlock}, x inc {xIncrement}, y inc {yIncrement}, k {k1 / k2}");

            var testedXPosition = xIncrement > 0 ? startBlock.X + 1 : startBlock.X;
            var testedYPosition = yIncrement > 0 ? startBlock.Z + 1 : startBlock.Z;

            //Fast pass - vertical line
            if (startBlock.X == finishBlock.X)
            {
                while (startBlock != finishBlock)
                {
                    var pointY = new Vector2(
                        ((testedYPosition) - start.Y) * k2 / k1 + start.X,
                        testedYPosition);

                    testedYPosition += yIncrement;
                    startBlock = new GridPos(startBlock.X, startBlock.Z + yIncrement);
                    yield return (startBlock, Vector2.Distance(pointY, start), yIncrement > 0 ? Side2d.Back : Side2d.Forward);

                    if (--blockLimit <= 0)
                    {
                        Debug.LogWarning($"Block limit on grid intersections from {start} to {finish}");
                        yield break;
                    }
                }
            }
            //Fast pass - horizontal line
            else if (startBlock.Z == finishBlock.Z)
            {
                while (startBlock != finishBlock)
                {
                    var pointX = new Vector2(testedXPosition,
                        (testedXPosition - start.X) * k1 / k2 + start.Y);

                    testedXPosition += xIncrement;
                    startBlock = new GridPos(startBlock.X + xIncrement, startBlock.Z);
                    yield return (startBlock, Vector2.Distance(pointX, start), xIncrement > 0 ? Side2d.Left : Side2d.Right);

                    if (--blockLimit <= 0)
                    {
                        Debug.LogWarning($"Block limit on grid intersections from {start} to {finish}");
                        yield break;
                    }

                }
            }
            //Default pass - diagonal line
            else
            {
                var testedPoint = start;
                var pointX = new Vector2(testedXPosition,
                    (testedXPosition - start.X) * k1 / k2 + start.Y);
                var pointY = new Vector2((testedYPosition - start.Y) * k2 / k1 + start.X,
                    testedYPosition);
                while (startBlock != finishBlock)
                {
                    var distanceToXPoint = Vector2.DistanceSquared(pointX, testedPoint);
                    var distanceToYPoint = Vector2.DistanceSquared(pointY, testedPoint);
                    //Debug.Log($"{Time.frameCount}: comparing distance ({pointX}, {testedPoint}) = {distanceToXPoint} and ({pointY}, {testedPoint}) = {distanceToYPoint}. Diff {Math.Abs(distanceToXPoint-distanceToYPoint)}");

                    //Compare two intersection points
                    if (MathHelper.ApproximatelyEquivalent(distanceToXPoint, distanceToYPoint, 0.00000001))  //Tolerance for ~1 values
                    {
                        //Check for bad strict diagonal case, select step closer to final block
                        //Debug.Log($"Points on equal distance");
                        if (GridPos.DistanceSquared(new GridPos(startBlock.X + xIncrement, startBlock.Z), finishBlock) < GridPos.DistanceSquared(new GridPos(startBlock.X, startBlock.Z + yIncrement), finishBlock))
                        {
                            //X-increment block is closer
                            testedXPosition += xIncrement;
                            testedPoint = pointX;
                            startBlock = new GridPos(startBlock.X + xIncrement, startBlock.Z);
                            yield return (startBlock, Vector2.Distance(pointX, start), xIncrement > 0 ? Side2d.Left : Side2d.Right);
                            pointX = new Vector2(testedXPosition,
                                (testedXPosition - start.X) * k1 / k2 + start.Y);
                        }
                        else
                        {
                            //X-increment block is closer
                            testedYPosition += yIncrement;
                            testedPoint = pointY;
                            startBlock = new GridPos(startBlock.X, startBlock.Z + yIncrement);
                            yield return (startBlock, Vector2.Distance(pointY, start), yIncrement > 0 ? Side2d.Back : Side2d.Forward);
                            pointY = new Vector2(
                                (testedYPosition - start.Y) * k2 / k1 + start.X,
                                testedYPosition);
                        }
                    }
                    else if (distanceToXPoint < distanceToYPoint)
                    {
                        //Debug.Log($"First point {pointX} is nearer, step from {startBlock} to {new Vector2i(startBlock.X + xIncrement, startBlock.Z)} at {pointX}");

                        testedXPosition += xIncrement;
                        testedPoint = pointX;
                        startBlock = new GridPos(startBlock.X + xIncrement, startBlock.Z);
                        yield return (startBlock, Vector2.Distance(pointX, start), xIncrement > 0 ? Side2d.Left : Side2d.Right);
                        pointX = new Vector2(testedXPosition,
                            (testedXPosition - start.X) * k1 / k2 + start.Y);
                    }
                    else
                    {
                        //Debug.Log($"Second point {pointY} is nearer, step from {startBlock} to {new Vector2i(startBlock.X, startBlock.Z + yIncrement)} at {pointY}");
                        testedYPosition += yIncrement;
                        testedPoint = pointY;
                        startBlock = new GridPos(startBlock.X, startBlock.Z + yIncrement);
                        yield return (startBlock, Vector2.Distance(pointY, start), yIncrement > 0 ? Side2d.Back : Side2d.Forward);
                        pointY = new Vector2((testedYPosition - start.Y) * k2 / k1 + start.X,
                            testedYPosition);
                    }

                    if (--blockLimit <= 0)
                    {
                        Debug.LogWarning($"Block limit on grid intersections from {start} to {finish}");
                        yield break;
                    }
                }
            }
        }

        public static IEnumerable<(GridPos blockPosition, float distance, Side2d normal)> GridIntersections(
	        GridPos start, GridPos finish, int blockLimit = 1000)
        {
            return GridIntersections(BlockInfo.GetWorldCenter(start), BlockInfo.GetWorldCenter(finish), blockLimit);
        }
    }
}
