using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;

namespace TerrainDemo.Micro
{
    public readonly struct BlockData : IEquatable<BlockData>
    {
        public readonly float Height;
        public readonly float MinHeight;
        public readonly float MaxHeight;
        public readonly Vector3 Normal;

        public bool IsEmpty => Normal == Vector3.Zero;

        public static readonly BlockData Empty = new BlockData();

        public BlockData(in Heights h00, in Heights h01, in Heights h10, in Heights h11)
        {
            Height = (h00.Nominal + h01.Nominal + h10.Nominal + h11.Nominal) / 4;
            MinHeight = h00.Base;
            MaxHeight = h00.Nominal;

            if (h01.Base < MinHeight) MinHeight = h01.Base;
            if (h10.Base < MinHeight) MinHeight = h10.Base;
            if (h11.Base < MinHeight) MinHeight = h11.Base;
            if (h01.Nominal > MaxHeight) MaxHeight = h01.Nominal;
            if (h10.Nominal > MaxHeight) MaxHeight = h10.Nominal;
            if (h11.Nominal > MaxHeight) MaxHeight = h11.Nominal;

            var normal = Vector3.Cross(                             //todo simplify
                new Vector3(-1, h01.Nominal - h10.Nominal, 1),
                new Vector3(1, h11.Nominal - h00.Nominal, 1)
            );
            Normal = normal.Normalized();
        }

        public bool Equals(BlockData other)
        {
            if (IsEmpty && other.IsEmpty)
                return true;

            return Height.Equals(other.Height) && Normal.Equals(other.Normal);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is BlockData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Height.GetHashCode() * 397) ^ Normal.GetHashCode();
            }
        }

        public static bool operator ==(BlockData left, BlockData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockData left, BlockData right)
        {
            return !left.Equals(right);
        }
    }
}
