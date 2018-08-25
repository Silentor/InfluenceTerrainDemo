using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class Poisson
    {
        /// <summary>
        /// Generate zone points using Poisson sampling
        /// </summary>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        public static Vector2[] GeneratePoints(Bounds landBounds, Vector2 density)
        {
            const int maxPointsCount = 1000;
            const int tryCount = 10;

            var checkedPoints = new List<Vector2>();
            var uncheckedPoints = new List<Vector2>();

            //Generate start point at center
            var center = (landBounds.min + landBounds.max) / 2;
            var startPoint = center;

            uncheckedPoints.Add(startPoint);

            //Generate point around first unchecked
            while (uncheckedPoints.Any())
            {
                var processedPoint = uncheckedPoints.Last();
                uncheckedPoints.RemoveAt(uncheckedPoints.Count - 1);

                for (int i = 0; i < tryCount; i++)
                {
                    var r = UnityEngine.Random.Range(density.x, density.y);
                    var a = UnityEngine.Random.Range(0, 2 * Mathf.PI);
                    var newPoint = processedPoint + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));

                    if (landBounds.Contains(newPoint))
                    {
                        if (checkedPoints.TrueForAll(p => Vector2.SqrMagnitude(p - newPoint) > density.x * density.x)
                            && uncheckedPoints.TrueForAll(p =>
                                Vector2.SqrMagnitude(p - newPoint) > density.x * density.x))
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
