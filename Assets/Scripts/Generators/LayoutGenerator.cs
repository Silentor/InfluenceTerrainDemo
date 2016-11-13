using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Generates <see cref="LandLayout"/> from land settings
    /// </summary>
    public abstract class LayoutGenerator
    {
        private readonly ILandSettings _settings;

        protected LayoutGenerator(ILandSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Generate or update <see cref="LandLayout"/>
        /// </summary>
        /// <param name="oldLandLayout">null - generate from scratch</param>
        /// <returns></returns>
        public virtual LandLayout Generate(LandLayout oldLandLayout)
        {
            var bounds = _settings.LandBounds;

            var points = GeneratePoints(_settings.ZonesCount, bounds, _settings.ZonesDensity);
            var cellMesh = CellMeshGenerator.Generate(points, (Bounds)bounds);
            var zoneTypes = SetZoneTypes(cellMesh.Cells, _settings);

            LandLayout result;
            if (oldLandLayout == null)
            {
                result = new LandLayout(_settings, cellMesh, zoneTypes);
            }
            else
            {
                oldLandLayout.Update(cellMesh, zoneTypes);
                result = oldLandLayout;
            }

            return result;
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected abstract Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density);

        protected abstract ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings);

        public enum Type
        {
            Random,
            PoissonTwoSide,
            PoissonClustered
        }
    }
}
