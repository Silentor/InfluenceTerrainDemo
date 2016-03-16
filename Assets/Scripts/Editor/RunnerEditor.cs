using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Tools;
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

        private Vector3? _cachedMapIntersection;
        private int _mapIntersectionCachedFrame = -1;
        private Vector2? _cachedLayoutIntersection;
        private int _layoutIntersectionCachedFrame = -1;
        private Vector3 _cameraPosition;

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
                DrawLandLayout();
                Vector3? cursorPosition;

                if (IsMapMode())
                    cursorPosition = GetMapIntersection();
                else
                    cursorPosition = Convert(GetLayoutIntersection());

                if (cursorPosition.HasValue)
                {
                    if (Event.current.shift)
                        ShowInfluenceInfo(cursorPosition.Value);
                    ShowCursorInfo(cursorPosition.Value);
                    ShowBlockInfo(cursorPosition.Value);
                    ShowChunkInfo(cursorPosition.Value);
                    ShowZoneInfo(cursorPosition.Value);
                }
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmos(Runner target, GizmoType gizmoType)
        {
            if(Application.isPlaying && _self != null)
                _self.DrawGizmos2();
        }

        private void DrawGizmos2()
        {
            //Draw land bounds
            DrawRectangle.ForGizmo(_target.LayoutBounds, Color.gray);

            //Get intersection points
            if (Application.isPlaying)
            {
                if (IsMapMode())
                {
                    //Draw cursor-map intersection
                    var mapInter = GetMapIntersection();
                    if (mapInter.HasValue)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(mapInter.Value, 0.05f);
                        DrawChunkAndBlock(Convert(mapInter.Value));

                        if(IsLayoutMode())
                            DrawSelectedZone(Convert(mapInter.Value));
                    }
                }
                else if (IsLayoutMode())
                {
                    var layoutHit = GetLayoutIntersection();
                    if (layoutHit.HasValue)
                    {
                        DrawSelectedZone(layoutHit.Value);
                        DrawChunkAndBlock(layoutHit.Value);
                    }
                }
            }
        }

        #endregion

        private void DrawChunkAndBlock(Vector2 worldPosition)
        {
            const float yOffset = 0.01f;

            if (IsMapMode())
            {
                var chunkPos = Chunk.GetPosition(worldPosition);
                Chunk chunk;
                if (_main.Map.Map.TryGetValue(chunkPos, out chunk))
                {
                    //Draw chunk bounds
                    var chunkBounds = (Bounds)Chunk.GetBounds(chunkPos);
                    
                    var r1 = new Vector3(chunkBounds.min.x, chunk.HeightMap[0, 0] + yOffset, chunkBounds.min.z);
                    var r2 = new Vector3(chunkBounds.max.x, chunk.HeightMap[chunk.GridSize - 1, 0] + yOffset, chunkBounds.min.z);
                    var r3 = new Vector3(chunkBounds.min.x, chunk.HeightMap[0, chunk.GridSize - 1] + yOffset, chunkBounds.max.z);
                    var r4 = new Vector3(chunkBounds.max.x, chunk.HeightMap[chunk.GridSize - 1, chunk.GridSize - 1] + yOffset,
                        chunkBounds.max.z);

                    DrawPolyline.ForGizmo(GetChunkPolyBound(chunk, yOffset), Color.red);

                    //Draw block bounds
                    var blockPos = (Vector2i) worldPosition;
                    var localPos = Chunk.GetLocalPosition(worldPosition);
                    r1 = new Vector3(blockPos.X, chunk.HeightMap[localPos.X, localPos.Z] + yOffset, blockPos.Z);
                    r2 = new Vector3(blockPos.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z] + yOffset, blockPos.Z);
                    r3 = new Vector3(blockPos.X, chunk.HeightMap[localPos.X, localPos.Z + 1] + yOffset, blockPos.Z + 1);
                    r4 = new Vector3(blockPos.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z + 1] + yOffset, blockPos.Z + 1);

                    DrawRectangle.ForDebug(r1, r2, r4, r3, Color.red);

                    //Draw block normal
                    var n1 = chunk.NormalMap[localPos.X, localPos.Z];
                    var blockCenter = (r1 + r2 + r3 + r4)/4;
                    DrawArrow.ForDebug(blockCenter, n1, Color.red);
                }
            }
            else                //Layout mode
            {
                //Draw chunk bounds
                var chunkPos = Chunk.GetPosition(worldPosition);
                var chunkBounds = Chunk.GetBounds(chunkPos);
                DrawRectangle.ForGizmo(chunkBounds, Color.red);

                //Draw block bounds
                DrawRectangle.ForGizmo(new Bounds2i((Vector2i) worldPosition, 1, 1), Color.red);
            }
        }

        private void DrawSelectedZone(Vector2 worldPosition)
        {
            //Calc zone under cursor
            if (_main.LandLayout != null && _main.LandLayout.Zones.Any() && _main.LandLayout.Bounds.Contains((Vector2i)worldPosition))
            {
                var selectedZone = _main.LandLayout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).First();
                var zoneColor = _target[selectedZone.Type].LandColor;

                Gizmos.color = zoneColor;

                //Draw zone bounds
                //foreach (var chunk in selectedZone.ChunkBounds)
                //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

                foreach (var chunk in _main.LandLayout.GetChunks(selectedZone))
                    DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

                foreach (var block in selectedZone.GetBlocks2())
                {
                    var blockBounds = new Bounds2i(block, 1, 1);
                    DrawRectangle.ForGizmo(blockBounds, zoneColor);
                }
            }
        }

        private void DrawLandLayout()
        {
            if (!IsLayoutMode())
                return;

            var zones = _layout.Zones.ToArray();
            Handles.matrix = Matrix4x4.identity;

            var newCenters = zones.Select(c => Convert(c.Center)).ToArray();

            for (int i = 0; i < zones.Count(); i++)
            {
                var zone = zones[i];
                Handles.color = _layout.Zones.ElementAt(i).Type != ZoneType.Empty
                    ? _target[zone.Type].LandColor
                    : Color.black;

                //Draw edges
                foreach (var edge in zone.Cell.Edges)
                    Handles.DrawAAPolyLine(Convert(edge.Vertex1), Convert(edge.Vertex2));

                //Draw fill
                Handles.color = new Color(Handles.color.r/2, Handles.color.g/2, Handles.color.b/2, Handles.color.a/2);
                foreach (var vert in zone.Cell.Vertices)
                    Handles.DrawLine(Convert(zone.Center), Convert(vert));

                newCenters[i] = Handles.Slider2D(newCenters[i], Vector3.forward, Vector3.forward, Vector3.right, 5,
                    Handles.SphereCap, 0);
            }

            //if (GUI.changed)
            //{
            //    _voronoi = CellMeshGenerator.Generate(newCenters.Select(c => Convert(c)), (Bounds)_target.LayoutBounds);
            //    for (int i = 0; i < newCenters.Length; i++)
            //        _layout[i] = new Zone(_voronoi[i], _layout[i].Type);
            //}
        }

        #region Info blocks

        private void ShowInfluenceInfo(Vector3 layoutPosition)
        {
            var influence = _layout.GetInfluence(Convert(layoutPosition));

            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Absolute");
                var labelText = String.Join("\n",
                    influence.Select(z => String.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                if (_target.InfluenceLimit == 0)
                    influence = influence.Pack(_target.InfluenceThreshold);
                else
                    influence = influence.Pack(_target.InfluenceLimit);

                GUILayout.Label("Packed");
                labelText = String.Join("\n",
                    influence.Select(z => String.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);
            };

            Handles.BeginGUI();
            GUILayout.Window(5, new Rect(Vector2.zero, Vector2.one * 200), windowFunc, "Influence");
            Handles.EndGUI();
        }

        private void ShowZoneInfo(Vector3 position)
        {
            //Calc zone under cursor
            if (_layout != null && _layout.Zones.Any())
            {
                var selectedPosition = Convert(position);
                var selectedZone = _layout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - selectedPosition)).First();

                GUI.WindowFunction windowFunc = id =>
                {
                    GUILayout.Label(string.Format("Id, type: {0} - {1}", selectedZone.Cell.Id, selectedZone.Type));
                    GUILayout.Label(string.Format("Is closed: {0}", selectedZone.Cell.IsClosed));
                };

                Handles.BeginGUI();
                GUILayout.Window(4, new Rect(0, Screen.height - 400, 200, 110), windowFunc, "Zone");
                Handles.EndGUI();
            }
        }

        private void ShowChunkInfo(Vector3 position)
        {
            var selectPoint = Convert(position);
            var chunkPos = Chunk.GetPosition(selectPoint);
            var chunkBounds = Chunk.GetBounds(chunkPos);
            var localPos = Chunk.GetLocalPosition(selectPoint);

            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Pos: " + chunkPos + " : " + localPos);
                GUILayout.Label("Bounds: " + chunkBounds.Min + "-" + chunkBounds.Max);
            };

            Handles.BeginGUI();
            GUILayout.Window(3, new Rect(0, Screen.height - 300, 200, 110), windowFunc, "Chunk");
            Handles.EndGUI();
        }

        private void ShowBlockInfo(Vector3 position)
        {
            GUI.WindowFunction windowFunc;
            if (IsMapMode())
            {
                var blockPos = (Vector2i)position;
                var block = _main.Map.GetBlock(blockPos);
                windowFunc = id =>
                {
                    GUILayout.Label("Pos: " + blockPos);
                    GUILayout.Label("Type: " + block.Type);
                    GUILayout.Label(string.Format("Height height: {0:F1}", block.Height));
                    GUILayout.Label(string.Format("Normal: {0}", block.Normal));
                    GUILayout.Label("Inclination: " + (int) Vector3.Angle(block.Normal, Vector3.up));
                };
            }
            else //Layout mode
            {
                var blockPos = (Vector2i)position;
                windowFunc = id =>
                {
                    GUILayout.Label("Pos: " + blockPos);
                };
            }

            Handles.BeginGUI();
            GUILayout.Window(1, new Rect(0, Screen.height - 220, 200, 110), windowFunc, "Block");
            Handles.EndGUI();
        }

        private void ShowCursorInfo(Vector3 position)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Cursor pos: " + position);
                GUILayout.Label(string.Format("Distance: {0:F1}", Vector3.Distance(_cameraPosition, position)));
            };

            Handles.BeginGUI();
            GUILayout.Window(0, new Rect(0, Screen.height - 100, 200, 90), windowFunc, "Cursor");
            Handles.EndGUI();
        }

        #endregion

        private bool IsLayoutMode()
        {
            return _main.Map == null ||
                   (SceneView.currentDrawingSceneView.camera.orthographic &&
                    Vector3.Angle(SceneView.currentDrawingSceneView.camera.transform.forward, Vector3.down) < 1);
        }

        private bool IsMapMode()
        {
            return _main.Map != null;
        }

        /// <summary>
        /// Get cursor-map intersection point
        /// </summary>
        /// <returns>null if cursor is not on map of map is not generated</returns>
        private Vector3? GetMapIntersection()
        {
            if (Time.frameCount != _mapIntersectionCachedFrame)
            {
                _mapIntersectionCachedFrame = Time.frameCount;
                _cachedMapIntersection = null;

                var pos = Event.current.mousePosition;
                if (Application.isPlaying && _main.Map != null && pos.x >= 0 && pos.x <= Screen.width && pos.y >= 0 && pos.y <= Screen.height)
                {
                    var worldRay = HandleUtility.GUIPointToWorldRay(pos);
                    _cachedMapIntersection = _main.Map.GetRayMapIntersection(worldRay);
                    _cameraPosition = worldRay.origin;
                }
            }

            return _cachedMapIntersection;
        }

        /// <summary>
        /// Get cursor-layout intersection point
        /// </summary>
        /// <returns>null if cursor is not on map of map is not generated</returns>
        private Vector2? GetLayoutIntersection()
        {
            if (Time.frameCount != _layoutIntersectionCachedFrame)
            {
                _layoutIntersectionCachedFrame = Time.frameCount;
                _cachedLayoutIntersection = null;

                var pos = Event.current.mousePosition;
                if (Application.isPlaying && pos.x <= Screen.width && pos.y >= 0 && pos.y <= Screen.height)
                {
                    var worldRay = HandleUtility.GUIPointToWorldRay(pos);
                    var worldPlane = new Plane(Vector3.up, Vector3.zero);
                    float hitDistance;
                    if (worldPlane.Raycast(worldRay, out hitDistance))
                    {
                        var layoutHitPoint3d = worldRay.GetPoint(hitDistance);
                        var layoutHitPoint = new Vector2(layoutHitPoint3d.x, layoutHitPoint3d.z);
                        if (_main.LandLayout.Bounds.Contains((Vector2i) layoutHitPoint))
                            _cachedLayoutIntersection = layoutHitPoint;
                        _cameraPosition = worldRay.origin;
                    }
                }
            }

            return _cachedLayoutIntersection;
        }

        private Vector3[] GetChunkPolyBound(Chunk chunk, float yOffset)
        {
            var result = new List<Vector3>();
            var chunkWorldPos = (Vector3)Chunk.GetBounds(chunk.Position).Min;

            for (int i = 0; i < chunk.GridSize; i++)
            {
                var localPos = new Vector3(0, chunk.HeightMap[0, i] + yOffset, i);
                result.Add(chunkWorldPos + localPos);
            }

            for (int i = 0; i < chunk.GridSize; i++)
            {
                var localPos = new Vector3(i, chunk.HeightMap[i, chunk.GridSize - 1] + yOffset, chunk.GridSize - 1);
                result.Add(chunkWorldPos + localPos);
            }

            for (int i = chunk.GridSize - 1; i >= 0; i--)
            {
                var localPos = new Vector3(chunk.GridSize - 1, chunk.HeightMap[chunk.GridSize - 1, i] + yOffset, i);
                result.Add(chunkWorldPos + localPos);
            }

            for (int i = chunk.GridSize - 1; i >= 0; i--)
            {
                var localPos = new Vector3(i, chunk.HeightMap[i, 0] + yOffset, 0);
                result.Add(chunkWorldPos + localPos);
            }

            return result.ToArray();
        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        private static Vector3? Convert(Vector2? v)
        {
            if (v.HasValue)
                return Convert(v.Value);
            else
                return null;
        }

        private static Vector3 Convert(Vector2i v)
        {
            return Convert((Vector2) v);
        }

        private static Vector2 Convert(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
    }
}
