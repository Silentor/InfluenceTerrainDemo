using JetBrains.Annotations;
using OpenTK;

namespace TerrainDemo.Spatial
{
    public readonly struct Ray
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction.Normalized();
        }

        [Pure]
        public Vector3 GetPoint(float distance)
        {
            return Origin + Direction * distance;
        }

        public static explicit operator UnityEngine.Ray(Ray input)
        {
            return new UnityEngine.Ray(input.Origin, input.Direction);
        }

        public static explicit operator Ray(UnityEngine.Ray input)
        {
            return new Ray(input.origin, input.direction);
        }

    }
}
