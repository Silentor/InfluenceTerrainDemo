using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Libs.SimpleJSON;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Voronoi
{
    public class CellMesh
    {
        public Bounds Bounds { get; private set; }
        public readonly Cell[] Cells;

        public Cell this[int index] { get { return Cells[index]; } }

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
        [Pure]
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

            if (result != null && result.IsContains(position))
                return result;
            return null;
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

            if (result != null && result.IsContains(position))
                return result;
            return null;
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
        /// Get direct neighbors, neighbors of neighbors, etc step by step
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public FloodFiller FloodFill([NotNull] Cell start, Predicate<Cell> allowFill = null)
        {
            if (start == null) throw new ArgumentNullException("start");
            Assert.IsTrue(Cells.Contains(start));

            return new FloodFiller(this, start, allowFill);
        }

        /// <summary>
        /// Get direct neighbors, neighbors of neighbors, etc step by step
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public FloodFiller FloodFill([NotNull] Cell[] start, Predicate<Cell> allowFill = null)
        {
            if (start == null) throw new ArgumentNullException("start");
            Assert.IsTrue(start.All(c => Cells.Contains(c)));

            return new FloodFiller(this, start, allowFill);
        }

        /// <summary>
        /// Enumerate cell neighbors in breath-first manner (optimized)
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public IEnumerable<Cell> GetNeighbors([NotNull] Cell center)
        {
            if (center == null) throw new ArgumentNullException("center");

            foreach (var neighbor in center.Neighbors)
                yield return neighbor;

            foreach (var neighbor in center.Neighbors2)
                yield return neighbor;

            var processed = new Cell[center.Neighbors.Length + center.Neighbors2.Length + 1];
            Array.Copy(center.Neighbors, processed, center.Neighbors.Length);
            Array.Copy(center.Neighbors2, 0, processed, center.Neighbors.Length, center.Neighbors2.Length);
            processed[center.Neighbors.Length + center.Neighbors2.Length] = center;

            var fill = FloodFill(processed);
            for (int i = 1; i < 100; i++)                //100 + 2 steps - sanity number, probably very large map can contains more
            {
                var neighbors = fill.GetNeighbors(i);
                if (neighbors.Any())
                    foreach (var neighbor in neighbors)
                        yield return neighbor;
                else yield break;
            }
        }

        /// <summary>
        /// Enumerate cell neighbors in breath-first manner
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public IEnumerable<Cell> GetNeighbors([NotNull] Cell center, Predicate<Cell> allowFill = null)
        {
            if (center == null) throw new ArgumentNullException("center");

            var fill = FloodFill(center, allowFill);
            for (int i = 1; i < 100; i++)                //100 steps - sanity number, probably very large map can contains more
            {
                var neighbors = fill.GetNeighbors(i);
                if(neighbors.Any())
                    foreach (var neighbor in neighbors)
                        yield return neighbor;
                else yield break;
            }
        }

        public JSONNode ToJSON()
        {
            var json = new JSONClass();
            json["bounds"].SetBounds(Bounds);
            json["cells"].SetArray(Cells, c => c.ToJSON());

            return json;
        }

        /// <summary>
        /// Mostly develop function. Is given a correct cluster?
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        //public bool CheckCluster(IEnumerable<Cell> cluster)
        //{
        //    if (!cluster.All(c => Cells.Contains(c)))
        //        throw new InvalidOperationException("Cells not in CellMesh");

        //    //Check duplicates
        //    if (cluster.Distinct().Count() != cluster.Count())
        //        return false;

        //    //Check links
        //    var fillCluster = 

        //}

        public static CellMesh FromJSON(JSONNode node)
        {
            var bounds = node["bounds"].GetBounds();
            var cells = node["cells"].GetArray(Cell.FromJSON);

            var result = new CellMesh(cells, bounds);

            foreach (var cell in cells)
                cell.Init(result);

            return result;
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

        /// <summary>
        /// For searching cells using flood-fill algorithm
        /// </summary>
        public class FloodFiller
        {
            private readonly CellMesh _mesh;
            private readonly Predicate<Cell> _searchFor;
            private readonly List<List<Cell>> _neighbors = new List<List<Cell>>();

            /// <summary>
            /// Create flood-fill around <see cref="start"> cell
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFiller(CellMesh mesh, Cell start, Predicate<Cell> searchFor = null)
            {
                Assert.IsTrue(mesh.Cells.Contains(start));

                _mesh = mesh;
                _searchFor = searchFor;
                _neighbors.Add(new List<Cell> {start});
            }

            /// <summary>
            /// Create flood-fill around <see cref="start"> cells
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="start"></param>
            public FloodFiller(CellMesh mesh, Cell[] start, Predicate<Cell> searchFor = null)
            {
                Assert.IsTrue(start.All(c => mesh.Cells.Contains(c)));

                _mesh = mesh;
                _searchFor = searchFor;
                var startStep = new List<Cell>();
                startStep.AddRange(start);
                _neighbors.Add(startStep);
            }


            /// <summary>
            /// Get neighbors of cell(s)
            /// </summary>
            /// <param name="step">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
            /// <returns></returns>
            public IEnumerable<Cell> GetNeighbors(int step)
            {
                if(step == 0)
                    return _neighbors[0];

                if (step < _neighbors.Count)
                    return _neighbors[step];

                //Calculate neighbors
                if (step - 1 < _neighbors.Count)
                {
                    var processedCellsIndex = Math.Max(0, step - 2);
                    var result = GetNeighbors(_neighbors[step - 1], _neighbors[processedCellsIndex]);
                    _neighbors.Add(result);
                    return result;
                }
                else
                {
                    //Calculate previous steps (because result of step=n used for step=n+1)
                    for (int i = _neighbors.Count; i < step; i++)
                        GetNeighbors(i);
                    return GetNeighbors(step);
                }
            }

            /// <summary>
            /// Get neighbors of <see cref="prevNeighbors"/> doesnt contained in <see cref="alreadyProcessed"/>
            /// </summary>
            /// <param name="prevNeighbors"></param>
            /// <param name="alreadyProcessed"></param>
            /// <returns></returns>
            private List<Cell> GetNeighbors(List<Cell> prevNeighbors, List<Cell> alreadyProcessed)
            {
                var result = new List<Cell>();
                foreach (var neigh1 in prevNeighbors)
                {
                    foreach (var neigh2 in neigh1.Neighbors)
                    {
                        if ((_searchFor == null || _searchFor(neigh2))          //check search for condition
                            && !result.Contains(neigh2) && !prevNeighbors.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
                            result.Add(neigh2);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Part of CellMesh
        /// </summary>
        public class Submesh : IEnumerable<Cell>                        //todo consider make CellMesh and Submesh a common parent
        {
            private readonly CellMesh _mesh;
            public readonly Cell[] Cells;
            private Cell[] _borderCells;

            public Cell this[int index] { get { return Cells[index]; } }

            public Submesh(CellMesh mesh, Cell[] cells)
            {
                if(cells.All(c => mesh.Cells.Contains(c)) == false)
                    throw new InvalidOperationException("Submesh is not part of mesh");

                _mesh = mesh;
                Cells = cells;
                Array.Sort(Cells, Cell.IdIncComparer);
            }

            public bool Contains(Cell cell)
            {
                var result = Array.BinarySearch(Cells, cell, Cell.IdIncComparer);
                return result >= 0;
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

                for (var i = 0; i < Cells.Length; i++)
                {
                    var cellDistance = Vector2.SqrMagnitude(position - Cells[i].Center);
                    if (cellDistance < minDistance)
                    {
                        minDistance = cellDistance;
                        result = Cells[i];
                    }
                }

                if (result != null && result.IsContains(position))
                    return result;
                return null;
            }

            /// <summary>
            /// Get cells that has neighbors outside submesh
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Cell> GetBorderCells()
            {
                if(_borderCells == null)
                    _borderCells = Cells.Where(c => !c.Neighbors.All(Contains)).ToArray();
                return _borderCells;
            }

            /// <summary>
            /// All cells that's not border
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Cell> GetInnerCells()
            {
                return Cells.Where(c => c.Neighbors.All(Contains));
            }

            public FloodFiller FloodFill([NotNull] Cell[] start, Predicate<Cell> searchFor = null)
            {
                if (start == null) throw new ArgumentNullException("start");
                Assert.IsTrue(start.All(c => Cells.Contains(c)));

                if(searchFor != null)
                    return _mesh.FloodFill(start, cell => Contains(cell) && searchFor(cell));
                else
                    return _mesh.FloodFill(start, Contains);
            }

            public FloodFiller FloodFill([NotNull] Cell start, Predicate<Cell> searchFor = null)
            {
                if (start == null) throw new ArgumentNullException("start");
                Assert.IsTrue(Cells.Contains(start));

                if (searchFor != null)
                    return _mesh.FloodFill(start, cell => Contains(cell) && searchFor(cell));
                else
                    return _mesh.FloodFill(start, Contains);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<Cell> GetEnumerator()
            {
                return ((IEnumerable<Cell>)Cells).GetEnumerator();
            }
        }
    }
}
