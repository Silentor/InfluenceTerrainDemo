using System;
using JetBrains.Annotations;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class DrawPolyline
    {
        public static void ForGizmo([NotNull] Vector3[] line, bool closePolygon = false)
        {
            ForGizmo(line, Gizmos.color, closePolygon);
        }

        public static void ForGizmo([NotNull] Vector3[] line, Color color, bool closePolygon)
        {
            if (line == null) throw new ArgumentNullException("line");
            if(line.Length < 2)
                return;

            Gizmos.color = color;
            for (int i = 0; i < line.Length - 1; i++)
                Gizmos.DrawLine(line[i], line[i + 1]);
            if(closePolygon)
                Gizmos.DrawLine(line[line.Length - 1], line[0]);
        }

        public static void ForDebug([NotNull] Vector3[] line, bool closePolygon = false)
        {
            ForDebug(line, Gizmos.color, closePolygon);
        }

        public static void ForDebug([NotNull] Vector3[] line, Color color, bool closePolygon)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (line.Length < 2)
                return;

            for (int i = 0; i < line.Length - 1; i++)
                Debug.DrawLine(line[i], line[i + 1], color, 0);
            if (closePolygon)
                Debug.DrawLine(line[line.Length - 1], line[0], color, 0);
        }
    }
}
