using System;
using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tools
{
    public class HalfPlane
    {
        public HalfPlane(Vector2 linePoint1, Vector2 linePoint2, Vector2 halflanePoint)
        {
            if(linePoint1 == linePoint2) throw new ArgumentException("Line is undefined");

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
        public static bool ContainsInConvex(Vector2 point, params HalfPlane[] bounds)
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
