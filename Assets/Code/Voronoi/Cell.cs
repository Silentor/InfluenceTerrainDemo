using System.Diagnostics;
using UnityEngine;

namespace Assets.Code.Voronoi
{
    [DebuggerDisplay("Cell(Id={Id}, Center={Center}, Neighs={Neighbors.Length}, IsClosed={IsClosed})")]
    public class Cell
    {
        /// <summary>
        /// Unique id of cell in mesh
        /// </summary>
        public int Id;

        /// <summary>
        /// Position of cell center
        /// </summary>
        public Vector2 Center;

        /// <summary>
        /// Clockwise sorted vertices
        /// </summary>
        public Vector2[] Vertices;

        /// <summary>
        /// Clockwise oriented edges
        /// </summary>
        public Edge[] Edges;

        /// <summary>
        /// Clockwise oriented neighbor cells
        /// </summary>
        public Cell[] Neighbors;

        public Bounds Bounds;

        public bool IsClosed;

        /// <summary>
        /// Check if position contains in cell
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsContains(Vector2 position)
        {
            foreach (var edge in Edges)
            {
                if ((position.y - edge.Vertex1.y)*(edge.Vertex2.x - edge.Vertex1.x) -
                    (position.x - edge.Vertex1.x)*(edge.Vertex2.y - edge.Vertex1.y) > 0)
                    return false;
            }

            return true;
        }

        public struct Edge
        {
            public readonly Vector2 Vertex1;
            public readonly Vector2 Vertex2;

            public Edge(Vector2 vertex1, Vector2 vertex2)
            {
                Vertex1 = vertex1;
                Vertex2 = vertex2;
            }

            public Edge Reverse()
            {
                return new Edge(Vertex2, Vertex1);
            }
        }
    }
}
