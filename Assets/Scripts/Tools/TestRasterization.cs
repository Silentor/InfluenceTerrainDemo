using TerrainDemo.Layout;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public class TestRasterization : MonoBehaviour
    {
        private GameObject _handle1;
        private GameObject _handle2;

        // Use this for initialization
        void Start()
        {
            _handle1 = new GameObject("Handle1");
            _handle1.transform.parent = transform;
            _handle1.transform.localPosition = new Vector3(-10, 0, -5);

            _handle2 = new GameObject("Handle2");
            _handle2.transform.parent = transform;
            _handle2.transform.localPosition = new Vector3(10, 0, 5);
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

                var points = ZoneLayout.DDA(p1, p2);
                foreach (var p in points)
                    DrawRectangle.ForGizmo(new Bounds2i(p, 1, 1));
            }
        }
    }
}
