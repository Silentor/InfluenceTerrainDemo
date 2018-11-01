using TerrainDemo.Spatial;
using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Micro
{
    public struct BlockInfo
    {
        public readonly Blocks Block;
        public readonly float Height;
        public readonly Vector3 Normal;

        public BlockInfo(Blocks block, float height)
        {
            Block = block;
            Height = height;
            Normal = block.Normal;
        }

        public static readonly BlockInfo Empty = new BlockInfo(Blocks.Empty, 0);

        public static Bounds2i GetBounds(Vector2i worldPosition)
        {
            return new Bounds2i(worldPosition, 1, 1);
        }

        public static Vector2 GetCenter(Vector2i worldPosition)
        {
            return new Vector2(worldPosition.X + 0.5f, worldPosition.Z + 0.5f);
        }

    }
}
