using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TerrainDemo.Hero
{
    public class Input : MonoBehaviour
    {
        private Vector2 _oldMoveDirection;
        private Vector2? _oldMouseNormalizedPosition;
        private float _lastRotationFired;
        private float _lastZoomFired;

        private void Start()
        {
            _oldMouseNormalizedPosition = UnityEngine.Input.mousePosition / new Vector2(Screen.width, Screen.height);
        }

        void Update()
        {
            //Process movement input
            var moveDir = Vector2.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W))
                moveDir = Vector2.up;
            if (UnityEngine.Input.GetKey(KeyCode.S))
                moveDir += Vector2.down * 0.5f;
            if (UnityEngine.Input.GetKey(KeyCode.A))
                moveDir += Vector2.left * 0.75f;
            if (UnityEngine.Input.GetKey(KeyCode.D))
                moveDir += Vector2.right * 0.75f;

            if (_oldMoveDirection != moveDir)
            {
                if (moveDir != Vector2.zero)
                {
                    var clampedDir = Vector2.ClampMagnitude(moveDir, 1);
                    Move?.Invoke(new OpenToolkit.Mathematics.Vector2(clampedDir.x, clampedDir.y));
                }
                else
                    StopMoving?.Invoke();

                _oldMoveDirection = moveDir;
            }

            //Process rotation input
            

            //Get horizontal mouse move
            var normalizedPosition = UnityEngine.Input.mousePosition / new Vector2(Screen.width, Screen.height);

            if (UnityEngine.Input.GetMouseButtonDown(1) && normalizedPosition.x >= 0 && normalizedPosition.x <= 1 && normalizedPosition.y >= 0 && normalizedPosition.y <= 1)
            {
                _oldMouseNormalizedPosition = normalizedPosition;
            }
            else if (UnityEngine.Input.GetMouseButton(1) && _oldMouseNormalizedPosition.HasValue)
            {
                float mouseManualRotation = Math.Sign((normalizedPosition - _oldMouseNormalizedPosition.Value).x);
                _oldMouseNormalizedPosition = normalizedPosition;

                if (mouseManualRotation != 0)
                    DoRotate(mouseManualRotation);
                else
                    DoStopRotating();
            }
            else if(UnityEngine.Input.GetMouseButtonUp(1))
            {
                DoStopRotating();
                _oldMouseNormalizedPosition = null;
            }

            DoZoom(UnityEngine.Input.mouseScrollDelta.y);

            /*
            else if (UnityEngine.Input.GetKey(KeyCode.Q))
                Rotate(-1);
            else if (UnityEngine.Input.GetKey(KeyCode.E))
                Rotate(1);
            */

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                Fire();
            }

            /*
            if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                Build();
            }
            */


            //Debug keys
            //Soft restart
            if (UnityEngine.Input.GetKey(KeyCode.R) && UnityEngine.Input.GetKey(KeyCode.LeftShift))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void DoStopRotating()
        {
            if (_lastRotationFired != 0)
            {
                StopRotating?.Invoke();
                _lastRotationFired = 0;
            }
        }

        private void DoRotate(float value)
        {
            if (_lastRotationFired != value)
            {
                if(value != 0)
                    Rotate?.Invoke(value);
                else
                    StopRotating?.Invoke();
                _lastRotationFired = value;
            }
        }

        private void DoZoom(float value)
        {
            if (_lastZoomFired != value)
            {
                Zoom?.Invoke(-value);
                _lastZoomFired = value;
            }
        }

        /// <summary>
        /// Fired when move direction changed. Param: normalized move direction
        /// </summary>
        public event Action<OpenToolkit.Mathematics.Vector2> Move = delegate { };

        /// <summary>
        /// No move event present
        /// </summary>
        public event Action StopMoving;

        /// <summary>
        /// Rotate by normalize value
        /// </summary>
        public event Action<float> Rotate = delegate { };

        /// <summary>
        /// No rotation event present
        /// </summary>
        public event Action StopRotating;

        public event Action<float> Zoom;

        public event Action Fire = delegate { };

        public event Action Build = delegate { };
    }
}
