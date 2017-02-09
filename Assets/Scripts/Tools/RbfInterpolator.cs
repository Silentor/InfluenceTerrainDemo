using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;


namespace TerrainDemo.Tools
{
    /// <summary>
    /// RBF interpolation between cell centers
    /// </summary>
    public class RbfInterpolator
    {
        public RbfInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (values == null) throw new ArgumentNullException("values");
            if (mesh.Cells.Length != values.Length) throw new ArgumentException("mesh length != values length");

            _mesh = mesh;
            _values = values;

            alglib.rbf.rbfcreate(2, 1, _model);
            var points = new double[values.Length, 3];
            for (int i = 0; i < values.Length; i++)
            {
                points[i, 0] = mesh[i].Center.x;
                points[i, 1] = mesh[i].Center.y;
                points[i, 2] = values[i];
            }
            alglib.rbf.rbfsetpoints(_model, points, values.Length);
            alglib.rbf.rbfreport rep = new alglib.rbf.rbfreport();
            alglib.rbf.rbfbuildmodel(_model, rep);
        }

        public double GetValue(Vector2 position)
        {
            return alglib.rbf.rbfcalc2(_model, position.x, position.y);
        }

        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;
        private readonly Dictionary<Cell, Cell[]> _neighbors = new Dictionary<Cell, Cell[]>();
        private readonly alglib.rbf.rbfmodel _model = new alglib.rbf.rbfmodel();
    }
}
