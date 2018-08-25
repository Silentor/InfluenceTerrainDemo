using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Micro
{
    public struct BlockInfo
    {
        public readonly Vector2i Position;
        public readonly BlockType Type;
        public readonly float Height;
        public Vector3 Normal;
        public readonly double[] Influence;

        public BlockInfo(Vector2i position, BlockType type, float height, Vector3 normal, double[] influence)
        {
            Position = position;
            Type = type;
            Height = height;
            Normal = normal;
            Influence = influence;
        }

        public static readonly Map.BlockInfo Empty = new Map.BlockInfo(Vector2i.Zero, BlockType.Empty, 0, Vector3.zero);

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
