using System;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
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
        private readonly LandSettings _settings;

        protected LayoutGenerator(LandSettings settings)
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
            var timer = Stopwatch.StartNew();

            var bounds = _settings.LandBounds;

            var points = GeneratePoints(bounds, _settings.ZonesRange);
            var cellMesh = CellMeshGenerator.Generate(points, (Bounds)bounds);
            var clusters = SetClusters(cellMesh, _settings);

            LandLayout result;
            if (oldLandLayout == null)
            {
                result = new LandLayout(_settings, cellMesh, clusters);
            }
            else
            {
                oldLandLayout.Update(cellMesh, clusters);
                result = oldLandLayout;
            }

            UnityEngine.Debug.LogFormat("Generated layout: {0} msec", timer.ElapsedMilliseconds);

            return result;
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected abstract Vector2[] GeneratePoints(Bounds2i landBounds, Vector2 density);

        protected abstract ClusterInfo[] SetClusters(CellMesh mesh, LandSettings settings);

        public enum Type
        {
            PoissonTwoSide,
            PoissonClustered
        }
    }
}
