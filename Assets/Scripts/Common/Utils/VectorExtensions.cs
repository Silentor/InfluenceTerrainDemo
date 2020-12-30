﻿using System;
using JetBrains.Annotations;
using TerrainDemo.Spatial;
using UnityEngine;
using OpenTK = OpenToolkit.Mathematics;

namespace TerrainDemo.Tools
{
    public static class VectorExtensions
    {
        /// <summary>
        /// I prefer custom Vector2 - Vector3 conversion logic
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 ConvertTo2D(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        /// <summary>
        /// I prefer custom Vector2 - Vector3 conversion logic
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 ConvertTo3D(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        /// <summary>
        /// I prefer custom Vector2 - Vector3 conversion logic
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static OpenTK.Vector2 ConvertTo2D(this OpenTK.Vector3 v)
        {
            return new OpenTK.Vector2(v.X, v.Z);
        }

        public static Vector3 ConvertTo3D(this Vector2i v)
        {
            return ((Vector2)v).ConvertTo3D();
        }

        public static OpenTK.Vector3 ToVector3(this OpenTK.Vector2 v, float yValue = 0)
        {
	        return new Vector3(v.X, yValue, v.Y);
        }

        public static UnityEngine.Vector3 ToUnityVector3(this OpenTK.Vector2 v, float yValue = 0)
        {
	        return new Vector3(v.X, yValue, v.Y);
        }

        public static string ToString(this OpenTK.Vector2 v, int precision)
        {
	        switch (precision)
	        {
		        case 0:
			        return $"({v.X:F0}, {v.Y:F0})";
		        case 1:
			        return $"({v.X:F1}, {v.Y:F1})";
		        case 2:
			        return $"({v.X:F2}, {v.Y:F2})";
		        case 3:
			        return $"({v.X:F3}, {v.Y:F3})";
		        default:
			        return v.ToString();
	        }
        }

        [Pure]
        public static string ToString(this OpenTK.Vector3 v, int precision)
        {
	        switch (precision)
	        {
		        case 0:  return $"({v.X:N0}, {v.Y:N0}, {v.Z:N0})";
		        case 1:  return $"({v.X:N1}, {v.Y:N1}, {v.Z:N1})";
		        case 2:  return $"({v.X:N2}, {v.Y:N2}, {v.Z:N2})";
		        case 3:  return $"({v.X:N3}, {v.Y:N3}, {v.Z:N3})";
		        default: return v.ToString();
	        }
        }

        /// <summary>
        /// Compare points in clockwise order (relatively center)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="center"></param>
        /// <returns>-1 = a -> b clockwise; 1 = b -> a clockwise; 0 = point a, b are same</returns>
        public static int ClockWiseComparer(Vector2 a, Vector2 b, Vector2 center)
        {
            //Based on http://stackoverflow.com/questions/6989100/sort-points-in-clockwise-order

            //Some buggy optimization, consider perfomance usefulness
            //if (a.X - center.X >= 0 && b.X - center.X < 0)
            //    return true;
            //if (a.X - center.X < 0 && b.X - center.X >= 0)
            //    return false;
            //if (Math.Abs(a.X - center.X) < float.Epsilon && Math.Abs(b.X - center.X) < float.Epsilon)
            //{
            //    if (a.Y - center.Y >= 0 || b.Y - center.Y >= 0)
            //        return a.Y > b.Y;
            //    return b.Y > a.Y;
            //}

            if (a == center || b == center)
                throw new InvalidOperationException("Some input points match the center point");

            if (a == b)
                return 0;

            // compute the pseudoscalar product of vectors (center -> a) x (center -> b)
            var ca = center - a;
            var cb = center - b;
            var det = ca.x * cb.y - cb.x * ca.y;
            if (det > 0)
                return 1;
            if (det < 0)
                return -1;

            // points a and b are on the same line from the center
            // check which point is closer to the center
            return ca.sqrMagnitude.CompareTo(cb.sqrMagnitude);
        }

        /// <summary>
        /// Unity Vector == operator does approx comparison also, this is a coarse version
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool ApproxEqual(Vector2 v1, Vector2 v2)
        {
            return Math.Abs(v1.x - v2.x) < 0.001 && Math.Abs(v1.y - v2.y) < 0.001;
        }
    }
}