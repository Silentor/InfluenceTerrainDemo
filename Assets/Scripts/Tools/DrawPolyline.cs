using System;
using JetBrains.Annotations;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class DrawPolyline
    {
        public static void ForGizmo([NotNull] Vector3[] line)
        {
            ForGizmo(line, Gizmos.color);
        }

        public static void ForGizmo([NotNull] Vector3[] line, Color color)
        {
            if (line == null) throw new ArgumentNullException("line");

            Gizmos.color = color;
            if(line.Length > 1)
                for (int i = 0; i < line.Length - 1; i++)
                {
                    Gizmos.DrawLine(line[i], line[i + 1]);
                }
        }

        public static void ForDebug([NotNull] Vector3[] line)
        {
            ForDebug(line, Gizmos.color);
        }

        public static void ForDebug([NotNull] Vector3[] line, Color color)
        {
            if (line == null) throw new ArgumentNullException("line");

            if (line.Length > 1)
                for (int i = 0; i < line.Length - 1; i++)
                {
                    Debug.DrawLine(line[i], line[i + 1], color, 0);
                }
        }
    }
}
