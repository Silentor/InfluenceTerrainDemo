using TerrainDemo.Generators;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    public class ClusterInfo
    {
        public int Id;
        public ClusterType Type;
        public ZoneInfo[] Zones;
        public Mesh<ZoneInfo>.Submesh Mesh;
        public Mesh<ZoneInfo>.Face Center;
        public ClusterGenerator Generator;
    }
}
