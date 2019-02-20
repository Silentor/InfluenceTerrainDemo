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

        public readonly float Base;
        public readonly float Underground;
        public readonly float Main;

        public float Nominal => Main;

        public bool IsMainLayerPresent => Main > Underground;

        public bool IsUndergroundLayerPresent => Underground > Base;

        public static readonly Heights Zero = new Heights(0, 0, 0);

        public Heights(float mainHeight, float undergroundHeight, float baseHeight)
        {
            if (undergroundHeight < baseHeight)
                undergroundHeight = baseHeight;

            if (mainHeight < baseHeight)
                mainHeight = baseHeight;

            if (mainHeight < undergroundHeight)
                mainHeight = undergroundHeight;

            Base = baseHeight;
            Underground = undergroundHeight;
            Main = mainHeight;
        }

        public Heights(Heights copyFrom) : this(copyFrom.Main, copyFrom.Underground, copyFrom.Base)
        {
        }

        [Pure]
        public Heights Dig(float deep)
        {
            var newHeight = Main - deep;
            var newHeightForOre = Main - deep / 2;
            return new Heights(newHeight, Math.Min(Underground, newHeightForOre), Base);
        }

        [Pure]
        public Heights Build(float pileHeight)
        {
            return new Heights(Main + pileHeight, Underground, Base);
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
            return new Vector3d(h.Base, h.Underground, h.Main);
        }

        public static explicit operator Heights(Vector3d v)
        {
            return new Heights((float)v.Z, (float)v.Y, (float)v.X);
        }

        public override string ToString()
        {
            var noMainLayer = IsMainLayerPresent ? "" : "*";
            var noUnderLayer = IsUndergroundLayerPresent ? "" : "*";
            return $"({Main:N1}{noMainLayer}, {Underground:N1}{noUnderLayer}, {Base:N1})";
        }

        public bool Equals(Heights other)
        {
            return Base.Equals(other.Base) 
                   && Underground.Equals(other.Underground) 
                   && Main.Equals(other.Main);
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
                var hashCode = Base.GetHashCode();
                hashCode = (hashCode * 397) ^ Underground.GetHashCode();
                hashCode = (hashCode * 397) ^ Main.GetHashCode();
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
