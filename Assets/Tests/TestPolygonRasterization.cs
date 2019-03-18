using System;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tests
{
    public class TestPolygonRasterization : MonoBehaviour
    {
        public Mode Workmode;

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

            _h3 = new GameObject("Handle3").transform;
            _h3.parent = transform;
            _h3.localPosition = new Vector3(-2.4f, 0, -1.1f);

            _h4 = new GameObject("Handle4").transform;
            _h4.parent = transform;
            _h4.localPosition = new Vector3(-0.5f, 0, 2.3f);
        }

        void OnDrawGizmos()
        {
            if (_h1 && _h2 && _h3 && _h4)
            {
                var center = (_h1.position + _h2.position + _h3.position + _h4.position) / 4;

                var h1 = new HalfPlane(_h1.position.ConvertTo2D(), _h2.position.ConvertTo2D(), center.ConvertTo2D());
                var h2 = new HalfPlane(_h2.position.ConvertTo2D(), _h3.position.ConvertTo2D(), center.ConvertTo2D());
                var h3 = new HalfPlane(_h3.position.ConvertTo2D(), _h4.position.ConvertTo2D(), center.ConvertTo2D());
                var h4 = new HalfPlane(_h4.position.ConvertTo2D(), _h1.position.ConvertTo2D(), center.ConvertTo2D());

                var containsCallCounter = 0;
                
                var isContains = new Predicate<Vector2>(delegate(Vector2 p)
                {
                    containsCallCounter++;
                    return HalfPlane.ContainsInConvex(p, new []{h1, h2, h3, h4});
                });
                var bounds = new Box2(
                    Mathf.Min(_h1.position.x, _h2.position.x, _h3.position.x, _h4.position.x),
                    Mathf.Max(_h1.position.z, _h2.position.z, _h3.position.z, _h4.position.z),
                    Mathf.Max(_h1.position.x, _h2.position.x, _h3.position.x, _h4.position.x),
                    Mathf.Min(_h1.position.z, _h2.position.z, _h3.position.z, _h4.position.z));

                //Draw test polygon
                Gizmos.color = Color.white;
                Gizmos.DrawLine(_h1.position, _h2.position);
                Gizmos.DrawLine(_h2.position, _h3.position);
                Gizmos.DrawLine(_h3.position, _h4.position);
                Gizmos.DrawLine(_h4.position, _h1.position);

                //Draw "world" bounds
                DrawRectangle.ForGizmo(bounds, Color.gray);

                if (Workmode == Mode.Blocks)
                {
                    //Draw block bounds
                    var blockBounds = (Bounds2i) bounds;

                    //Draw block centers inside bounds
                    foreach (var blockBound in blockBounds)
                    {
                        var blockCenter = BlockInfo.GetWorldCenter(blockBound);
                        DebugExtension.DrawPoint(blockCenter.ConvertTo3D(), Color.white / 2, 0.1f);
                    }

                    var blocks = Rasterization.ConvexToBlocks(isContains, bounds);
                    Assert.IsTrue(blocks.Length == blocks.Distinct().Count());

                    //Draw rasterized blocks
                    foreach (var p in blocks)
                        DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1), Color.green);
                }
                else
                {
                    var vertices = Rasterization.ConvexToVertices(isContains, bounds);
                    Assert.IsTrue(vertices.Length == vertices.Distinct().Count());

                    //Draw rasterized vertices
                    foreach (var p in vertices)
                        DebugExtension.DrawPoint(p, Color.green, 0.1f);
                }

                Debug.LogFormat("Contains calls count {0}", containsCallCounter);


            }
        }

        public enum Mode
        {
            Blocks,
            Vertices
        }
    }
}
