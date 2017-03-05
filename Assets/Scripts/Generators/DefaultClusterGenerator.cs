using JetBrains.Annotations;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;

namespace TerrainDemo.Generators
{
    public class DefaultClusterGenerator : ClusterGenerator
    {
        public DefaultClusterGenerator([NotNull] LandSettings settings, [NotNull] CellMesh mesh, int clusterId, ZoneType type) : base(settings, mesh, clusterId, type)
        {
        }
    }
}
