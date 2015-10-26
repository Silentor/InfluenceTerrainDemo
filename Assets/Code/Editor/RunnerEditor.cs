using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Layout;
using Assets.Code.Tools;
using Assets.Code.Voronoi;
using UnityEditor;
using UnityEngine;

namespace Assets.Code.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor
    {
        private Runner _target;
        private List<ZoneLayout> _layout;
        private Cell[] _voronoi;

        void OnEnable()
        {
            _target = (Runner)target;
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
                    _voronoi = CellMeshGenerator.Generate(_layout.Select(z => z.Center), (Bounds)_target.LayoutBounds);
                }

                if (GUILayout.Button("Rebuild land"))
                {
                    _layout = null;
                    _voronoi = null;
                    _target.BuildAll();
                }

                if (_layout != null && GUILayout.Button("Create map"))
                {
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

                var pos = Event.current.mousePosition;
                if (pos.x >= 0 && pos.x <= Screen.width && pos.y >= 0 && pos.y <= Screen.height)
                {
                    var worldRay = HandleUtility.GUIPointToWorldRay(pos);
                    var worldPlane = new Plane(Vector3.up, Vector3.zero);
                    float hitDistance;
                    if (worldPlane.Raycast(worldRay, out hitDistance))
                    {
                        var layoutHitPoint3 = worldRay.GetPoint(hitDistance);
                        var layoutHitPoint = new Vector2(layoutHitPoint3.x, layoutHitPoint3.z);

                        if (Event.current.shift)
                            ShowInfluenceInfo(layoutHitPoint);

                        ShowChunkInfo(layoutHitPoint);
                        ShowZoneInfo(layoutHitPoint);
                    }
                }
            }
        }

        [DrawGizmo(GizmoType.Selected, typeof(Runner))]
        static void OnGizmoDraw(Runner target, GizmoType gizmoType)
        {
            //Draw land bounds
            DrawRectangle.ForGizmo(target.LayoutBounds, Color.gray);

            //Get intersection points
            if (Application.isPlaying)
            {
                var worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var worldPlane = new Plane(Vector3.up, Vector3.zero);
                float hitDistance;
                if (worldPlane.Raycast(worldRay, out hitDistance))
                {
                    var layoutHitPoint3d = worldRay.GetPoint(hitDistance);
                    var layoutHitPoint = new Vector2(layoutHitPoint3d.x, layoutHitPoint3d.z);

                    ShowChunkBounds(layoutHitPoint);
                    DrawSelectedZone(target, layoutHitPoint);
                }

                //писать зону под курсором
                //визуализировать чанки зоны, личные и общие
            }
        }

        private static void ShowChunkBounds(Vector2 worldPosition)
        {
            //Draw chunk bounds
            var chunkPos = Chunk.GetPosition(worldPosition);
            var chunkBounds = Chunk.GetBounds(chunkPos);
            DrawRectangle.ForGizmo(chunkBounds, Color.gray);

            //Draw block bounds
            DrawRectangle.ForGizmo(new Bounds2i((Vector2i)worldPosition, 1, 1), Color.gray);
        }

        private static void DrawSelectedZone(Runner target, Vector2 worldPosition)
        {
            //Calc zone under cursor
            if (Main.Layout != null && Main.Layout.Zones.Any() && Main.Layout.Bounds.Contains((Vector2i)worldPosition))
            {
                var selectedZone = Main.Layout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).First();
                var zoneColor = target[selectedZone.Type].LandColor;

                Gizmos.color = zoneColor;

                //Draw zone bounds
                DrawRectangle.ForGizmo(selectedZone.Bounds, zoneColor);

                //Draw additional rays to highlight zone
                foreach (var edge in selectedZone.Cell.Edges)
                {
                    var edgeCenter = new Vector3((edge.Vertex1.x + edge.Vertex2.x)/2, 0, (edge.Vertex1.y + edge.Vertex2.y) / 2);
                    Gizmos.DrawLine(Convert(selectedZone.Center), edgeCenter);
                }

                //Draw zone chunks
                //foreach (var chunk in Main.Layout.GetChunks(selectedZone))
                    //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);
            }
        }

        private void ShowInfluenceInfo(Vector2 layoutPosition)
        {
            if (Main.Layout != null)
            {
                var influence = Main.Layout.GetInfluence(layoutPosition);

                Handles.BeginGUI();

                GUILayout.BeginArea(new Rect(Vector2.zero, Vector2.one * 200));

                GUILayout.Label("Absolute influence");
                var labelText = String.Join("\n", influence.Select(z => String.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                GUILayout.EndArea();

                if (_target.InfluenceLimit == 0)
                    influence = influence.Pack(_target.InfluenceThreshold);
                else
                    influence = influence.Pack(_target.InfluenceLimit);

                GUILayout.BeginArea(new Rect(new Vector2(0, 220), new Vector2(200, 420)));

                GUILayout.Label("Packed influence");
                labelText = String.Join("\n", influence.Select(z => String.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                GUILayout.EndArea();

                Handles.EndGUI();
            }
        }

        private void ShowZoneLayoutEditor()
        {
            if (_voronoi != null)
            {
                Handles.matrix = Matrix4x4.identity;

                var newCenters = _voronoi.Select(c => Convert(c.Center)).ToArray();

                for (int i = 0; i < _voronoi.Count(); i++)
                {
                    var cell = _voronoi.ElementAt(i);
                    Handles.color = _layout[i].Type != ZoneType.Empty ? _target.Zones.First(zs => zs.Type == _layout[i].Type).LandColor : Color.black;

                    //Draw edges
                    foreach (var edge in cell.Edges)
                        Handles.DrawAAPolyLine(Convert(edge.Vertex1), Convert(edge.Vertex2));

                    //Draw fill
                    foreach (var vert in cell.Vertices)
                        Handles.DrawLine(Convert(cell.Center), Convert(vert));

                    newCenters[i] = Handles.Slider2D(newCenters[i], Vector3.forward, Vector3.forward, Vector3.right, 5, Handles.SphereCap, 0);
                }

                //if (GUI.changed)
                //{
                //    _voronoi = CellMeshGenerator.Generate(newCenters.Select(c => Convert(c)), (Bounds)_target.LayoutBounds);
                //    for (int i = 0; i < newCenters.Length; i++)
                //        _layout[i] = new Zone(_voronoi[i], _layout[i].Type);
                //}
            }
        }

        private static void ShowChunkInfo(Vector2 worldPosition)
        {
            var chunkPos = Chunk.GetPosition(worldPosition);
            var chunkBounds = Chunk.GetBounds(chunkPos);

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(0, Screen.height - 120, 200, 110));
            GUILayout.Label("World_f " + worldPosition);
            GUILayout.Label("World_i " + (Vector2i)worldPosition);
            GUILayout.Label("Chunk " + chunkPos + " : " + Chunk.GetLocalPosition(worldPosition));
            GUILayout.Label("Bounds " + chunkBounds.Min + "-" + chunkBounds.Max);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void ShowZoneInfo(Vector2 worldPosition)
        {
            //Calc zone under cursor
            if (Main.Layout != null && Main.Layout.Zones.Any() && Main.Layout.Bounds.Contains((Vector2i)worldPosition))
            {
                var selectedZone = Main.Layout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).First();

                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(0, Screen.height - 200, 200, 110));
                GUILayout.Label("Zone " + selectedZone.Type);
                GUILayout.EndArea();
                Handles.EndGUI();
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
