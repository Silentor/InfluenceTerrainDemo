using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using TerrainDemo.Spatial;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tools
{
    public static class DrawRectangle
    {
        [Conditional("UNITY_EDITOR")]
        public static void ForGizmo(Box2 rectangle, Color color, bool filled = false)
        {
            var corner1 = new Vector3(rectangle.Left, 0, rectangle.Bottom);
            var corner2 = new Vector3(rectangle.Left, 0, rectangle.Top);
            var corner3 = new Vector3(rectangle.Right, 0, rectangle.Top);
            var corner4 = new Vector3(rectangle.Right, 0, rectangle.Bottom);

            var points = new[] { corner1, corner2, corner3, corner4, corner1 };

            Gizmos.color = color;
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);

            if (filled)
            {
                Gizmos.DrawLine(corner1, corner3);
                Gizmos.DrawLine(corner2, corner4);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void ForGizmo(Bounds2i rectangle)
        {
            ForGizmo(rectangle, Gizmos.color);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ForGizmo(Bounds2i rectangle, Color color, bool filled = false)
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

            if (filled)
            {
                Gizmos.DrawLine(corner1, corner3);
                Gizmos.DrawLine(corner2, corner4);
            }
        }

        [Conditional("UNITY_EDITOR")]
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

        [Conditional("UNITY_EDITOR")]
        public static void ForDebug(Vector3 r1, Vector3 r2, Vector3 r3, Vector3 r4, Color color, float duration = 0)
        {
            Debug.DrawLine(r1, r2, color, duration);
            Debug.DrawLine(r2, r3, color, duration);
            Debug.DrawLine(r3, r4, color, duration);
            Debug.DrawLine(r4, r1, color, duration);
        }

#if UNITY_EDITOR
        [Conditional("UNITY_EDITOR")]
        public static void ForHandle(Bounds2i rectangle, Color color, uint width = 0, bool filled = false)
        {
            var corner1 = new Vector3(rectangle.Min.X, 0, rectangle.Min.Z);
            var corner2 = new Vector3(rectangle.Min.X, 0, rectangle.Max.Z + 1);
            var corner3 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Max.Z + 1);
            var corner4 = new Vector3(rectangle.Max.X + 1, 0, rectangle.Min.Z);

            var points = new[] {corner1, corner2, corner3, corner4, corner1};

            Handles.color = color;
            if(width == 0)
                Handles.DrawPolyLine(points);
            else
                Handles.DrawAAPolyLine(width, points);

            if (filled)
            {
                Handles.DrawLine(corner1, corner3);
                Handles.DrawLine(corner2, corner4);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void ForHandle(Bounds rectangle, Color color, bool filled = false)
        {
            var corner1 = new Vector3(rectangle.min.x, 0, rectangle.min.z);
            var corner2 = new Vector3(rectangle.min.x, 0, rectangle.max.z);
            var corner3 = new Vector3(rectangle.max.x, 0, rectangle.max.z);
            var corner4 = new Vector3(rectangle.max.x, 0, rectangle.min.z);

            var points = new[] { corner1, corner2, corner3, corner4, corner1 };

            Handles.color = color;
            Handles.DrawPolyLine(points);

            if (filled)
            {
                Handles.DrawLine(corner1, corner3);
                Handles.DrawLine(corner2, corner4);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void ForHandle(Box2 rectangle, Color color, bool filled = false)
        {
            var corner1 = new Vector3(rectangle.Left, 0, rectangle.Bottom);
            var corner2 = new Vector3(rectangle.Left, 0, rectangle.Top);
            var corner3 = new Vector3(rectangle.Right, 0, rectangle.Top);
            var corner4 = new Vector3(rectangle.Right, 0, rectangle.Bottom);

            var points = new[] { corner1, corner2, corner3, corner4, corner1 };

            Handles.color = color;
            Handles.DrawPolyLine(points);

            if (filled)
            {
                Handles.DrawLine(corner1, corner3);
                Handles.DrawLine(corner2, corner4);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void ForHandle(Vector3 c00, Vector3 c01, Vector3 c11, Vector3 c10, Color color, uint width = 0, bool filled = false)
        {
            var points = new[] { c00, c01, c11, c10, c00 };

            Handles.color = color;
            if(width == 0)
                Handles.DrawPolyLine(points);
            else
                Handles.DrawAAPolyLine(width, points);

            if (filled)
            {
                Handles.DrawLine(c00, c11);
                Handles.DrawLine(c01, c10);
            }
        }
#endif
    }
}
