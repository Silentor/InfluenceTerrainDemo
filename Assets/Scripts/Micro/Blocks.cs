using System.Runtime.InteropServices;
using UnityEngine;

namespace TerrainDemo.Micro
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Blocks
    {
        public BlockType Base;
        public BlockType Layer1;

        //Debug
        public UnityEngine.Vector3 Normal;

        public BlockType Top => Layer1 != BlockType.Empty ? Layer1 : Base;

        public bool IsEmpty => Layer1 == BlockType.Empty && Base == BlockType.Empty;

        //public static readonly Blocks Empty = new Blocks(){Base = BlockType.Empty, Layer1 = BlockType.Empty, Normal = Vector3.up};

        public override string ToString()
        {
            return $"({Layer1}, {Base})";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Blocks))
            {
                return false;
            }

            var blocks = (Blocks)obj;
            return Base == blocks.Base &&
                   Layer1 == blocks.Layer1;
        }

        public override int GetHashCode()
        {
            var hashCode = -862721379;
            hashCode = hashCode * -1521134295 + Base.GetHashCode();
            hashCode = hashCode * -1521134295 + Layer1.GetHashCode();
            return hashCode;
        }
    }
}
