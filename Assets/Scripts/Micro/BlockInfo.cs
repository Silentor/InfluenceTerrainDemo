﻿using System.Runtime.CompilerServices;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Helper block structure, combine all block properties
    /// </summary>
    public class BlockInfo
    {
        public readonly Vector2i Position;
        public readonly Blocks Block;
        public readonly float Height;
        public readonly Heights Corner00;
        public readonly Heights Corner01;
        public readonly Heights Corner11;
        public readonly Heights Corner10;
        public readonly Vector3 Normal;

        public BlockInfo(Vector2i position, Blocks block, Heights corner00, Heights corner01, Heights corner11, Heights corner10)
        {
            Position = position;
            Block = block;
            Corner00 = corner00;
            Corner01 = corner01;
            Corner11 = corner11;
            Corner10 = corner10;
            Height = (Corner00.Nominal + Corner01.Nominal + Corner11.Nominal + Corner10.Nominal) / 4;
            Normal = block
                .Normal; // GetBlockNormal(Corner00.Nominal, Corner11.Nominal, Corner01.Nominal, Corner10.Nominal);
        }

        public static Bounds2i GetBounds(Vector2i worldPosition)
        {
            return new Bounds2i(worldPosition, 1, 1);
        }

        public static Vector2 GetWorldCenter(Vector2i blockPosition)
        {
            return new Vector2(blockPosition.X + 0.5f, blockPosition.Z + 0.5f);
        }
        public static Vector2 GetWorldCenter(int blockPositionX, int blockPositionZ)
        {
            return new Vector2(blockPositionX + 0.5f, blockPositionZ + 0.5f);
        }

        public Vector3 GetCenter()
        {
            return new Vector3(Position.X + 0.5f, Height, Position.Z + 0.5f);
        }

        //based on http://www.flipcode.com/archives/Calculating_Vertex_Normals_for_Height_Maps.shtml
        public static Vector3 GetBlockNormal(float height00, float height11, float height01, float height10)
        {
            var slope1 = height11 - height00;
            var slope2 = height10 - height01;
            var result = new Vector3(-slope1, 2, slope2);
            return result.normalized;
        }

    }
}