using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Code
{
    public class Chunk
    {
        /// <summary>
        /// Blocks size (meters)
        /// </summary>
        public readonly int BlockSize;
        /// <summary>
        /// Side blocks count
        /// </summary>
        public readonly int BlocksCount;
        /// <summary>
        /// Chunk position (chunk units)
        /// </summary>
        public readonly Vector2i Position;
        /// <summary>
        /// Heightmap points side count
        /// </summary>
        public readonly int GridSize;
        /// <summary>
        /// Chunk side lenght (meters)
        /// </summary>
        public readonly int Size;

        public readonly float[,] HeightMap;
        public readonly ZoneRatio[,] Influence;
        public readonly BlockType[,] BlockType;
        public Vector3[] Flora;
        public Vector3[] Stones;

        public Chunk(int blocksCount, int blockSize, Vector2i position)
        {
            BlockSize = blockSize;
            BlocksCount = blocksCount;
            Position = position;
            GridSize = BlocksCount + 1;
            Size = BlocksCount * BlockSize;
            HeightMap = new float[GridSize, GridSize];
            Influence = new ZoneRatio[GridSize, GridSize];
            BlockType = new BlockType[BlocksCount, BlocksCount];
        }

        /// <summary>
        /// Calculate world position of center of chunk
        /// </summary>
        /// <param name="position">Chunk position</param>
        /// <param name="chunkSize"></param>
        /// <returns>World position</returns>
        public static Vector2 GetChunkCenter(Vector2i position, int chunkSize)
        {
            return new Vector2(chunkSize * (0.5f + position.X), chunkSize * (0.5f + position.Z));
        }

        public static Vector2i GetChunkPosition(Vector2 worldPosition, int chunkSize)
        {
            return new Vector2i(((int)worldPosition.x) / chunkSize, ((int)worldPosition.y) / chunkSize);
        }

        /// <summary>
        /// Get 2D world bounds of chunk
        /// </summary>
        /// <param name="position">Chunk position</param>
        /// <param name="chunkSize"></param>
        /// <returns>World bounds</returns>
        public static Bounds GetChunkBounds(Vector2i position, int chunkSize)
        {
            return new Bounds(GetChunkCenter(position, chunkSize), new Vector3(chunkSize, chunkSize, chunkSize));
        }
    }
}
