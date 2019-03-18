using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TerrainDemo.Hero
{
    public class Input : MonoBehaviour
    {
        private Vector2 _oldMoveDirection;
        private Vector2 _oldMouseNormalizedPosition;
        private float _lastRotationFired;

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
            if (normalizedPosition.x >= 0 && normalizedPosition.x <= 1 && normalizedPosition.y >= 0 &&
                normalizedPosition.y <= 1)
            {
                var mouseManualRotation = (normalizedPosition - _oldMouseNormalizedPosition).x;

                //Get simulated delta if mouse near screen edges
                var mouseAutoRotation = 0f;
                if (normalizedPosition.x < 0.1f && normalizedPosition.x >= 0)
                    mouseAutoRotation = -0.01f;
                else if (normalizedPosition.x > 0.9f && normalizedPosition.x <= 1)
                    mouseAutoRotation = 0.01f;

                if (Math.Abs(mouseAutoRotation) > Math.Abs(mouseManualRotation))
                    mouseManualRotation = mouseAutoRotation;

                if (mouseManualRotation != _lastRotationFired)
                {
                    if (mouseManualRotation != 0)
                        Rotate?.Invoke(mouseManualRotation);
                    else
                        StopRotating?.Invoke();

                    _lastRotationFired = mouseManualRotation;
                }

                _oldMouseNormalizedPosition = normalizedPosition;
            }
            else
            {
                if (_lastRotationFired != 0)
                {
                    _lastRotationFired = 0;
                    StopRotating?.Invoke();
                }

                _oldMouseNormalizedPosition = normalizedPosition;
            }

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

            if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                Build();
            }


            //Debug keys
            //Soft restart
            if (UnityEngine.Input.GetKey(KeyCode.R) && UnityEngine.Input.GetKey(KeyCode.LeftShift))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        public event Action Fire = delegate { };

        public event Action Build = delegate { };
    }
}
