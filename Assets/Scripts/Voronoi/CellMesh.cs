using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Voronoi
{
    public class CellMesh
    {
        public Bounds Bounds { get; private set; }
        public readonly Cell[] Cells;

        public CellMesh([NotNull] Cell[] cells, Bounds bounds)
        {
            if (cells == null) throw new ArgumentNullException("cells");
            if(cells.Length == 0) throw new InvalidOperationException("Cell mesh is empty");

            Cells = cells;
            Bounds = bounds;
        }

        /// <summary>
        /// Get nearest (containing) cell for given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Cell GetCellFor(Vector2 position)
        {
            var minDistance = float.MaxValue;
            Cell result = null;

            for (int i = 0; i < Cells.Length; i++)
            {
                var cellDistance = Vector2.SqrMagnitude(position - Cells[i].Center);
                if (cellDistance < minDistance)
                {
                    minDistance = cellDistance;
                    result = Cells[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Get nearest (containing) cell for given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Cell GetCellFor(Vector2 position, [NotNull] Predicate<Cell> condition)
        {
            if (condition == null) throw new ArgumentNullException("condition");

            var minDistance = float.MaxValue;
            Cell result = null;

            for (int i = 0; i < Cells.Length; i++)
            {
                var cellDistance = Vector2.SqrMagnitude(position - Cells[i].Center);
                if (cellDistance < minDistance && condition(Cells[i]))
                {
                    minDistance = cellDistance;
                    result = Cells[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Get cells containing in given circle (visible cells)
        /// Naive implementation, for optimization look at http://www.bitlush.com/posts/circle-vs-polygon-collision-detection-in-c-sharp
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public Cell[] GetCellsFor(Vector2 position, float radius)
        {
            //Containig cell certanly in results
            var sqrRadius = radius*radius;
            var startCell = GetCellFor(position);
            var result = new List<Cell> {startCell};

            //Check nearest cells
            foreach (var neighCell in startCell.Neighbors)
            {
                if(CheckCell(neighCell, position, sqrRadius))
                    result.Add(neighCell);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Get direct neighbors, neighbors of neighbors, etc
        /// </summary>
        /// <param name="centerCell"></param>
        /// <returns></returns>
        public IEnumerable<Cell[]> GetNeighbours([NotNull] Cell centerCell)
        {
            if (centerCell == null) throw new ArgumentNullException("centerCell");
            Assert.IsTrue(Cells.Contains(centerCell));

            var processed = new List<Cell> {centerCell};
            var neighs = new List<Cell> {centerCell};
            var neighs2 = new List<Cell>();

            while (true)
            {
                foreach (var neigh1 in neighs)
                {
                    foreach (var neigh2 in neigh1.Neighbors)
                    {
                        if (!processed.Contains(neigh2) && !neighs2.Contains(neigh2))
                            neighs2.Add(neigh2);
                    }
                }

                if(neighs2.Count == 0)
                    yield break;

                processed.AddRange(neighs);
                neighs.Clear();
                neighs.AddRange(neighs2);
                neighs2.Clear();

                yield return neighs.ToArray();
            }
        }

        private static bool CheckCell(Cell cell, Vector2 position, float sqrRadius)
        {
            //Fast check - center
            if (Vector2.SqrMagnitude(cell.Center - position) <= sqrRadius)
                return true;

            //Fast check - vertices
            for (int i = 0; i < cell.Vertices.Length; i++)
            {
                var neighVert = cell.Vertices[i];
                if (Vector2.SqrMagnitude(neighVert - position) <= sqrRadius)
                {
                    return true;
                }
            }

            //todo check edges

            return false;
        }
    }
}
