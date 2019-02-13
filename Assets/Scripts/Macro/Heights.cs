using System;
using JetBrains.Annotations;
using OpenTK;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Multiheightmap vertex height (immutable)
    /// </summary>
    public readonly struct Heights : IEquatable<Heights>
    {
        public const int LayersCount = 3;

        public readonly float BaseHeight;
        public readonly float UndergroundHeight;
        public readonly float Layer1Height;
        public readonly bool IsCreated;

        public float Nominal => Math.Max(Math.Max(BaseHeight, Layer1Height), UndergroundHeight);

        public bool IsLayer1Present => Layer1Height > UndergroundHeight;

        public bool IsUndergroundLayerPresent => UndergroundHeight > BaseHeight;

        public Heights(float layer1Height, float undergroundHeight, float baseHeight)
        {
            if (undergroundHeight < baseHeight)
                undergroundHeight = baseHeight;

            if (layer1Height < baseHeight)
                layer1Height = baseHeight;

            if (layer1Height < undergroundHeight)
                layer1Height = undergroundHeight;

            BaseHeight = baseHeight;
            UndergroundHeight = undergroundHeight;
            Layer1Height = layer1Height;
            IsCreated = true;
        }

        public Heights(Heights copyFrom) : this(copyFrom.Layer1Height, copyFrom.UndergroundHeight, copyFrom.BaseHeight)
        {
        }

        [Pure]
        public Heights Dig(float deep)
        {
            var newHeight = Layer1Height - deep;
            var newHeightForOre = Layer1Height - deep / 2;
            return new Heights(newHeight, Math.Min(UndergroundHeight, newHeightForOre), BaseHeight);
        }

        [Pure]
        public Heights Build(float pileHeight)
        {
            return new Heights(Layer1Height + pileHeight, UndergroundHeight, BaseHeight);
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
            return new Heights((float)v.Z, (float)v.Y, (float)v.X);
        }

        public override string ToString()
        {
            var noMainLayer = IsLayer1Present ? "" : "*";
            var noUnderLayer = IsUndergroundLayerPresent ? "" : "*";
            return $"({Layer1Height:N1}{noMainLayer}, {UndergroundHeight:N1}{noUnderLayer}, {BaseHeight:N1})";
        }

        public bool Equals(Heights other)
        {
            return BaseHeight.Equals(other.BaseHeight) 
                   && UndergroundHeight.Equals(other.UndergroundHeight) 
                   && Layer1Height.Equals(other.Layer1Height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Heights heights && Equals(heights);
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
