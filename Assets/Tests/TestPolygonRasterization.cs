using System;
using System.Linq;
using OpenTK;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tests
{
    public class TestPolygonRasterization : MonoBehaviour
    {
        private Transform _h1;
        private Transform _h2;
        private Transform _h3;
        private Transform _h4;

        // Use this for initialization
        void Start()
        {
            _h1 = new GameObject("Handle1").transform;
            _h1.parent = transform;
            _h1.localPosition = new Vector3(1.2f, 0, 1.5f);

            _h2 = new GameObject("Handle2").transform;
            _h2.parent = transform;
            _h2.localPosition = new Vector3(0.55f, 0, -1.55f);

            _h3 = new GameObject("Handle2").transform;
            _h3.parent = transform;
            _h3.localPosition = new Vector3(-2.4f, 0, -1.1f);

            _h4 = new GameObject("Handle2").transform;
            _h4.parent = transform;
            _h4.localPosition = new Vector3(-0.5f, 0, 2.3f);
        }

        void OnDrawGizmos()
        {
            if (_h1 && _h2 && _h3 && _h4)
            {

                var center = (_h1.position + _h2.position + _h3.position + _h4.position) / 4;

                var h1 = new HalfPlane((Vector2)_h1.position, (Vector2)_h2.position, (Vector2)center);
                var h2 = new HalfPlane((Vector2)_h2.position, (Vector2)_h3.position, (Vector2)center);
                var h3 = new HalfPlane((Vector2)_h3.position, (Vector2)_h4.position, (Vector2)center);
                var h4 = new HalfPlane((Vector2)_h4.position, (Vector2)_h1.position, (Vector2)center);

                var isContains = new Predicate<Vector2>(p => HalfPlane.ContainsInConvex(p, h1, h2, h3, h4));
                var bounds = new Box2(
                    Mathf.Min(_h1.position.x, _h2.position.x, _h3.position.x, _h4.position.x),
                    Mathf.Max(_h1.position.z, _h2.position.z, _h3.position.z, _h4.position.z),
                    Mathf.Max(_h1.position.x, _h2.position.x, _h3.position.x, _h4.position.x),
                    Mathf.Min(_h1.position.z, _h2.position.z, _h3.position.z, _h4.position.z));
                var blocks = Rasterization.Polygon2(isContains, bounds);
               
                Assert.IsTrue(blocks.Length == blocks.Distinct().Count());

                Gizmos.color = Color.white;
                Gizmos.DrawLine(_h1.position, _h2.position);
                Gizmos.DrawLine(_h2.position, _h3.position);
                Gizmos.DrawLine(_h3.position, _h4.position);
                Gizmos.DrawLine(_h4.position, _h1.position);

                foreach (var p in blocks)
                    DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1));

                DrawRectangle.ForGizmo(bounds, Color.gray);
            }
        }
    }
}
