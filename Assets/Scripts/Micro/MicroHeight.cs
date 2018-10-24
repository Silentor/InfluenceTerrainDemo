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
        public readonly int ZoneId;

        public float Height => Math.Max(BaseHeight, Layer1Height);

        public MicroHeight(float baseHeight, float layer1Height, int zoneId, bool additionalLayer = false)
        {
            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            BaseHeight = baseHeight;
            Layer1Height = layer1Height;
            ZoneId = zoneId;
            AdditionalLayer = additionalLayer;
        }

        /// <summary>
        /// Get layer id and Zone id
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        /// <param name="vertex4"></param>
        /// <returns></returns>
        public static ValueTuple<int, BlockLayer> GetMostInfluencedZone(MicroHeight vertex1, MicroHeight vertex2, MicroHeight vertex3,
            MicroHeight vertex4)
        {
            var baseHeight = float.MinValue;
            var layer1Height = float.MinValue;
            var influencedBaseZone = Macro.Zone.InvalidId;
            var influencedLayer1Zone = Macro.Zone.InvalidId;

            if (vertex1.Layer1Height > vertex1.BaseHeight)
            {
                if (vertex1.Layer1Height > layer1Height)
                {
                    layer1Height = vertex1.Layer1Height;
                    influencedLayer1Zone = vertex1.ZoneId;
                }
            }
            else
            {
                if (vertex1.BaseHeight > baseHeight)
                {
                    baseHeight = vertex1.BaseHeight;
                    influencedBaseZone = vertex1.ZoneId;
                }
            }

            if (vertex2.Layer1Height > vertex2.BaseHeight)
            {
                if (vertex2.Layer1Height > layer1Height)
                {
                    layer1Height = vertex2.Layer1Height;
                    influencedLayer1Zone = vertex2.ZoneId;
                }
            }
            else
            {
                if (vertex2.BaseHeight > baseHeight)
                {
                    baseHeight = vertex2.BaseHeight;
                    influencedBaseZone = vertex2.ZoneId;
                }
            }

            if (vertex3.Layer1Height > vertex3.BaseHeight)
            {
                if (vertex3.Layer1Height > layer1Height)
                {
                    layer1Height = vertex3.Layer1Height;
                    influencedLayer1Zone = vertex3.ZoneId;
                }
            }
            else
            {
                if (vertex3.BaseHeight > baseHeight)
                {
                    baseHeight = vertex3.BaseHeight;
                    influencedBaseZone = vertex3.ZoneId;
                }
            }

            if (vertex4.Layer1Height > vertex4.BaseHeight)
            {
                if (vertex4.Layer1Height > layer1Height)
                {
                    layer1Height = vertex4.Layer1Height;
                    influencedLayer1Zone = vertex4.ZoneId;
                }
            }
            else
            {
                if (vertex4.BaseHeight > baseHeight)
                {
                    baseHeight = vertex4.BaseHeight;
                    influencedBaseZone = vertex4.ZoneId;
                }
            }

            Assert.IsTrue(influencedLayer1Zone != Macro.Zone.InvalidId || influencedBaseZone != Macro.Zone.InvalidId);

            if (influencedLayer1Zone != Macro.Zone.InvalidId)
                return new ValueTuple<int, BlockLayer>(influencedLayer1Zone, BlockLayer.Main);
            else
                return new ValueTuple<int, BlockLayer>(influencedBaseZone, BlockLayer.Base);
        }
    }
}
