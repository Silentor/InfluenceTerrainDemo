using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;

namespace TerrainDemo.Generators
{
    public class DefaultClusterGenerator : ClusterGenerator
    {
        public DefaultClusterGenerator([NotNull] LandSettings settings, [NotNull] Mesh<ZoneLayout> mesh, int clusterId, ClusterType type) : base(settings, mesh, clusterId, type)
        {
        }
    }
}
