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
    public class BarycentricInterpolator
    {
        public BarycentricInterpolator([NotNull] CellMesh.Submesh mesh, [NotNull] double[] values)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (values == null) throw new ArgumentNullException("values");
            if (mesh.Cells.Length != values.Length) throw new ArgumentException("mesh length != values length");

            _mesh = mesh;
            _values = values;
        }

        public double GetValue(Vector2 position)
        {
            //Get point's triangle
            var nearestCell = _mesh.GetCellFor(position);

            //No sense to calculate barycentric if there is no triangle
            var neighbors = GetNeighborsFor(nearestCell);
            if (neighbors.Length < 2)
                return 0;

            //To support extrapolation
            var besti = 0;
            var bestError = float.MaxValue;
            var bestBary = Vector3.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                var baryCoords = Intersections.Barycentric2DCoords(position, nearestCell.Center, neighbors[i].Center, neighbors[(i + 1) % neighbors.Length].Center);
                if (baryCoords.x > 0 && baryCoords.y > 0 && baryCoords.z > 0)
                {
                    var valueA = _values[Array.IndexOf(_mesh.Cells, nearestCell)];      //todo binary search or store value in cell
                    var valueB = _values[Array.IndexOf(_mesh.Cells, neighbors[i])];
                    var valueC = _values[Array.IndexOf(_mesh.Cells, neighbors[(i + 1) % neighbors.Length])];

                    return baryCoords.x * valueA + baryCoords.y * valueB + baryCoords.z * valueC;
                }

                var error = baryCoords - new Vector3(0.333333333f, 0.3333333333f, 0.333333333f);
                var error2 = error.x * error.x + error.y * error.y + error.z * error.z;
                if (error2 < bestError)
                {
                    bestError = error2;
                    besti = i;
                    bestBary = baryCoords;
                }

            }

            //Extrapolation
            var valueA2 = _values[Array.IndexOf(_mesh.Cells, nearestCell)];      //todo binary search or store value in cell
            var valueB2 = _values[Array.IndexOf(_mesh.Cells, neighbors[besti])];
            var valueC2 = _values[Array.IndexOf(_mesh.Cells, neighbors[(besti + 1) % neighbors.Length])];

            return bestBary.x * valueA2 + bestBary.y * valueB2 + bestBary.z * valueC2;

            //Instead of extrapolation support nearestCell drift


            //Something going wrong, point is not in nearestCell? or triangle is degenerated
            return 0;
        }

        public static Vector2[] GetTriangle(Vector2 position, CellMesh mesh)
        {
            var nearestCell = mesh.GetCellFor(position);
            var neighbors = nearestCell.Edges.Select(e => e.Neighbor).ToArray();
            if (neighbors.Length < 2)
                return null;

            if (Event.current.shift && Event.current.type == EventType.repaint)
            {
                //DEBUG mode

                for (int i = 0; i < neighbors.Length; i++)
                {
                    var baryCoords = Intersections.Barycentric2DCoords(position, nearestCell.Center, neighbors[i].Center,
                        neighbors[(i + 1) % neighbors.Length].Center);
                    var res = baryCoords.x >= 0 && baryCoords.y >= 0 && baryCoords.z >= 0;
                    if (res)
                    {
                        Debug.LogFormat("Bary {0}, {1}, {2}, {3}, {4}, {5} result true", 
                            baryCoords.x, baryCoords.y, baryCoords.z,
                            nearestCell.Id, neighbors[i].Id, neighbors[(i + 1) % neighbors.Length].Id
                            );
                    }
                    else
                        Debug.LogFormat("Bary {0}, {1}, {2}, {3}, {4}, {5} result false", 
                            baryCoords.x, baryCoords.y, baryCoords.z,
                            nearestCell.Id, neighbors[i].Id, neighbors[(i + 1) % neighbors.Length].Id
                            );
                }

            }

            var besti = 0;
            var bestError = float.MaxValue;
            for (int i = 0; i < neighbors.Length; i++)
            {
                var baryCoords = Intersections.Barycentric2DCoords(position, nearestCell.Center, neighbors[i].Center,
                    neighbors[(i + 1)%neighbors.Length].Center);
                if (baryCoords.x >= 0 && baryCoords.y >= 0 && baryCoords.z >= 0)
                    return new Vector2[]
                        {nearestCell.Center, neighbors[i].Center, neighbors[(i + 1)%neighbors.Length].Center};

                var error = baryCoords - new Vector3(0.333333333f, 0.3333333333f, 0.333333333f);
                var error2 = error.x*error.x + error.y*error.y + error.z*error.z;
                if (error2 < bestError)
                {
                    bestError = error2;
                    besti = i;
                }

            }
            //Try to calculate best pssible triangle
            return new Vector2[]
                        {nearestCell.Center, neighbors[besti].Center, neighbors[(besti + 1)%neighbors.Length].Center};

            return null;
        }

        private readonly CellMesh.Submesh _mesh;
        private readonly double[] _values;
        private readonly Dictionary<Cell, Cell[]> _neighbors = new Dictionary<Cell, Cell[]>();

        private Cell[] GetNeighborsFor(Cell cell)
        {
            Cell[] result;
            if (!_neighbors.TryGetValue(cell, out result))
            {
                result = _mesh.FloodFill(cell).GetNeighbors(1).ToArray();
                Array.Sort(result, (x, y) => VectorExtensions.ClockWiseComparer(x.Center, y.Center, cell.Center));
                _neighbors[cell] = result;
            }

            return result;
        }


    }
}
