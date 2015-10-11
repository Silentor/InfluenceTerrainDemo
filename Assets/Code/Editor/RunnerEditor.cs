using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Layout;
using Assets.Code.Voronoi;
using UnityEditor;
using UnityEngine;

namespace Assets.Code.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor 
    {
        private Runner _target;
        private List<Zone> _layout;
        private Cell[] _voronoi;

        void OnEnable()
        {
            _target = (Runner) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && _target != null)
            {
                if (_layout == null)
                {
                    //Local copy
                    _layout = Main.Layout.Zones.ToList();
                    _voronoi = CellMeshGenerator.Generate(_layout.Select(z => z.Center), _target.LayoutBounds);
                }

                if (GUILayout.Button("Rebuild land"))
                {
                    _layout = null;
                    _voronoi = null;
                    _target.BuildAll();
                }

                if (_layout != null && GUILayout.Button("Create map"))
                {
                    Main.Layout = new LandLayout(_target.LayoutBounds, _layout.ToArray());
                    var map = Main.GenerateMap(_target);
                    _target.MeshAndVisualize(map);
                }

                //if (GUILayout.Button("Set layout"))
                //{
                //    _target.Set Layout(_layout);
                //}
            }
        }

        void OnSceneGUI()
        {
            //Get intersection points
            if (Application.isPlaying)
            {
                ShowZoneLayoutEditor();

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
            if (Main.Land != null)
            {
                var influence = Main.Land.GetInfluence(layoutPosition);

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

        private void ShowZoneLayoutEditor()
        {
            if(_voronoi != null)
            {
                Handles.matrix = Matrix4x4.identity;

                var newCenters = _voronoi.Select(c => Convert(c.Center)).ToArray();

                for (int i = 0; i < _voronoi.Count(); i++)
                {
                    var cell = _voronoi.ElementAt(i);
                    Handles.color = _target.Zones.First(zs => zs.Type == _layout[i].Type).LandColor;

                    //Draw edges
                    foreach (var edge in cell.Edges)
                        Handles.DrawAAPolyLine(Convert(edge.Vertex1), Convert(edge.Vertex2));

                    //Draw fill
                    foreach (var vert in cell.Vertices)
                        Handles.DrawLine(Convert(cell.Center), Convert(vert));

                    newCenters[i] = Handles.Slider2D(newCenters[i], Vector3.forward, Vector3.forward, Vector3.right, 5, Handles.SphereCap, 0);
                }

                if (GUI.changed)
                {
                    _voronoi = CellMeshGenerator.Generate(newCenters.Select(c => Convert(c)), _target.LayoutBounds);
                    for (int i = 0; i < newCenters.Length; i++)
                        _layout[i] = new Zone(_voronoi[i].Center, _layout[i].Type);
                }
            }
        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        private static Vector2 Convert(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

    }
}
