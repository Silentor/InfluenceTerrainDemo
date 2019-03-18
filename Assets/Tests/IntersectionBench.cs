using System.Collections;
using System.Diagnostics;
using TerrainDemo.Tools;
using UnityEngine;
using Vector3 = OpenTK.Vector3;
using Ray = TerrainDemo.Spatial.Ray;


namespace TerrainDemo.Tests
{
    public class IntersectionBench : MonoBehaviour
    {
        private Ray r;
        private Vector3 v1, v2, v3;

        void Start()
        {
            v1 = new Vector3(-3.2f, -2 , 5 );
            v2 = new Vector3(3.35f, -1.94f, 5);
            v3 = new Vector3(3f, 5, 5);

            StartCoroutine(Workbench());
        }

        private IEnumerator Workbench()
        {
            const int iterations = 100000;
            yield return new WaitForSeconds(1);

            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            var random = new System.Random(seed);
            var testRays = new Ray[iterations];
            for (int i = 0; i < testRays.Length; i++)
            {
                var origin = new OpenTK.Vector3((float)(random.NextDouble() - 0.5) * 5, (float)(random.NextDouble() - 0.5) * 5, 0);
                testRays[i] = new Spatial.Ray(origin, Vector3.UnitZ);
            }

            var timer = Stopwatch.StartNew();
            var intersectionsCount = 0;
            for (int i = 0; i < testRays.Length; i++)
            {
                if (Intersections.LineTriangleIntersection(in testRays[i], v1, v2, v3, out var distance) == 1)
                    intersectionsCount++;

            }
            timer.Stop();

            //40 msec on 100000 iteration
            UnityEngine.Debug.Log($"Old intersector time {timer.ElapsedMilliseconds} msec, intersections count {intersectionsCount}");

            //-----------------------------------------------

            timer = Stopwatch.StartNew();
            intersectionsCount = 0;
            for (int i = 0; i < testRays.Length; i++)
            {
                if (Intersections.RayTriangleIntersection(in testRays[i], v1, v2, v3) > 0)
                    intersectionsCount++;

            }
            timer.Stop();

            //27 msec on 100000 iteration
            UnityEngine.Debug.Log($"New intersector time {timer.ElapsedMilliseconds} msec, intersections count {intersectionsCount}");
        }
    }
}
