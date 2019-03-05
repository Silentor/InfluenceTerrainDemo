using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Spatial
{
    public class HalfPlane
    {
        public readonly Vector2 Point1;
        public readonly Vector2 Point2;

        public HalfPlane(Vector2 linePoint1, Vector2 linePoint2, Vector2 halflanePoint)
        {
            if(linePoint1 == linePoint2) throw new ArgumentException("Line is undefined");

            Point1 = linePoint1;
            Point2 = linePoint2;

            //Create common line equation
            var a = linePoint1.Y - linePoint2.Y;
            var b = linePoint2.X - linePoint1.X;
            var c = linePoint1.X * linePoint2.Y - linePoint2.X * linePoint1.Y;

            var commonLine = a * halflanePoint.X + b * halflanePoint.Y + c;
            if(Mathf.Approximately(commonLine, 0))
                throw new ArgumentException("Plane half is not defined");

            if (commonLine < 0)
            {
                a = -a;
                b = -b;
                c = -c;
            }

            _a = a;
            _b = b;
            _c = c;
        }

        public bool Contains(Vector2 point)
        {
            return _a * point.X + _b * point.Y + _c >= 0;
        }

        /// <summary>
        /// Is point in convex polygon bounded by given halfplanes. Convexity is not validated!
        /// </summary>
        /// <param name="point"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static bool ContainsInConvex(Vector2 point, IEnumerable<HalfPlane> bounds)       //todo make "CheckContains" internal class to store halfplanes?
        {
            foreach (var bound in bounds)
            {
                if (!bound.Contains(point))
                    return false;
            }

            return true;
        }

        private readonly float _a;
        private readonly float _b;
        private readonly float _c;
    }

    
}
