using System;
using UnityEngine.Assertions;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Multiheightmap vertex height
    /// </summary>
    public struct MicroHeight
    {
        public readonly float BaseHeight;
        public readonly float Layer1Height;
        public readonly bool AdditionalLayer;
        [Obsolete]
        public readonly int ZoneId;

        public float Height => Math.Max(BaseHeight, Layer1Height);

        public bool IsLayer1Present => Layer1Height > BaseHeight;

        public MicroHeight(float baseHeight, float layer1Height, bool additionalLayer = false)
        {
            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            BaseHeight = baseHeight;
            Layer1Height = layer1Height;
            AdditionalLayer = additionalLayer;
            ZoneId = -1;
        }
    }
}
