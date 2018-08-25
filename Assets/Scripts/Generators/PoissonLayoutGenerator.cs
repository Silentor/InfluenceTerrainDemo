using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    /// <summary>
    /// Distribute points by Poisson distribution but do not set zone types
    /// </summary>
    public abstract class PoissonLayoutGenerator : LayoutGenerator
    {
        protected PoissonLayoutGenerator(LandSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected Vector2[] GeneratePoints(Bounds2i landBounds, Vector2 density)
        {
            const int maxPointsCount = 1000;
            var checkedPoints = new List<Vector2>();
            var uncheckedPoints = new List<Vector2>();

            //Generate start point (near bounds center)
            var center = (landBounds.Max + landBounds.Min) / 2;
            var zoneCenterX = Random.Range(center.X - density.y / 2, center.X + density.y / 2);
            var zoneCenterY = Random.Range(center.Z - density.y / 2, center.Z + density.y / 2);
            var startPoint = new Vector2(zoneCenterX, zoneCenterY);

            uncheckedPoints.Add(startPoint);

            //Generate point around first unchecked
            while (uncheckedPoints.Any())
            {
                var processedPoint = uncheckedPoints.First();
                uncheckedPoints.RemoveAt(0);

                for (int i = 0; i < 10; i++)
                {
                    var r = Random.Range(density.x + 0.1f, density.y);
                    var a = Random.Range(0, 2 * Mathf.PI);
                    var newPoint = processedPoint + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));

                    if (landBounds.Contains((Vector2i)newPoint))
                    {
                        if (checkedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x)
                            && uncheckedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x))
                            uncheckedPoints.Add(newPoint);
                    }
                }

                checkedPoints.Add(processedPoint);
                if (checkedPoints.Count >= maxPointsCount)
                    break;
            }

            return checkedPoints.ToArray();
        }
    }
}