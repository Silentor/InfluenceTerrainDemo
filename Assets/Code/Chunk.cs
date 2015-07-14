using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Code
{
    public class Chunk
    {
        public readonly int BlockSize;
        public readonly int BlocksCount;
        public readonly Vector2 Position;
        public readonly int GridSize;
        public readonly int Size;
        public readonly Vector4 Type;

        public float[,] HeightMap;

        public Chunk(int blocksCount, int blockSize, Vector2 position, Vector4 type)
        {
            Type = type;
            BlockSize = blockSize;
            BlocksCount = blocksCount;
            Position = position;
            GridSize = BlocksCount + 1;
            Size = BlocksCount * BlockSize;
            HeightMap = new float[GridSize, GridSize];
        }
    }
}
