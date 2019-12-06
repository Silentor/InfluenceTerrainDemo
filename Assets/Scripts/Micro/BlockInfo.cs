using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Helper block structure, combine all block properties. Handy but slow, mostly for debug purposes
    /// </summary>
    public readonly struct BlockInfo : IEquatable<BlockInfo>
    {
        public readonly GridPos Position;
        public readonly Blocks Block;
        public readonly Heights Corner00;
        public readonly Heights Corner01;
        public readonly Heights Corner11;
        public readonly Heights Corner10;
        public readonly Vector3 Normal;
        public readonly float Height;

        public Vector3 Position00 => new Vector3(Position.X, Corner00.Nominal, Position.Z);
        public Vector3 Position01 => new Vector3(Position.X, Corner01.Nominal, Position.Z + 1);
        public Vector3 Position10 => new Vector3(Position.X + 1, Corner10.Nominal, Position.Z);
        public Vector3 Position11 => new Vector3(Position.X + 1, Corner11.Nominal, Position.Z + 1);

        private readonly BaseBlockMap _map;

        public BlockInfo(GridPos position, BaseBlockMap map)
        {
            Position = position;
            Block = map.GetBlockRef(position);
            Corner00 = map.GetHeightRef(position);
            Corner01 = map.GetHeightRef(position + Vector2i.Unit01);
            Corner11 = map.GetHeightRef(position + Vector2i.Unit11); 
            Corner10 = map.GetHeightRef(position + Vector2i.Unit10);
            ref readonly var data = ref map.GetBlockData(position);
            Normal = data.Normal;
            Height = data.Height;
            _map = map;
        }

        public static Bounds2i GetBounds(GridPos worldPosition)
        {
            return new Bounds2i(worldPosition, 1, 1);
        }

        public static (Vector2 min, Vector2 max) GetWorldBounds(GridPos worldPosition)
        {
            return (new Vector2(worldPosition.X, worldPosition.Z),
                new Vector2(worldPosition.X + 1, worldPosition.Z + 1));
        }

        public static Vector2 GetWorldCenter(GridPos blockPosition)
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

        /*
        //based on http://www.flipcode.com/archives/Calculating_Vertex_Normals_for_Height_Maps.shtml
        public static Vector3 GetBlockNormal(float height00, float height11, float height01, float height10)
        {
            var slope1 = height11 - height00;
            var slope2 = height10 - height01;
            var result = new Vector3(-slope1, 2, slope2);
            return result.Normalized();
        }
        */

        public bool Equals(BlockInfo other)
        {
            return Position.Equals(other.Position) && Block.Equals(other.Block) && Corner00.Equals(other.Corner00) && Corner01.Equals(other.Corner01) && Corner11.Equals(other.Corner11) && Corner10.Equals(other.Corner10) && Normal.Equals(other.Normal) && Height.Equals(other.Height) && Equals(_map, other._map);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is BlockInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Block.GetHashCode();
                hashCode = (hashCode * 397) ^ Corner00.GetHashCode();
                hashCode = (hashCode * 397) ^ Corner01.GetHashCode();
                hashCode = (hashCode * 397) ^ Corner11.GetHashCode();
                hashCode = (hashCode * 397) ^ Corner10.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                hashCode = (hashCode * 397) ^ (_map != null ? _map.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(BlockInfo left, BlockInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockInfo left, BlockInfo right)
        {
            return !left.Equals(right);
        }
    }
}
