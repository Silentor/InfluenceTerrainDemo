using System;
using JetBrains.Annotations;
using OpenTK;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Multiheightmap vertex height (immutable)
    /// </summary>
    public struct Heights : IEquatable<Heights>
    {
        public const int LayersCount = 3;

        public readonly float BaseHeight;
        public readonly float UndergroundHeight;
        public readonly float Layer1Height;
        public readonly bool AdditionalLayer;

        public float Nominal => Math.Max(BaseHeight, Layer1Height);

        public bool IsLayer1Present => Layer1Height > UndergroundHeight;

        public bool IsUndergroundLayerPresent => UndergroundHeight > BaseHeight;

        public Heights(float baseHeight, float undergroundHeight, float layer1Height, bool additionalLayer = false)
        {
            if (undergroundHeight < baseHeight)
                undergroundHeight = baseHeight;

            if (layer1Height < undergroundHeight)
                layer1Height = undergroundHeight;

            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            BaseHeight = baseHeight;
            UndergroundHeight = undergroundHeight;
            Layer1Height = layer1Height;
            AdditionalLayer = additionalLayer;
        }

        [Pure]
        public Heights Dig(float deep)
        {
            var newHeight = Layer1Height - deep;
            var newHeightForOre = Layer1Height - deep / 2;
            return new Heights(BaseHeight, Math.Min(UndergroundHeight, newHeightForOre), newHeight);
        }

        [Pure]
        public Heights Build(float pileHeight)
        {
            return new Heights(BaseHeight, UndergroundHeight, Layer1Height + pileHeight);
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

        public static explicit operator Vector3d(Heights h)
        {
            return new Vector3d(h.BaseHeight, h.UndergroundHeight, h.Layer1Height);
        }

        public static explicit operator Heights(Vector3d v)
        {
            return new Heights((float)v.X, (float)v.Y, (float)v.Z);
        }

        public override string ToString()
        {
            var noMainLayer = IsLayer1Present ? "" : "*";
            var noUnderLayer = IsUndergroundLayerPresent ? "" : "*";
            return $"({Layer1Height:N1}{noMainLayer}, {UndergroundHeight:N1}{noUnderLayer}, {BaseHeight:N1})";
        }

        public bool Equals(Heights other)
        {
            return BaseHeight.Equals(other.BaseHeight) && UndergroundHeight.Equals(other.UndergroundHeight) && Layer1Height.Equals(other.Layer1Height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Heights && Equals((Heights) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = BaseHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ UndergroundHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ Layer1Height.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Heights left, Heights right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Heights left, Heights right)
        {
            return !left.Equals(right);
        }
    }
}
