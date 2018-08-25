using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Generators;
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

        public readonly ClusterType Type;

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

        public readonly Mesh<ZoneLayout>.Submesh Mesh;

        public ClusterGenerator Generator;

        public ClusterLayout(ClusterInfo info, Mesh<ZoneLayout>.Submesh mesh, Graph<ClusterLayout>.Node node, LandLayout land)
        {
            //if (info.ClusterHeight == null) throw new ArgumentNullException("baseHeightPoints");
            //if (info.ZoneHeights == null) throw new ArgumentNullException("baseHeightPoints");
            if (mesh == null) throw new ArgumentNullException("mesh");

            Id = info.Id;
            //BaseHeightPoints = info.ClusterHeights;
            Zones = mesh.Select(zl => zl.Data).ToArray();
            Type = info.Type;
            //_neighbors = info.NeighborsSafe;
            Mesh = mesh;

            //Get edges
            var cells = Zones.Select(z => z.Face).ToArray();
            //var outerEdges = mesh.GetBorderCells().SelectMany(c => c.Edges).Where(e => !cells.Contains(e.Neighbor));
            var edges = new List<Edge>();
            //foreach (var outerEdge in outerEdges)
                //edges.Add(new Edge {Vertex1 = outerEdge.Vertex1, Vertex2 = outerEdge.Vertex2});
            Edges = edges;
        }

        public struct Edge
        {
            public Vector2 Vertex1;
            public Vector2 Vertex2;
        }
    }
}
