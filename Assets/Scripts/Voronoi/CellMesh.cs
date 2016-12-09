using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Tools.SimpleJSON;
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
        public Neighbors GetOuterRings([NotNull] Cell centerCell)
        {
            if (centerCell == null) throw new ArgumentNullException("centerCell");
            Assert.IsTrue(Cells.Contains(centerCell));

            return new Neighbors(this, centerCell);
        }

        public Neighbors GetInnerRings([NotNull] Cell[] cluster)
        {
            if (cluster == null) throw new ArgumentNullException("cluster");
            Assert.IsTrue(cluster.All(c => Cells.Contains(c)));

            return new Neighbors(this, cluster);
        }

        /// <summary>
        /// Enumerate cell neighbors in breath-first manner
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public IEnumerable<Cell> GetNeighbors([NotNull] Cell center)
        {
            if (center == null) throw new ArgumentNullException("center");

            var outer = GetOuterRings(center);
            for (int i = 1; i < 10; i++)
            {
                var neighbors = outer.GetNeighbors(i);
                if(neighbors.Any())
                    foreach (var neighbor in neighbors)
                    {
                        yield return neighbor;
                    }
                else yield break;
            }
        }

        public JSONNode ToJSON()
        {
            var json = new JSONClass();
            json["bounds"].SetBounds(Bounds);
            json["cells"].SetArray(Cells, c => c.ToJSON());
            var cellsNeighbors = new JSONArray();
            foreach (var cell in Cells)
            {
                var neighbors = new JSONData(string.Join(" ", cell.Neighbors.Select(c => c.Id.ToString()).ToArray()));
                cellsNeighbors[cell.Id] = neighbors;
            }
            json["neighbors"] = cellsNeighbors;

            return json;
        }

        public static CellMesh FromJSON(JSONNode node)
        {
            var bounds = node["bounds"].GetBounds();
            var cells = node["cells"].GetArray(Cell.FromJSON);

            //Parse cells neighbors
            var neighborsJson = node["neighbors"];
            var neighbors = new Cell[neighborsJson.Count][];
            for (int i = 0; i < neighborsJson.Count; i++)
            {
                var cellNeighString = neighborsJson[i].Value;
                var cellNeighs = cellNeighString.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => cells[int.Parse(s)]).ToArray();
                neighbors[i] = cellNeighs;
            }

            var result = new CellMesh(cells, bounds);
            for (int i = 0; i < cells.Length; i++)
                cells[i].Init(neighbors[i]);

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
        /// For searching inner or outer neighbors
        /// </summary>
        public class Neighbors
        {
            private readonly CellMesh _mesh;
            private readonly List<List<Cell>> _neighbors = new List<List<Cell>>();

            /// <summary>
            /// For calculate outer rings of cemter cell
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="center"></param>
            public Neighbors(CellMesh mesh, Cell center)
            {
                _mesh = mesh;
                _neighbors.Add(new List<Cell> {center});

                Assert.IsTrue(_mesh.Cells.Contains(center));
            }

            /// <summary>
            /// For calculate inner rings of cluster
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="cluster"></param>
            public Neighbors(CellMesh mesh, Cell[] cluster)
            {
                _mesh = mesh;

                //Cells thats has neighbors outside cluster
                var outerRing = new List<Cell>();
                foreach (var cell in cluster)
                {
                    if(cell.Neighbors.Any(c => !cluster.Contains(c)))
                        outerRing.Add(cell);
                }
                _neighbors.Add(outerRing);
            }

            public IEnumerable<Cell> GetNeighbors(int rank)
            {
                if(rank == 0)
                    return _neighbors[0];

                if (rank < _neighbors.Count)
                    return _neighbors[rank];

                //Calculate neighbors
                if (rank - 1 < _neighbors.Count)
                {
                    var processedCellsIndex = Math.Max(0, rank - 2);
                    var result = GetNeighbors(_neighbors[rank - 1], _neighbors[processedCellsIndex]);
                    _neighbors.Add(result);
                    return result;
                }
                else
                    return GetNeighbors(rank - 1);
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
                        if (!result.Contains(neigh2) && !prevNeighbors.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
                            result.Add(neigh2);
                    }
                }

                return result;
            }
        }
    }
}
