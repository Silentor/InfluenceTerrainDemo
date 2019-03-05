using System.Linq;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tests
{
    public class TestLineRasterization : MonoBehaviour
    {
        private GameObject _handle1;
        private GameObject _handle2;
        private int counter;

        // Use this for initialization
        void Start()
        {
            _handle1 = new GameObject("Handle1");
            _handle1.transform.parent = transform;
            _handle1.transform.localPosition = new Vector3(UnityEngine.Random.Range(-10f, 10), 0, UnityEngine.Random.Range(-10f, 10));

            _handle2 = new GameObject("Handle2");
            _handle2.transform.parent = transform;
            _handle2.transform.localPosition = new Vector3(UnityEngine.Random.Range(-10f, 10), 0, UnityEngine.Random.Range(-10f, 10));
        }

        void OnDrawGizmos()
        {
            if (_handle1 && _handle2)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(_handle1.transform.position, _handle2.transform.position);

                //var p1 = new Vector2i(_handle1.transform.position.x, _handle1.transform.position.z);
                //var p2 = new Vector2i(_handle2.transform.position.x, _handle2.transform.position.z);
                var p1 = new Vector2(_handle1.transform.position.x, _handle1.transform.position.z);
                var p2 = new Vector2(_handle2.transform.position.x, _handle2.transform.position.z);
                var pi1 = (Vector2i) p1;
                var pi2 = (Vector2i)p2;

                //var points = Rasterization.DDA(p1, p2, true).ToArray();     Debug.Log("Rasterization.DDA conservative");
                var pointsDDA = Rasterization.DDA(p1, p2, false).ToArray(); 
                //var points = Rasterization.lineNoDiag(pi1.X, pi1.Z, pi2.X, pi2.Z).ToArray();  Debug.Log("Rasterization.lineNoDiag");
                var pointsBresInt = Rasterization.BresenhamInt(pi1, pi2).ToArray(); 
                var pointsBresFloat = Rasterization.BresenhamFloat(p1.X, p1.Y, p2.X, p2.Y).ToArray(); 

                Assert.IsTrue(pointsDDA.Length == pointsDDA.Distinct().Count());
                Assert.IsTrue(pointsBresInt.Length == pointsBresInt.Distinct().Count());
                Assert.IsTrue(pointsBresFloat.Length == pointsBresFloat.Distinct().Count());

                if (counter % 20 < 10)
                    foreach (var p in pointsDDA)
                        DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1), Color.white);

                /*
                else if (counter % 21 < 14)
                    foreach (var p in pointsBresInt)
                    DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1), Color.red);
                    */

                else //if (counter % 21 >= 14)
                    foreach (var p in pointsBresFloat)
                        DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1), Color.green);

                counter++;
            }
        }
    }
}
