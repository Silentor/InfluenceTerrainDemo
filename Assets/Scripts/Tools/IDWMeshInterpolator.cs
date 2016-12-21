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
    /// Interpolate float value on cellmesh
    /// </summary>
    public class IDWMeshInterpolator
    {
        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;
        private const int LocalPoints = 7;

        public IDWMeshInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
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

            //Spatial optimization
            var center = _mesh.GetCellFor(position);
            var nearestCells = new List<Cell>(center.Neighbors.Length + center.Neighbors2.Length + 1);
            nearestCells.Add(center);
            var floodFill = _mesh.GetFloodFill(center);
            nearestCells.AddRange(floodFill.GetNeighbors(1));
            var neighborsRank2 = floodFill.GetNeighbors(2);
            nearestCells.AddRange(neighborsRank2);

            //Try to flood fill more points if not enough
            if (nearestCells.Count < LocalPoints && neighborsRank2.Any())
            {
                var step = 3;
                while (nearestCells.Count < LocalPoints)
                {
                    var nextStepFloodFill = floodFill.GetNeighbors(step++);
                    if (!nextStepFloodFill.Any())
                        break;
                    else
                        nearestCells.AddRange(nextStepFloodFill);
                }
            }

            nearestCells.Sort(new Cell.DistanceComparer(position));

            var nearestCellsCount = Math.Min(nearestCells.Count, LocalPoints);
            var searchRadius = Vector2.Distance(nearestCells[nearestCellsCount - 1].Center, position);

            //Sum up zones influence
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = nearestCells[i];
                var cellIndex = Array.FindIndex(_mesh.Cells, c => c == cell);
                var weight = IDWLocalShepard(cell.Center, position, searchRadius);
                result += _values[cellIndex] * weight;
                weigths += weight;
            }

            return result/weigths;
        }

        private double IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            double d = Vector2.Distance(interpolatePoint, point);

            var a = searchRadius - d;
            if (a < 0) a = 0;
            var b = a / (searchRadius * d);
            return b * b;
        }
    }
}