using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;


namespace TerrainDemo.Tools
{
    /// <summary>
    /// Barycentric interpolation between cell centers. Use 3 nearest cells
    /// </summary>
    public class ShepardInterpolator
    {
        public ShepardInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (values == null) throw new ArgumentNullException("values");
            if (mesh.Cells.Length != values.Length) throw new ArgumentException("mesh length != values length");

            _mesh = mesh;
            _values = values;

            var points = new double[values.Length, 3];
            for (int i = 0; i < values.Length; i++)
            {
                points[i, 0] = mesh[i].Center.x;
                points[i, 1] = mesh[i].Center.y;
                points[i, 2] = values[i];
            }

            _model = new alglib.idwinterpolant();
            alglib.idwbuildmodifiedshepard(points, values.Length, 2, 2, 5, 7, out _model);
        }

        public double GetValue(Vector2 position)
        {
            return alglib.idwcalc(_model, new double[]{ position.x, position.y });
        }

        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;
        private readonly Dictionary<Cell, Cell[]> _neighbors = new Dictionary<Cell, Cell[]>();
        private readonly alglib.idwinterpolant _model;
    }
}
