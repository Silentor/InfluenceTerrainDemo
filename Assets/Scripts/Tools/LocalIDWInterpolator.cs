using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Interpolate float value on cellmesh. Use constant neighbors count <see cref="LocalPoints"/> as a data points
    /// </summary>
    public class LocalIDWInterpolator
    {
        public LocalIDWInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (values == null) throw new ArgumentNullException("values");
            if(mesh.Cells.Length != values.Length) throw new ArgumentException("mesh length != values length");
            if(mesh.Cells.Length < LocalPoints) Debug.LogWarningFormat("Submesh is too small {0} for this IDWMeshInterpolator {1}",
                mesh.Cells.Length, LocalPoints);

            _mesh = mesh;
            _values = values;
        }

        public double GetValue(Vector2 position)
        {
            var result = 0.0;
            var weigths = 0.0;

            //Get near cells for local interpolation field
            var center = _mesh.GetCellFor(position);

            var nearestCells = GetLocalsFor(center);
            Array.Sort(nearestCells, new Cell.DistanceComparer(position));

            var nearestCellsCount = Math.Min(nearestCells.Length, LocalPoints);
            var searchRadius = Vector2.Distance(nearestCells[nearestCellsCount - 1].Center, position);

            //Sum up zones influence
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = nearestCells[i];
                var cellIndex = Array.IndexOf(_mesh.Cells, cell);               //todo cache values with nearest cells in GetLocalcsFor()
                var weight = IDWLocalShepard(cell.Center, position, searchRadius);
                result += _values[cellIndex] * weight;
                weigths += weight;
            }

            return result/weigths;
        }

        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;
        private const int LocalPoints = 7;
        private readonly Dictionary<Cell, Cell[]> _neighbors = new Dictionary<Cell, Cell[]>();

        private double IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            double d = Vector2.Distance(interpolatePoint, point);

            var a = searchRadius - d;
            if (a < 0) a = 0;
            var b = a / (searchRadius * d);
            return b * b;
        }

        private Cell[] GetLocalsFor(Cell cell)
        {
            Cell[] result;
            if (!_neighbors.TryGetValue(cell, out result))
            {
                var nearestCells = new List<Cell>(cell.Neighbors.Length + cell.Neighbors2.Length + 1);
                nearestCells.Add(cell);
                var floodFill = _mesh.GetFloodFill(cell);                         
                nearestCells.AddRange(floodFill.GetNeighbors(1));
                var neighborsRank2 = floodFill.GetNeighbors(2);
                nearestCells.AddRange(neighborsRank2);

                result = nearestCells.ToArray();
                _neighbors[cell] = result;
            }

            return result;
        }
    }

}