using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Voronoi
{
    public static class CellMeshGenerator
    {
        /// <summary>
        /// Generate full cell mesh
        /// </summary>
        /// <param name="cellCenters">Result zones count can be less</param>
        /// <param name="bounds">Size of square grid of chunks</param>
        /// <returns></returns>
        public static CellMesh Generate(IEnumerable<Vector2> cellCenters, Bounds bounds)
        {
            var points = cellCenters.ToArray();
            var voronoi = GenerateVoronoi(points, bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z);
            var mesh = ProcessVoronoi(points, voronoi, bounds);
            return mesh;
        }

        /// <summary>
        /// Generate Voronoi diagram by points
        /// </summary>
        /// <param name="cellCenters"></param>
        /// <returns></returns>
        private static GraphEdge[] GenerateVoronoi(Vector2[] cellCenters, float minX, float maxX, float minY, float maxY)
        {
            var voronoi = new Voronoi(0.1);

            //Prepare data
            var xValues = new double[cellCenters.Length];
            var yValues = new double[cellCenters.Length];

            for (int i = 0; i < xValues.Length; i++)
            {
                xValues[i] = cellCenters[i].x;
                yValues[i] = cellCenters[i].y;
            }

            //Calc Voronoi
            var result = voronoi.generateVoronoi(xValues, yValues, minX, maxX, minY, maxY);

            return result.ToArray();
        }

        /// <summary>
        /// Calculate cell-mesh from list of edges of Voronoi graph
        /// </summary>
        /// <param name="zonesCoords">Coords of center of every cell</param>
        /// <param name="edges">All edges of Voronoi diagram</param>
        /// <returns>Mesh of cells</returns>
        private static CellMesh ProcessVoronoi(Vector2[] zonesCoords, GraphEdge[] edges, Bounds bounds)
        {
            //Clear duplicate edges (some open cell cases)
            edges = edges.Distinct(GraphEdgeComparer.Default).ToArray();

            //Clear zero length edges (some open cell cases)
            edges = edges.Where(e => e.x1 != e.x2 && e.y1 != e.y2).ToArray();

            //Prepare temp collection for sorting cell edges clockwise
            var cellsEdges = new List<GraphEdge>[zonesCoords.Length];
            for (var i = 0; i < cellsEdges.Length; i++)
                cellsEdges[i] = new List<GraphEdge>();

            //Fill edge sort collection
            foreach (var graphEdge in edges)
            {
                cellsEdges[graphEdge.site1].Add(graphEdge);
                cellsEdges[graphEdge.site2].Add(graphEdge);
            }

            var isCellsClosed = new bool[zonesCoords.Length];

            //For every cell: rotate edges clockwise, sort edges clockwise, check if cell is closed
            for (int cellIndex = 0; cellIndex < cellsEdges.Length; cellIndex++)
            {
                var cellEdges = cellsEdges[cellIndex];
                for (var edgeIndex = 0; edgeIndex < cellEdges.Count; edgeIndex++)
                {
                    var edge = cellEdges[edgeIndex];
                    if (VectorExtensions.ClockWiseComparer(new Vector2((float)edge.x1, (float)edge.y1),
                            new Vector2((float)edge.x2, (float)edge.y2), zonesCoords[cellIndex]) > 0)
                    {
                        //Inverse direction of edge
                        cellEdges[edgeIndex] = new GraphEdge() { site1 = edge.site1, site2 = edge.site2, x1 = edge.x2, y1 = edge.y2, x2 = edge.x1, y2 = edge.y1 };
                    }
                }

                //Sort all edges clockwise
                var zoneCenter = zonesCoords[cellIndex];
                cellEdges.Sort((e1, e2) => VectorExtensions.ClockWiseComparer(new Vector2((float)e1.x1, (float)e1.y1), new Vector2((float)e2.x1, (float)e2.y1), zoneCenter)
                );

                var isCellClosed = false;
                //So, we get edges in clockwise order, check if cell is closed
                if (cellEdges.Count > 2)
                {
                    isCellClosed = true;
                    for (int i = 0; i < cellEdges.Count; i++)
                    {
                        var edge = cellEdges[i];
                        var nextEdge = cellEdges[(i + 1) % cellEdges.Count];
                        if (Math.Abs(edge.x2 - nextEdge.x1) > 0.1 || Math.Abs(edge.y2 - nextEdge.y1) > 0.1)
                        {
                            isCellClosed = false;
                            break;
                        }
                    }
                }

                isCellsClosed[cellIndex] = isCellClosed;

                Assert.IsTrue(!isCellClosed || cellEdges.Count >= 3, "Closed sell with < 3 edges!!");
            }

            //Fill result cellmesh
            var resultCells = new Cell[zonesCoords.Length];

            //Create cells
            for (int i = 0; i < resultCells.Length; i++)
            {
                var vertices = isCellsClosed[i]
                    ? cellsEdges[i].Select(e => new Vector2((float) e.x1, (float) e.y1)).ToArray()
                    : cellsEdges[i].SelectMany(e => new[]
                    {
                        new Vector2((float) e.x1, (float) e.y1),
                        new Vector2((float) e.x2, (float) e.y2)
                    }).Distinct().ToArray();

                var cellEdges = cellsEdges[i].Select(ce => new Cell.Edge(
                    new Vector2((float)ce.x1, (float)ce.y1), new Vector2((float)ce.x2, (float)ce.y2),
                    ce.site1 == i ? ce.site2 : ce.site1)).ToArray();

                resultCells[i] = new Cell(i, zonesCoords[i], isCellsClosed[i], vertices, cellEdges, bounds);
            }

            var result = new CellMesh(resultCells, bounds);

            //Fill cells references
            for (int i = 0; i < resultCells.Length; i++)
            {
                var cell = result[i];
                cell.Init(result);
            }

            return result;
        }

        private static bool IsZoneAllowed(bool[,] chunkGrid, Vector2i newZoneCoord, int minDistance)
        {
            var gridSize = chunkGrid.GetUpperBound(0);

            for (int x = newZoneCoord.X - minDistance; x <= newZoneCoord.X + minDistance; x++)
            {
                for (int z = newZoneCoord.Z - minDistance; z <= newZoneCoord.Z + minDistance; z++)
                {
                    if (x < 0 || z < 0 || x >= gridSize || z >= gridSize)
                        continue;

                    if (chunkGrid[x, z])
                        return false;
                }
            }

            return true;
        }

        private class GraphEdgeComparer : IEqualityComparer<GraphEdge>
        {
            public static readonly GraphEdgeComparer Default = new GraphEdgeComparer();

            public bool Equals(GraphEdge x, GraphEdge y)
            {
                return Math.Abs(x.x1 - y.x1) <= 0.001 &&
                       Math.Abs(x.x2 - y.x2) <= 0.001 &&
                       Math.Abs(x.y1 - y.y1) <= 0.001 &&
                       Math.Abs(x.y2 - y.y2) <= 0.001;
            }

            public int GetHashCode(GraphEdge obj)
            {
                return obj.x1.GetHashCode() ^ obj.x2.GetHashCode() ^ obj.y1.GetHashCode() ^ obj.y2.GetHashCode();
            }
        }
    }
}
