﻿using System.Collections.Generic;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;

namespace TerrainDemo.OldCodeToRevision
{
    public class Chunk
    {
        /// <summary>
        /// Chunk linear size (meters) = BlockSize * BlockCount
        /// </summary>
        public const int Size = 16;                 
        private const int Shift = 4;
        private const int Mask = 0x0F;

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
        /// Heightmap of chunk grid
        /// </summary>
        public readonly float[,] HeightMap;

        /// <summary>
        /// Influence of zones for chunk grid
        /// </summary>
        //public readonly ZoneRatio[,] Influence;

        /// <summary>
        /// Types of blocks
        /// </summary>
        public readonly BlockType[,] BlockType;

        /// <summary>
        /// Normals of blocks
        /// </summary>
        public readonly Vector3[,] NormalMap;

        public readonly List<Vector3> Flora = new List<Vector3>();
        public Vector3[] Stones;

        public Chunk(int blocksCount, int blockSize, Vector2i position)
        {
            BlockSize = blockSize;
            BlocksCount = blocksCount;
            Position = position;
            GridSize = BlocksCount + 1;
            HeightMap = new float[GridSize, GridSize];
            //Influence = new ZoneRatio[GridSize, GridSize];
            BlockType = new BlockType[BlocksCount, BlocksCount];
            NormalMap = new Vector3[BlocksCount, BlocksCount];

            //Debug
            Test();
        }

        /// <summary>
        /// Calculate world position of center of chunk
        /// </summary>
        /// <param name="chunkPosition">Chunk position</param>
        /// <returns>World position</returns>
        public static Vector2 GetCenter(GridPos chunkPosition)
        {
            return new Vector2((chunkPosition.X << Shift) + Size / 2, (chunkPosition.Z << Shift) + Size / 2);
        }

        /// <summary>
        /// Get world block position
        /// </summary>
        /// <param name="chunkPosition"></param>
        /// <param name="localPosition">Local block position in chunk</param>
        /// <returns></returns>
        public static Vector2i GetBlockPosition(Vector2i chunkPosition, Vector2i localPosition)
        {
            return new Vector2i(chunkPosition.X << Shift | (localPosition.X & Mask), chunkPosition.Z << Shift | (localPosition.Z & Mask));
        }

        /// <summary>
        /// Get chunk position for belonging world position
        /// </summary>
        public static GridPos GetPositionFromWorld(Vector2 worldPosition)
        {
            return GetPositionFromBlock((GridPos)worldPosition);
        }

        /// <summary>
        /// Get chunk position for belonging world position
        /// </summary>
        public static GridPos GetPositionFromWorld(Vector3 worldPosition)
        {
            return GetPositionFromBlock((GridPos)worldPosition);
        }

        /// <summary>
        /// Get chunk position for belonging block position
        /// </summary>
        /// <param name="blockPosition"></param>
        /// <returns></returns>
        public static GridPos GetPositionFromBlock(GridPos blockPosition)
        {
            return new GridPos(blockPosition.X >> Shift, blockPosition.Z >> Shift);
        }

        /// <summary>
        /// Get 2D world bounds in blocks
        /// </summary>
        /// <param name="position">Chunk position</param>
        /// <returns>World bounds in blocks</returns>
        public static Bound2i GetBounds(GridPos position)
        {
            var min = new GridPos(position.X << Shift, position.Z << Shift);
            var max = new GridPos(min.X + Size - 1, min.Z + Size - 1);
            return new Bound2i(min, max);
        }

        public static Vector2i GetLocalPosition(Vector2 worldPosition)
        {
            return GetLocalPosition((Vector2i) worldPosition);
        }

        public static Vector2i GetLocalPosition(Vector2i worldPosition)
        {
            return new Vector2i(worldPosition.X & Mask, worldPosition.Z & Mask);
        }

        private static void Test()
        {
            //var testCenter = new Vector2(-110.1f, -55.7f);
            ////var testCenter = new Vector2(-1f, -1f);

            //var chunkPos = GetChunkPosition(testCenter);
            //var chunkCenter = GetChunkCenter(chunkPos);

            //var assertDistance = Vector2.Distance(testCenter, chunkCenter);

            //Assert.IsTrue(assertDistance <= Mathf.Sqrt(chunkSize * chunkSize * 2));
        }
    }
}
