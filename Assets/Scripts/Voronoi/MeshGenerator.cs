using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Voronoi
{
    public static class MeshGenerator
    {
        /// <summary>
        /// Generate Voronoi mesh from points
        /// </summary>
        /// <param name="cellCenters">Result zones count can be less</param>
        /// <param name="bounds">Size of square grid of chunks</param>
        /// <returns></returns>
        public static Mesh<T> Generate<T>(IEnumerable<Vector2> cellCenters, Bounds bounds)
        {
            var points = cellCenters.ToArray();
            var voronoi = GenerateVoronoi(points, bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z);
            var mesh = ProcessVoronoi<T>(points, voronoi, bounds);
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
        private static Mesh<T> ProcessVoronoi<T>(Vector2[] zonesCoords, GraphEdge[] edges, Bounds bounds)
        {
            
            //Create cells
            //var result = new Mesh<T>(bounds);

            /*
            for (var i = 0; i < zonesCoords.Length; i++)
            {
                result.AddCell(zonesCoords[i],
                    edges.Where(e => e.site1 == i || e.site2 == i)
                        .Select(e => new Tuple<Vector2, Vector2>(new Vector2((float) e.x1, (float) e.y1),
                            new Vector2((float) e.x2, (float) e.y2))));
            }

            //todo implement edges addition
            */
            //return result;
            return null;
        }
    }
}
