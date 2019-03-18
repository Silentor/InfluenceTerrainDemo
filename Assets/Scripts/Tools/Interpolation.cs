using System;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Tools
{
    //Universal sigmoid function https://math.stackexchange.com/a/1754900

    public static class Interpolation
    {
        public static double SmoothStep(double x)
        {
            return 3 * x * x - 2 * x * x * x;
        }

        public static double SmootherStep(double x)
        {
            return x * x * x * (6 * x * x - 15 * x + 10);
        }

        public static double SmoothestStep(double x)
        {
            return x * x * x * x * (-20 * x * x * x + 70 * x * x - 84 * x + 35);
        }

        public static float SmoothStep(float x)
        {
            return 3 * x * x - 2 * x * x * x;
        }

        public static float SmootherStep(float x)
        {
            return x * x * x * (6 * x * x - 15 * x + 10);
        }

        public static float SmoothestStep(float x)
        {
            return x * x * x * x * (-20 * x * x * x + 70 * x * x - 84 * x + 35);
        }

        // Slightly slower than linear interpolation but much smoother
        public static double InOutCosine(double x)
        {
            var ft = x * Math.PI;
            var f = (1 - Math.Cos(ft)) * 0.5;
            return f;
        }

        public static double OutQuad(double x)
        {
            return x * (2 - x);
        }

        public static double InCubic(double x)
        {
            return x * x * x;
        }

        public static double InQuintic(double x)
        {
            return x * x * x * x;
        }

        public static double OutCubic(double x)
        {
            return (x - 1) * (x - 1) * (x - 1) + 1;
        }

        public static double InQuad(double x)
        {
            return x * x;
        }

        public static double InExpo(double x)
        {
            return Math.Pow(2, 10 * (x - 1));
        }

        // Much slower than cosine and linear interpolation, but very smooth
        // v1 = a, v2 = b
        // v0 = point before a, v3 = point after b
        public static Vector4 InterpolateCubic(Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3, float x)
        {
            var p = (v3 - v2) - (v0 - v1);
            var q = (v0 - v1) - p;
            var r = v2 - v0;
            var s = v1;
            return p * x * x * x + q * x * x + r * x + s;
        }

        /// <summary>
        /// http://iquilezles.org/www/articles/functions/functions.htm
        /// </summary>
        /// <param name="x"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static double Gain(double x, double k)
        {
            var a = 0.5 * Math.Pow(2.0 * ((x < 0.5) ? x : 1.0 - x), k);
            return (x < 0.5) ? a : 1.0 - a;
        }

        /// <summary>
        /// http://iquilezles.org/www/articles/functions/functions.htm
        /// </summary>
        /// <param name="x"></param>
        /// <param name="k"></param>
        public static double Parabola(double x, double k)
        {
            return Math.Pow(4.0 * x * (1.0 - x), k);
        }
    }
}
