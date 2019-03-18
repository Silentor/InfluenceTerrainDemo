﻿using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Tests
{
    public class TestIntersection : MonoBehaviour
    {
        private GameObject _handle4;
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

            _handle4 = new GameObject("Handle4");
            _handle4.transform.parent = transform;
            _handle4.transform.localPosition = new Vector3(0, 0, 0);
        }

        
        /// <summary>
        /// Test line-triangle intersection
        /// </summary>
        void OnDrawGizmos()
        {
            if(!Application.isPlaying)
                return;

            DrawPolyline.ForGizmo(new Vector3[] {_handle1.transform.position, _handle2.transform.position, _handle3.transform.position }, Color.white, true);

            //var screenRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(Input.mousePosition);
            var screenRay = new Ray(Vector3.zero, Vector3.forward);
            Debug.DrawRay(screenRay.origin, screenRay.direction * 100, Color.yellow);
            float distance;

            var v1 = _handle1.transform.position;
            var v2 = _handle2.transform.position;
            var v3 = _handle3.transform.position;
            var result2 = Intersections.RayTriangleIntersection(new Spatial.Ray(screenRay.origin, screenRay.direction),
                v1, v2, v3);

            var intersection = screenRay.GetPoint(result2);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(intersection, 0.1f);

            Debug.Log(result2);

            /*
            var result = Intersections.LineTriangleIntersection(screenRay, _handle1.transform.position, _handle2.transform.position,
                //_handle3.transform.position, out distance);

            var intersection = screenRay.GetPoint(distance);

            if (result == 1)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(intersection, 0.1f);
            }
            else 
                Debug.Log(result);
                */
        }
        

        /*
        /// <summary>
        /// Test barycentric interpolation
        /// </summary>
        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            _handle1.transform.position = (Vector2)_handle1.transform.position;
            _handle2.transform.position = (Vector2)_handle2.transform.position;
            _handle3.transform.position = (Vector2)_handle3.transform.position;
            _handle4.transform.position = (Vector2)_handle4.transform.position;

            DrawPolyline.ForGizmo(new[]
            {
                _handle1.transform.position, _handle2.transform.position, _handle3.transform.position
            }, Color.white, true);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_handle1.transform.position, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_handle2.transform.position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_handle3.transform.position, 0.5f);

            
            var result = Intersections.Barycentric2DCoords((OpenToolkit.Mathematics.Vector2)_handle4.transform.position, 
                (OpenToolkit.Mathematics.Vector2)_handle1.transform.position,
                (OpenToolkit.Mathematics.Vector2)_handle2.transform.position,
                (OpenToolkit.Mathematics.Vector2)_handle3.transform.position);
            Debug.Log(result);

            var resultColor = new Color(result.u, result.v, result.w);
            Gizmos.color = resultColor;
            Gizmos.DrawSphere(_handle4.transform.position, 0.5f);
        }
        */
    }
}
