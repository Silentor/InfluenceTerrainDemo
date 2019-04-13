using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Micro;
using TerrainDemo.Tools;
using TerrainDemo.Visualization;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(ActorView))]
    public class ActorInspector : UnityEditor.Editor
    {
        private Actor _actor;
        private Vector3 _prevPosition;
        private float _prevTime;
        private float _real3dSpeed;
        private float _real2dSpeed;
        private bool _wasStopped = true;

        private void OnEnable()
        {
            _actor = ((ActorView) target).Actor;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var debugInternals = _actor.GetDebugState();

            GUILayout.Label($"Map: {_actor.Map.Name}");
            GUILayout.Label($"Block: {_actor.BlockPosition}");

            ref readonly var block = ref _actor.Map.GetBlockRef(_actor.BlockPosition);
            var blockNormal = _actor.Map.GetBlockData(_actor.BlockPosition).Normal;

            var blockInclinationAngle = MathHelper.RadiansToDegrees(Vector3.CalculateAngle(Vector3.UnitY, blockNormal));
            GUILayout.Label($"Block angle : {blockInclinationAngle:N2}, block material : {block.Top}");
            GUILayout.Label($"Move dir : {debugInternals.moveDirection}, move magnitude : {debugInternals.moveDirection.Length:N3}");
            GUILayout.Label($"Inclination speed mod : {debugInternals.blockInclinationSpeedMod:N2}");
            GUILayout.Label($"Material speed mod : {debugInternals.blockMaterialSpeedMod:N2}");

            var actualSpeed = _actor.Speed * debugInternals.moveDirection.Length *
                              debugInternals.blockInclinationSpeedMod * debugInternals.blockMaterialSpeedMod;
            GUILayout.Label($"Max speed {_actor.Speed}, actual speed {actualSpeed:N2}");

            const float realSpeedCalcDelay = 0.2f;

            if (_wasStopped && _actor.State != Actor.NavState.Stopped)
            {
                //Reset real speed counter
                _prevTime = Time.time;
                _prevPosition = _actor.Position;
            }

            _wasStopped = _actor.State == Actor.NavState.Stopped;

            if (_actor.State != Actor.NavState.Stopped && (Time.time - _prevTime) >= realSpeedCalcDelay)
            {
                _real3dSpeed = (_actor.Position - _prevPosition).Length / (Time.time - _prevTime);
                _real2dSpeed = ((Vector2)_actor.Position - (Vector2)_prevPosition).Length / (Time.time - _prevTime);
                _prevTime = Time.time;
                _prevPosition = _actor.Position;
            }
            GUILayout.Label($"Real 3D speed {_real3dSpeed:N2}, 2D speed {_real2dSpeed:N2}");

            GUILayout.Label("Debug controls");
            if (!_actor.IsHero)
            {
                if (GUILayout.Button("Navigate to (51, 65)"))
                {
                    _actor.Nav.Go((51, 65));
                }

                if (GUILayout.Button("Navigate to (-49, -61)"))
                {
                    _actor.Nav.Go((-49, -61));
                }
            }

        }

        private void OnSceneGUI()
        {
            if (_actor != null)
            {
                //Show navigation path
                if (_actor.Nav.IsNavigated)
                {
                    Color color = Color.gray;
                    foreach (var wp in _actor.Nav.Path.Waypoints)
                    {
                        var mapPosition = BlockInfo.GetWorldCenter(wp.Position);
                        var position = new UnityEngine.Vector3(mapPosition.X, wp.Map.GetBlockData(wp.Position).Height,
                            mapPosition.Y);

                        if(wp == _actor.Nav.Path.Current)
                            color = Color.red;

                        Handles.color = color;
                        Handles.SphereHandleCap(0, position, Quaternion.identity, 1f, EventType.Repaint);
                        //DebugExtension.DebugWireSphere(position, color, 0.3f);

                        if (wp == _actor.Nav.Path.Current)
                            color = Color.white;
                    }
                }
            }
        }
    }
}
