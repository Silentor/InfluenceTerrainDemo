using System;
using System.Collections.Generic;

namespace TerrainDemo.Voronoi
{
    public class GraphEdgeComparer : IEqualityComparer<GraphEdge>
    {
        public static readonly GraphEdgeComparer Default = new GraphEdgeComparer();

        public bool Equals(GraphEdge x, GraphEdge y)
        {
            return Math.Abs(x.x1 - y.x1) <= 0.001 &&
                   Math.Abs(x.x2 - y.x2) <= 0.001 &&
                   Math.Abs(x.y1 - y.y1) <= 0.001 &&
                   Math.Abs(x.y2 - y.y2) <= 0.001;
        }

        public int GetHashCode(GraphEdge obj)
        {
            return obj.x1.GetHashCode() ^ obj.x2.GetHashCode() ^ obj.y1.GetHashCode() ^ obj.y2.GetHashCode();
        }
    }
}