using System;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class DrawCell
    {
        public static void ForGizmo([NotNull] Cell cell)
        {
            ForGizmo(cell, Gizmos.color);
        }

        public static void ForGizmo([NotNull] Cell cell, Color color)
        {
            if (cell == null) throw new ArgumentNullException("cell");

            Gizmos.color = color;
            for (int i = 0; i < cell.Vertices.Length - 1; i++)
                Gizmos.DrawLine(Convert(cell.Vertices[i]), Convert(cell.Vertices[i + 1]));
        }

        public static void ForDebug([NotNull] Cell cell)
        {
            ForDebug(cell, Gizmos.color);
        }

        public static void ForDebug([NotNull] Cell cell, Color color)
        {
            if (cell == null) throw new ArgumentNullException("cell");

            for (int i = 0; i < cell.Vertices.Length - 1; i++)
                Debug.DrawLine(Convert(cell.Vertices[i]), Convert(cell.Vertices[i + 1]), color);

        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }
    }
}
