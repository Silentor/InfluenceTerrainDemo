using System;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Interpolate float value on cellmesh
    /// </summary>
    public class IDWMeshInterpolator
    {
        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;

        public IDWMeshInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
        {
            _mesh = mesh;
            _values = values;
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (values == null) throw new ArgumentNullException("values");
            if(mesh.Cells.Length != values.Length) throw new ArgumentException("mesh length != values length");
        }

        public double GetValue(Vector2 position)
        {
            var result = 0.0;
            var weigths = 0.0;
            //todo need spatial optimization
            var nearestZones = _mesh.Cells.OrderBy(z => Vector2.SqrMagnitude(z.Center - position)).Take(7).ToArray();
            var searchRadius = Vector2.Distance(nearestZones.Last().Center, position);

            //Sum up zones influence
            foreach (var cell in nearestZones)
            {
                var cellIndex = Array.FindIndex(_mesh.Cells, c => c == cell);
                var weight = IDWLocalShepard(cell.Center, position, searchRadius);
                result += _values[cellIndex]*weight;
                weigths += weight;
            }

            return result/weigths;
        }

        private float IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, float searchRadius)
        {
            var d = Vector2.Distance(interpolatePoint, point);

            var a = searchRadius - d;
            if (a < 0) a = 0;
            var b = a / (searchRadius * d);
            return b * b;
        }
    }
}