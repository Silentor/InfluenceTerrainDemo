using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class DrawPoint
    {
        public static void ForFebug2D(Vector3 position, float scale, Color color, bool depthTest = true)
        {
            Debug.DrawLine(position - new Vector3(scale / 2, 0, 0), position + new Vector3(scale / 2, 0, 0), color, 0, depthTest);
            Debug.DrawLine(position - new Vector3(0, 0, scale / 2), position + new Vector3(0, 0, scale / 2), color, 0, depthTest);
        }

        public static void ForFebug2D(IEnumerable<Vector3> positions, float scale, Color color, bool depthTest = true)
        {
            var xOffset = new Vector3(scale / 2, 0, 0);
            var zOffset = new Vector3(0, 0, scale / 2);

            foreach (var position in positions)
            {
                Debug.DrawLine(position - xOffset, position + xOffset, color, 0, depthTest);
                Debug.DrawLine(position - zOffset, position + zOffset, color, 0, depthTest);
            }
        }
    }
}
