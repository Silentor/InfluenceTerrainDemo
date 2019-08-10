using System.Linq;
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
        private GUIStyle _currentWaypointStyle;
        private GUIStyle _nextWaypointStyle;
        private GUIStyle _oldWaypointStyle;
        private MicroMap _microMap;

        private void OnEnable()
        {
            _actor = ((ActorView) target).Actor;
            _microMap = FindObjectOfType<TriRunner>().Micro;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_currentWaypointStyle == null)
            {
                _currentWaypointStyle = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
                _nextWaypointStyle    = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.white}};
                _oldWaypointStyle     = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.gray}};
            }

            var debugInternals = _actor.GetDebugState();

            GUILayout.Label($"Map: {_actor.Map.Name}");
            GUILayout.Label($"Block: {_actor.BlockPosition}");

            GUILayout.Label("Locomotor", EditorStyles.centeredGreyMiniLabel);

            using (var speedScope = new EditorGUI.ChangeCheckScope())
            {
                var newSpeed = EditorGUILayout.DelayedFloatField("Speed", _actor.Speed);
                if (speedScope.changed)
                    _actor.Speed = newSpeed;
            }

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

            _actor.DebugLocomotion = GUILayout.Toggle(_actor.DebugLocomotion, "Debug locomotion");

            //Navigation component
            GUILayout.Label("Navigator", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"Is navigated: {_actor.Nav.IsNavigated}");

            //List path
            if (_actor.Nav.Path != null)
            {
                var path = _actor.Nav.Path;
                GUILayout.Label($"Path from {path.Start} to {path.Finish}, is valid {path.IsValid}, total length {path.GetPathLength()}");
                var segmentStyle = _oldWaypointStyle;
                foreach (var segment in path.Segments)
                {
                    GUIStyle waypointStyle;
                    if (segment.Node == path.Current.node)
                    {
                        segmentStyle = _currentWaypointStyle;
                        waypointStyle = _oldWaypointStyle;
                    }
                    else
                        waypointStyle = segmentStyle;

                    using (var h = new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"  {segment.ToString()}", segmentStyle);
                        if (GUILayout.Button("ʘ", GUILayout.Height(15), GUILayout.Width(15)))
                        {
                            var segmentBounds = new Bounds(segment.Points.First(), UnityEngine.Vector3.zero);
                            segmentBounds.Encapsulate(segment.Points.Last());
                            SceneView.lastActiveSceneView.Frame(segmentBounds);
                        }
                    }

                    foreach (var waypoint in segment.Points)
                    {
                        if (waypoint == path.Current.position)
                            waypointStyle = _currentWaypointStyle;

                        using (var h = new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label($"    {waypoint.ToString()}", waypointStyle);
                            if (GUILayout.Button("ʘ", GUILayout.Height(15), GUILayout.Width(15)))
                            {
                                SceneView.lastActiveSceneView.Frame(new Bounds(waypoint, UnityEngine.Vector3.one * 5));
                            }
                        }

                        if (waypoint == path.Current.position)
                            waypointStyle = _nextWaypointStyle;
                    }

                    if (segment.Node == path.Current.node)
                        segmentStyle = _nextWaypointStyle;
                }
            }
            
            //Easy debug navigation
            if (!_actor.IsHero)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Navigate to (45, 23)")) _actor.Nav.Go((45, 23));
                if (GUILayout.Button("Navigate to (-49, -61)")) _actor.Nav.Go((-49, -61));
                GUILayout.EndHorizontal();
            }

            _actor.DebugNavigation = GUILayout.Toggle(_actor.DebugNavigation, "Debug navigation");

        }

        private void OnSceneGUI()
        {
            if (_actor != null)
            {
                //Show navigation path
                if (_actor.Nav.IsNavigated)
                {
	                //Show all processed cells
	                foreach (var navCell in _actor.Nav.Path.ProcessedCosts)
	                {
		                if(_actor.Nav.Path.Segments.All(s => s.Node != navCell.Item1))
			                HandleMap.DrawNavigationNode( navCell.Item1, 20, Color.red, false );

		                //var mapPosition = BlockInfo.GetWorldCenter(wp.Position);
		                //var position = new UnityEngine.Vector3(mapPosition.X, wp.Map.GetBlockData(wp.Position).Height,
		                //    mapPosition.Y);
		                //DrawPoint.ForFebug2D(position, 0.5f, Color.blue, false);
	                }

                    foreach (var node in _actor.Nav.Path.Segments)
                    {
						//Draw node
						HandleMap.DrawNavigationNode( node.Node, 20, true );


                        //var mapPosition = BlockInfo.GetWorldCenter(node);
                        //var position = new UnityEngine.Vector3(mapPosition.X, _actor.Map.GetBlockData(node).Height,
                        //    mapPosition.Y);

                        ////if(wp == _actor.Nav.Path.CurrentPoint)
                        //    //color = Color.red;

                        //Handles.color = color;
                        ////var markSize = _actor.Nav.Path.Segments.Any(s => s.From == wp) ? 1 : 0.5f;
                        //Handles.SphereHandleCap(0, position, Quaternion.identity, /*markSize*/0.5f, EventType.Repaint);
                        ////DebugExtension.DebugWireSphere(position, color, 0.3f);

                        ////if (wp == _actor.Nav.Path.CurrentPoint)
                        //    //color = Color.white;
                    }

                    
                    
                }
            }
        }
    }
}
