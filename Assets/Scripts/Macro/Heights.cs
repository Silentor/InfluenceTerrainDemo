using System;
using OpenTK;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Multiheightmap vertex height
    /// </summary>
    public struct Heights
    {
        public const int LayersCount = 2;

        public readonly float BaseHeight;
        public readonly float Layer1Height;
        public readonly bool AdditionalLayer;

        public float Nominal => Math.Max(BaseHeight, Layer1Height);

        public bool IsLayer1Present => Layer1Height > BaseHeight;

        public Heights(float baseHeight, float layer1Height, bool additionalLayer = false)
        {
            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            BaseHeight = baseHeight;
            Layer1Height = layer1Height;
            AdditionalLayer = additionalLayer;
        }

        /*
        public Heights(Heights copyFrom)
        {
            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            BaseHeight = baseHeight;
            Layer1Height = layer1Height;
            AdditionalLayer = additionalLayer;
        }
        */

        public static explicit operator Vector2d(Heights h)
        {
            return new Vector2d(h.BaseHeight, h.Layer1Height);
        }

        public static explicit operator Heights(Vector2d v)
        {
            return new Heights((float)v.X, (float)v.Y);
        }
    }
}
