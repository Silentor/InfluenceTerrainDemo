using System.Runtime.InteropServices;

namespace TerrainDemo.Micro
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Blocks
    {
        public BlockType Base;
        public BlockType Layer1;

        public BlockType Top => Layer1 != BlockType.Empty ? Layer1 : Base;

        public static readonly Blocks Empty = new Blocks(){Base = BlockType.Empty, Layer1 = BlockType.Empty};

        public override string ToString()
        {
            return $"({Layer1}, {Base})";
        }
    }
}
