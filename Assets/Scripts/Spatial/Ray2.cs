using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Tools;

namespace TerrainDemo.Spatial
{
    public readonly struct Ray2
    {
        public readonly Vector2 Origin;
        public readonly Vector2 Direction;

        public Ray2(Vector2 origin, Vector2 direction)
        {
            Origin = origin;
            Direction = direction.Normalized();
        }

        [Pure]
        public Vector2 GetPoint(float distance)
        {
            return Origin + Direction * distance;
        }

        public static explicit operator UnityEngine.Ray(Ray2 input)
        {
            return new UnityEngine.Ray(input.Origin, input.Direction);
        }

        public static explicit operator Ray2(UnityEngine.Ray input)
        {
            return new Ray2((Vector2)input.origin, (Vector2)input.direction);
        }

    }
}
