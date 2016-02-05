using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tools
{
    public class TestIntersection : MonoBehaviour
    {
        private GameObject _handle3;
        private GameObject _handle1;
        private GameObject _handle2;

        void Start()
        {
            _handle1 = new GameObject("Handle1");
            _handle1.transform.parent = transform;
            _handle1.transform.localPosition = new Vector3(-10, 0, -5);

            _handle2 = new GameObject("Handle2");
            _handle2.transform.parent = transform;
            _handle2.transform.localPosition = new Vector3(10, 1, 5);

            _handle3 = new GameObject("Handle3");
            _handle3.transform.parent = transform;
            _handle3.transform.localPosition = new Vector3(-10, 5, 5);
        }

        void OnDrawGizmos()
        {
            if(!Application.isPlaying)
                return;

            DrawPolyline.ForGizmo(new Vector3[] {_handle1.transform.position, _handle2.transform.position, _handle3.transform.position, _handle1.transform.position }, Color.white);

            //var screenRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(Input.mousePosition);
            var screenRay = new Ray(Vector3.zero, Vector3.forward);
            Debug.DrawRay(screenRay.origin, screenRay.direction * 1000, Color.yellow);
            Vector3 intersectPos;
            var result = Intersections.LineTriangleIntersection(screenRay, _handle1.transform.position, _handle2.transform.position,
                _handle3.transform.position, out intersectPos);

            if(result == 1)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(intersectPos, 0.1f);
            }
            else 
                Debug.Log(result);
        }
    }
}
