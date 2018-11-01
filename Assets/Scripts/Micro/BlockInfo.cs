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
        public readonly MicroHeight Corner00;
        public readonly MicroHeight Corner01;
        public readonly MicroHeight Corner11;
        public readonly MicroHeight Corner10;
        public readonly Vector3 Normal;

        public BlockInfo(Vector2i position, Blocks block, MicroHeight corner00, MicroHeight corner01, MicroHeight corner11, MicroHeight corner10)
        {
            Position = position;
            Block = block;
            Corner00 = corner00;
            Corner01 = corner01;
            Corner11 = corner11;
            Corner10 = corner10;
            Height = (Corner00.Height + Corner01.Height + Corner11.Height + Corner10.Height) / 4;
            Normal = GetBlockNormal(Corner00.Height, Corner11.Height, Corner01.Height, Corner10.Height);
        }

        public static Bounds2i GetBounds(Vector2i worldPosition)
        {
            return new Bounds2i(worldPosition, 1, 1);
        }

        public static Vector2 GetCenter(Vector2i worldPosition)
        {
            return new Vector2(worldPosition.X + 0.5f, worldPosition.Z + 0.5f);
        }

        public Vector3 GetCenter()
        {
            return new Vector3(Position.X + 0.5f, Height, Position.Z + 0.5f);
        }

        private Vector3 GetBlockNormal(float height00, float height11, float height01, float height10)
        {
            var slope1 = height11 - height00;
            var slope2 = height10 - height01;
            var result = new Vector3(-slope1, 2, slope2);
            return result.normalized;
        }

    }
}
