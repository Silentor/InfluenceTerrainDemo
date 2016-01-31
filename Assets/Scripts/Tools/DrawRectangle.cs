using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class DrawRectangle
    {
        public static void ForGizmo(Bounds2i rectangle)
        {
            ForGizmo(rectangle, Gizmos.color);
        }

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

        public static void ForDebug(Bounds2i rectangle, Color color, float duration = 0)
        {
            var corner1 = new Vector3(rectangle.Min.X, 0, rectangle.Min.Z);
            var corner2 = new Vector3(rectangle.Min.X, 0, rectangle.Max.Z + 1);
            var corner3 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Max.Z + 1);
            var corner4 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Min.Z);

            Debug.DrawLine(corner1, corner2, color, duration);
            Debug.DrawLine(corner2, corner3, color, duration);
            Debug.DrawLine(corner3, corner4, color, duration);
            Debug.DrawLine(corner4, corner1, color, duration);
        }

        public static void ForDebug(Vector3 r1, Vector3 r2, Vector3 r3, Vector3 r4, Color color, float duration = 0)
        {
            Debug.DrawLine(r1, r2, color, duration);
            Debug.DrawLine(r2, r3, color, duration);
            Debug.DrawLine(r3, r4, color, duration);
            Debug.DrawLine(r4, r1, color, duration);
        }
    }
}
