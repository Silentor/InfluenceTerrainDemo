using System;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor
    {
        private Runner _target;
        private LandLayout _layout;
        private Main _main;
        private static RunnerEditor _self;

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                _target = (Runner)target;
                _main = _target.Main;
                _layout = _main.LandLayout;
                _self = this;
            }
        }

        #region Unity

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && _target != null)
            {
                if (GUILayout.Button("Rebuild land"))
                {
                    _target.BuildAll();
                    _layout = _main.LandLayout;
                }

                if (GUILayout.Button("Create map"))
                {
                    var map = _main.GenerateMap(_target);
                    _target.MeshAndVisualize(map);
                }
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
                        ShowBlockInfo(layoutHitPoint);
                    }
                }
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmos(Runner target, GizmoType gizmoType)
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
                    _self.DrawSelectedZone(target, layoutHitPoint);
                }

                //писать зону под курсором
                //визуализировать чанки зоны, личные и общие
            }
        }

        #endregion

        private static void ShowChunkBounds(Vector2 worldPosition)
        {
            //Draw chunk bounds
            var chunkPos = Chunk.GetPosition(worldPosition);
            var chunkBounds = Chunk.GetBounds(chunkPos);
            DrawRectangle.ForGizmo(chunkBounds, Color.gray);

            //Draw block bounds
            DrawRectangle.ForGizmo(new Bounds2i((Vector2i)worldPosition, 1, 1), Color.gray);
        }

        private void DrawSelectedZone(Runner target, Vector2 worldPosition)
        {
            //Calc zone under cursor
            if (_main.LandLayout != null && _main.LandLayout.Zones.Any() && _main.LandLayout.Bounds.Contains((Vector2i)worldPosition))
            {
                var selectedZone = _main.LandLayout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).First();
                var zoneColor = target[selectedZone.Type].LandColor;

                Gizmos.color = zoneColor;

                //Draw zone bounds
                //foreach (var chunk in selectedZone.ChunkBounds)
                //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

                foreach (var chunk in _main.LandLayout.GetChunks(selectedZone))
                    DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

                foreach (var block in selectedZone.GetBlocks2())
                    DrawRectangle.ForGizmo(new Bounds2i(block, 1, 1), zoneColor);

                //Draw additional rays to highlight zone
                foreach (var edge in selectedZone.Cell.Edges)
                {
                    //var edgeCenter = new Vector3((edge.Vertex1.x + edge.Vertex2.x)/2, 0, (edge.Vertex1.y + edge.Vertex2.y) / 2);
                    //Gizmos.DrawLine(Convert(selectedZone.Center), edgeCenter);
                    Gizmos.DrawLine(Convert((edge.Vertex1 - selectedZone.Center) * 0.99f + selectedZone.Center),
                        Convert((edge.Vertex2 - selectedZone.Center) * 0.99f + selectedZone.Center));
                    Gizmos.DrawLine(Convert((edge.Vertex1 - selectedZone.Center) * 0.9f + selectedZone.Center),
                        Convert((edge.Vertex2 - selectedZone.Center) * 0.9f + selectedZone.Center));
                    Gizmos.DrawLine(Convert((edge.Vertex1 - selectedZone.Center) * 0.7f + selectedZone.Center),
                        Convert((edge.Vertex2 - selectedZone.Center) * 0.7f + selectedZone.Center));
                }

                //Draw zone chunks
                //foreach (var chunk in Main.Layout.GetChunks(selectedZone))
                //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);
            }
        }

        private void ShowInfluenceInfo(Vector2 layoutPosition)
        {
            if (_layout != null)
            {
                var influence = _layout.GetInfluence(layoutPosition);

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
            var zones = _layout.Zones.ToArray();
            {
                Handles.matrix = Matrix4x4.identity;

                var newCenters = zones.Select(c => Convert(c.Center)).ToArray();

                for (int i = 0; i < zones.Count(); i++)
                {
                    var zone = zones[i];
                    Handles.color = _layout.Zones.ElementAt(i).Type != ZoneType.Empty ? _target[zone.Type].LandColor : Color.black;

                    //Draw edges
                    foreach (var edge in zone.Cell.Edges)
                        Handles.DrawAAPolyLine(Convert(edge.Vertex1), Convert(edge.Vertex2));

                    //Draw fill
                    Handles.color = new Color(Handles.color.r / 2, Handles.color.g / 2, Handles.color.b / 2, Handles.color.a / 2);
                    foreach (var vert in zone.Cell.Vertices)
                        Handles.DrawLine(Convert(zone.Center), Convert(vert));

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

        private void ShowZoneInfo(Vector2 worldPosition)
        {
            //Calc zone under cursor
            if (_layout != null && _layout.Zones.Any() && _layout.Bounds.Contains((Vector2i)worldPosition))
            {
                var selectedZone = _layout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).First();

                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(0, Screen.height - 200, 200, 110));
                GUILayout.Label("Zone " + selectedZone.Type);
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        private void ShowBlockInfo(Vector2 worldPosition)
        {
            if (_main.Map == null)
                return;

            //Calc block under cursor
            var blockPos = (Vector2i) worldPosition;
            var block = _main.Map.GetBlock(blockPos);

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(0, Screen.height - 400, 200, 110));
            GUILayout.Label("Block: " + block);
            GUILayout.EndArea();
            Handles.EndGUI();
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
