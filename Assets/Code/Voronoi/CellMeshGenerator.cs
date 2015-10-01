using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using Debug = UnityEngine.Debug;
using Random = System.Random;
//using OpenTK;

namespace Assets.Code.Voronoi
{
    public class CellMeshGenerator
    {
        /// <summary>
        /// Generate full cell mesh
        /// </summary>
        /// <param name="desiredZonesCount">Result zones count can be less</param>
        /// <param name="gridSize">Size of square grid of chunks</param>
        /// <returns></returns>
        public static Cell[] Generate(int count, Bounds landBounds, int seed, float minDistance = 32)
        {
            var points = GeneratePoints(count, landBounds, seed, minDistance);
            var voronoi = GenerateVoronoi(points, landBounds);
            var mesh = ProcessVoronoi(points, voronoi);
            return mesh;
        }

        /// <summary>
        /// Generate random centers of cells
        /// </summary>
        /// <param name="count"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static Vector2[] GeneratePoints(int count, Bounds landBounds, int seed, float minDistance = 32)
        {
            //Prepare input data
            var rnd = new Random(seed);
            var infiniteLoopChecker = 0;
            //var chunksGrid = new bool[gridSize * 2, gridSize * 2];

            //Generate zones center coords, check that only one zone occupies one chunk
            var zonesCoords = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var zoneCenterX = rnd.Next((int)landBounds.min.x, (int)landBounds.max.x);
                var zoneCenterY = rnd.Next((int)landBounds.min.y, (int)landBounds.max.y);
                var newCenter = new Vector2(zoneCenterX, zoneCenterY);
                if(zonesCoords.All(zc => Vector2.SqrMagnitude(zc - newCenter) > minDistance * minDistance))
                //if (IsZoneAllowed(chunksGrid, new Vector2i(zoneCenterX / 16, zoneCenterY / 16), minDistance))
                {
                    //chunksGrid[zoneCenterX / 16, zoneCenterY / 16] = true;
                    zonesCoords.Add(new Vector2 { x = zoneCenterX, y = zoneCenterY });
                }
                else
                {
                    if (infiniteLoopChecker++ < 100)
                        i--;
                    else
                        break;
                }
            }

            return zonesCoords.ToArray();
        }

        /// <summary>
        /// Generate Voronoi diagram by points
        /// </summary>
        /// <param name="cellCenters"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static List<GraphEdge> GenerateVoronoi(Vector2[] cellCenters, Bounds bounds)
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
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = voronoi.generateVoronoi(xValues, yValues, bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y);
            timer.Stop();

            UnityEngine.Debug.Log(string.Format("Voronoi diagram for {0} zones calc time {1} msec", cellCenters.Length, timer.ElapsedMilliseconds));

            return result;
        }

        /// <summary>
        /// Calculate cell-mesh from list of edges of Voronoi graph
        /// </summary>
        /// <param name="zonesCoords">Coords of center of every cell</param>
        /// <param name="edges">All edges of Voronoi diagram</param>
        /// <returns>Mesh of cells</returns>
        public static Cell[] ProcessVoronoi(Vector2[] zonesCoords, List<GraphEdge> edges)
        {
            var timer = Stopwatch.StartNew();

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
                    if (ClockWiseComparer(new Vector2((float) edge.x1, (float) edge.y1),
                            new Vector2((float) edge.x2, (float) edge.y2), zonesCoords[cellIndex]) > 0)
                    {
                        //Inverse direction of edge
                        cellEdges[edgeIndex] = new GraphEdge() { site1 = edge.site1, site2 = edge.site2, x1 = edge.x2, y1 = edge.y2, x2 = edge.x1, y2 = edge.y1 };
                    }
                }

                //Sort all edges clockwise
                var zoneCenter = zonesCoords[cellIndex];
                cellEdges.Sort((e1, e2) =>
                    ClockWiseComparer(new Vector2((float)e1.x1, (float)e1.y1), new Vector2((float)e2.x1, (float)e2.y1), zoneCenter)
                );

                var isCellClosed = false;
                //So, we get edges in clockwise order, check if cell is closed
                if(cellEdges.Count > 2)
                {
                    isCellClosed = true;
                    for (int i = 0; i < cellEdges.Count; i++)
                    {
                        var edge = cellEdges[i];
                        var nextEdge = cellEdges[(i + 1)%cellEdges.Count];
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
            var result = new Cell[zonesCoords.Length];

            //Create cells
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Cell
                {
                    Id = i,
                    Center = zonesCoords[i],
                    IsClosed = isCellsClosed[i],
                    Edges = (cellsEdges[i].Select(e => new Cell.Edge(
                        new Vector2((float)e.x1, (float)e.y1), new Vector2((float)e.x2, (float)e.y2)))).ToArray(),
                    Vertices = cellsEdges[i].Select(e => new Vector2((float)e.x1, (float)e.y1)).ToArray()
                };
            }

            //Fill cells references
            for (int i = 0; i < result.Length; i++)
            {
                var cell = result[i];
                var cellEdges = cellsEdges[i];
                cell.Neighbors = cellEdges.Select(e => e.site1 == cell.Id ? result[e.site2] : result[e.site1]).ToArray();
            }

            timer.Stop();
            Debug.Log(string.Format("Cellmesh calculated for {0} msec", timer.ElapsedMilliseconds));

            return result;
        }

        /// <summary>
        /// Compare points in clockwise order (relatively center)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="center"></param>
        /// <returns>-1 = a -> b clockwise; 1 = b -> a clockwise; 0 = point a, b are same</returns>
        private static int ClockWiseComparer(Vector2 a, Vector2 b, Vector2 center)
        {
            http://stackoverflow.com/questions/6989100/sort-points-in-clockwise-order

            //Some buggy optimization, consider perfomance usefulness
            //if (a.X - center.X >= 0 && b.X - center.X < 0)
            //    return true;
            //if (a.X - center.X < 0 && b.X - center.X >= 0)
            //    return false;
            //if (Math.Abs(a.X - center.X) < float.Epsilon && Math.Abs(b.X - center.X) < float.Epsilon)
            //{
            //    if (a.Y - center.Y >= 0 || b.Y - center.Y >= 0)
            //        return a.Y > b.Y;
            //    return b.Y > a.Y;
            //}

            if(a == center || b == center)
                throw new InvalidOperationException("Some input points match the center point");

            if (a == b)
                return 0;

            // compute the pseudoscalar product of vectors (center -> a) x (center -> b)
            var ca = center - a;
            var cb = center - b;
            var det = ca.x*cb.y - cb.x*ca.y;
            if (det > 0)
                return 1;
            if (det < 0)
                return -1;

            // points a and b are on the same line from the center
            // check which point is closer to the center
            return ca.sqrMagnitude.CompareTo(cb.sqrMagnitude);
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
    }
}
