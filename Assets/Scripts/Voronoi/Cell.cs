using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Voronoi
{
    [DebuggerDisplay("Cell(Id={Id}, Center={Center}, Neighs={Neighbors.Length}, IsClosed={IsClosed})")]
    public class Cell
    {
        /// <summary>
        /// Unique id of cell in mesh
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Position of cell center
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// Clockwise sorted vertices
        /// </summary>
        public readonly Vector2[] Vertices;

        /// <summary>
        /// Clockwise oriented edges
        /// </summary>
        public readonly Edge[] Edges;

        /// <summary>
        /// Clockwise oriented neighbor cells
        /// </summary>
        public Cell[] Neighbors { get; private set; }

        public readonly Bounds Bounds;

        public readonly bool IsClosed;

        public Cell(int id, Vector2 center, bool isClosed, [NotNull] Vector2[] vertices, [NotNull] Edge[] edges, Bounds meshBounds)
        {
            if (vertices == null) throw new ArgumentNullException("vertices");
            if (edges == null) throw new ArgumentNullException("edges");

            Id = id;
            Center = center;
            Vertices = vertices;
            Edges = edges;
            IsClosed = isClosed;

            //Calculate cell bounds (respecting mesh bounds)
            float? xMin = null, xMax = null, yMin = null, yMax = null;

            //Additional check for same edge cases
            if (!IsClosed)
            {
                if (!CheckEdges(new Vector2(meshBounds.min.x, meshBounds.min.z)))
                {
                    xMin = meshBounds.min.x;
                    yMin = meshBounds.min.z;
                }
                if (!CheckEdges(new Vector2(meshBounds.min.x, meshBounds.max.z)))
                {
                    xMin = meshBounds.min.x;
                    yMax = meshBounds.max.z;
                }
                if (!CheckEdges(new Vector2(meshBounds.max.x, meshBounds.max.z)))
                {
                    xMax = meshBounds.max.x;
                    yMax = meshBounds.max.z;
                }
                if (!CheckEdges(new Vector2(meshBounds.max.x, meshBounds.min.z)))
                {
                    xMax = meshBounds.max.x;
                    yMin = meshBounds.min.z;
                }
            }

            foreach (var vert in Vertices)
            {
                if (vert.x <= center.x && (!xMin.HasValue || vert.x < xMin)) xMin = vert.x;
                if (vert.x >= center.x && (!xMax.HasValue || vert.x > xMax)) xMax = vert.x;
                if (vert.y <= center.y && (!yMin.HasValue || vert.y < yMin)) yMin = vert.y;
                if (vert.y >= center.y && (!yMax.HasValue || vert.y > yMax)) yMax = vert.y;
            }

            if (xMin == null) xMin = meshBounds.min.x;
            if (xMax == null) xMax = meshBounds.max.x;
            if (yMin == null) yMin = meshBounds.min.z;
            if (yMax == null) yMax = meshBounds.max.z;

            var boundsCenter = new Vector3((xMax.Value - xMin.Value) / 2 + xMin.Value, 0, (yMax.Value - yMin.Value) / 2 + yMin.Value);
            var boundsSize = new Vector3(xMax.Value - xMin.Value, 0, yMax.Value - yMin.Value);
            Bounds = new Bounds(boundsCenter, boundsSize);
        }

        public void Init(Cell[] neighbors)
        {
            Neighbors = neighbors;
        }

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

        public float GetArea()
        {
            //Cached value
            if(_area > 0)
                return _area;

            //Convex polygon area
            _area = 0f;
            for (var i = 0; i < Vertices.Length; i++)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[(i + 1) % Vertices.Length];
                _area += (v1.x + v2.x)*(v1.y - v2.y);
            }

            _area = Mathf.Abs(_area) /2;
            return _area;
        }

        private float _area = -1;

        private bool CheckEdges(Vector2 boundsCorner)
        {
            foreach (var edge in Edges)
                if (Intersections.LineSegmentIntersection(Center, boundsCorner, edge.Vertex1, edge.Vertex2))
                    return true;

            return false;
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
