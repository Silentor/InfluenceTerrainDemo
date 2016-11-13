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
        protected PoissonLayoutGenerator(ILandSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected override Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density)
        {
            var checkedPoints = new List<Vector2>();
            var uncheckedPoints = new List<Vector2>();

            //Generate start point
            var zoneCenterX = Random.Range((float)landBounds.Min.X, landBounds.Max.X);
            var zoneCenterY = Random.Range((float)landBounds.Min.Z, landBounds.Max.Z);
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
                if (checkedPoints.Count >= count)
                    break;
            }

            return checkedPoints.ToArray();
        }
    }
}