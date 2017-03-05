using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Layout properties of Cluster (Biome)
    /// </summary>
    public class ClusterLayout
    {
        public readonly int Id;

        public readonly ZoneType Type;

        /// <summary>
        /// Points to build land base height
        /// </summary>
        public readonly Vector3[] BaseHeightPoints;

        /// <summary>
        /// Zones of this Cluster
        /// </summary>
        public readonly IEnumerable<ZoneLayout> Zones;

        /// <summary>
        /// Unsorted edges of cluster. TODO sort them
        /// </summary>
        public readonly IEnumerable<Edge> Edges;

        public readonly CellMesh.Submesh Cells;

        public ClusterLayout[] Neighbors { get; private set; }

        private ClusterInfo[] _neighbors;

        public ClusterLayout(ClusterInfo info, IEnumerable<ZoneLayout> zones, CellMesh mesh)
        {
            if (info.ClusterHeight == null) throw new ArgumentNullException("baseHeightPoints");
            if (info.ZoneHeights == null) throw new ArgumentNullException("baseHeightPoints");
            if (zones == null) throw new ArgumentNullException("zones");

            Id = info.Id;
            //BaseHeightPoints = info.ClusterHeights;
            Zones = zones;
            Type = info.Type;
            _neighbors = info.Neighbors;
            Cells = info.Mesh;

            //Get edges
            var cells = zones.Select(z => z.Cell).ToArray();
            var cluster = new CellMesh.Submesh(mesh, cells);
            var outerEdges = cluster.GetBorderCells().SelectMany(c => c.Edges).Where(e => !cells.Contains(e.Neighbor));
            var edges = new List<Edge>();
            foreach (var outerEdge in outerEdges)
                edges.Add(new Edge {Vertex1 = outerEdge.Vertex1, Vertex2 = outerEdge.Vertex2});
            Edges = edges;
        }

        public void Init(ClusterLayout[] layout)
        {
            Neighbors = _neighbors.Select(c => layout.First(c2 => c2.Id == c.Id)).ToArray();
        }

        public struct Edge
        {
            public Vector2 Vertex1;
            public Vector2 Vertex2;
        }
    }
}
