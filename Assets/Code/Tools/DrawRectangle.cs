using UnityEngine;

namespace Assets.Code.Tools
{
    public static class DrawRectangle
    {
        public static void ForGizmo(Bounds2i rectangle, Color color)
        {
            var corner1 = new Vector3(rectangle.Min.X, 0, rectangle.Min.Z);
            var corner2 = new Vector3(rectangle.Min.X, 0, rectangle.Max.Z + 1);
            var corner3 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Max.Z + 1);
            var corner4 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Min.Z);

            Gizmos.color = color;
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);
        }
    }
}
