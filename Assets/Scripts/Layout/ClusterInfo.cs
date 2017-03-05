using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    public class ClusterInfo
    {
        public int Id;
        public ZoneType Type;
        public ZoneInfo[] Zones;
        public Vector3 ClusterHeight;
        public Vector3[] ZoneHeights = new Vector3[0];
        public ClusterInfo[] Neighbors = new ClusterInfo[0];
        public CellMesh.Submesh Mesh;
        public Cell Center;
    }
}
