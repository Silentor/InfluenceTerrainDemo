using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Code
{
    public class Chunk
    {
        public const int Size = 16;
        private const int Shift = 4;

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
            HeightMap = new float[GridSize, GridSize];
            Influence = new ZoneRatio[GridSize, GridSize];
            BlockType = new BlockType[BlocksCount, BlocksCount];

            //Debug
            Test();
        }

        /// <summary>
        /// Calculate world position of center of chunk
        /// </summary>
        /// <param name="position">Chunk position</param>
        /// <returns>World position</returns>
        public static Vector2 GetChunkCenter(Vector2i position)
        {
            return new Vector2((position.X << Shift) + Size / 2, (position.Z << Shift) + Size / 2);
        }

        public static Vector2i GetChunkPosition(Vector2 worldPosition)
        {
            return new Vector2i((int)worldPosition.x >> Shift, (int)worldPosition.y >> Shift);
        }

        public static Vector2i GetChunkPosition(Vector2i worldPosition)
        {
            return new Vector2i(worldPosition.X >> Shift, worldPosition.Z >> Shift);
        }

        /// <summary>
        /// Get 2D world bounds of chunk
        /// </summary>
        /// <param name="position">Chunk position</param>
        /// <returns>World bounds</returns>
        public static Bounds GetChunkBounds(Vector2i position)
        {
            return new Bounds(GetChunkCenter(position), new Vector3(Size, Size, Size));
        }

        private static void Test()
        {
            var a = 16 >> 4;
            var b = -17 >> 4;
            var c = -17 / 16;

            const int chunkSize = 16;
            var testCenter = new Vector2(-110.1f, -55.7f);
            //var testCenter = new Vector2(-1f, -1f);

            var chunkPos = GetChunkPosition(testCenter);
            var chunkCenter = GetChunkCenter(chunkPos);

            var assertDistance = Vector2.Distance(testCenter, chunkCenter);

            Assert.IsTrue(assertDistance <= Mathf.Sqrt(chunkSize * chunkSize * 2));
        }
    }
}
