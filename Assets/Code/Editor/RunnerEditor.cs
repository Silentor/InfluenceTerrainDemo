using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Layout;
using UnityEditor;
using UnityEngine;

namespace Assets.Code.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor 
    {
        private Runner _target;
        private IEnumerable<Zone> _layout;
        private Dictionary<Vector2i, Chunk> _map;
        private Land _land;

        void OnEnable()
        {
            _target = (Runner) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Create land"))
                {
                    _target.BuildAll();
                }

                if (GUILayout.Button("Recreate map"))
                {
                    _target.RecreateMap();
                }
            }
        }

        void OnSceneGUI()
        {
            //Get intersection points
            if (Application.isPlaying)
            {
                var worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var worldPlane = new Plane(Vector3.up, Vector3.zero);
                float hitDistance;
                if (worldPlane.Raycast(worldRay, out hitDistance))
                {
                    var layoutHitPoint3 = worldRay.GetPoint(hitDistance);
                    var layoutHitPoint = new Vector2(layoutHitPoint3.x, layoutHitPoint3.z);

                    if(Event.current.shift)
                        ShowInfluenceInfo(layoutHitPoint);
                }
            }
        }

        private void ShowInfluenceInfo(Vector2 layoutPosition)
        {
            if (_target.Land != null)
            {
                var influence = _target.Land.GetInfluence(layoutPosition);

                Handles.BeginGUI();

                GUILayout.BeginArea(new Rect(Vector2.zero, Vector2.one * 200));

                GUILayout.Label("Absolute influence");
                var labelText = string.Join("\n", influence.Select(z => string.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                GUILayout.EndArea();

                if (_target.InfluenceLimit == 0)
                    influence = influence.Pack(_target.InfluenceThreshold);
                else
                    influence = influence.Pack(_target.InfluenceLimit);

                GUILayout.BeginArea(new Rect(new Vector2(0, 220), new Vector2(200, 420)));

                GUILayout.Label("Packed influence");
                labelText = string.Join("\n", influence.Select(z => string.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                GUILayout.EndArea();

                Handles.EndGUI();
            }
        }
        
        
    }
}
