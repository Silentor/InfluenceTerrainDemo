using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Tools;
using TerrainDemo.Layout;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor
    {
        private Runner _target;
        //private LandLayout _layout;
        private Main _main;
        private static RunnerEditor _self;

        private Vector3? _cachedMapIntersection;
        private int _mapIntersectionCachedFrame = -1;
        private Vector2? _cachedLayoutIntersection;
        private int _layoutIntersectionCachedFrame = -1;
        private Vector3 _cameraPosition;

        #region Unity

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                _target = (Runner)target;
                _main = _target.Main;
                _self = this;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && _target != null)
            {
                if (GUILayout.Button("Rebuild layout"))
                    _target.GenerateLayout();

                if (GUILayout.Button("Rebuild map"))
                    _target.GenerateMap();

                if (GUILayout.Button("Rebuild mesh"))
                    _target.GenerateMesh();

                if (GUILayout.Button("Save mesh"))
                    _target.SaveMesh();
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
                    cursorPosition = GetLayoutIntersection().ConvertTo3D();

                if (cursorPosition.HasValue)
                {
                    DrawSelectedCluster(cursorPosition.Value.ConvertTo2D());

                    if (Event.current.shift)
                        ShowInfluenceInfo(cursorPosition.Value);
                    ShowCursorInfo(cursorPosition.Value);
                    ShowBlockInfo(cursorPosition.Value);
                    ShowChunkInfo(cursorPosition.Value);
                    ShowZoneInfo(cursorPosition.Value);
                }

                ShowLayoutHints();

                //Dirty debug
                
                //if (Event.current.character != '')
                {
                    //Debug.Log(Event.current.character);

                    //var intersection = GetLayoutIntersection();
                    //if (intersection != null)
                    //{
                    //    _main.LandLayout.PrintInfluences(intersection.Value);
                    //}
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
            DrawRectangle.ForGizmo(_target.LandSettings.LandBounds, Color.gray);

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
                        DrawChunkAndBlock(mapInter.Value.ConvertTo2D());

                        if(IsLayoutMode())
                            DrawSelectedZone(mapInter.Value.ConvertTo2D());

                        return;
                    }
                }

                if (IsLayoutMode())
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

                    DrawPolyline.ForGizmo(GetChunkPolyBound(chunk, yOffset), Color.red, false);

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
                var selectedZone = _main.LandLayout.GetZoneFor(worldPosition, false);
                var zoneColor = _target.LandSettings[selectedZone.Type].LandColor;

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

        /// <summary>
        /// Draw basic land layout (land border, zones, clusters)
        /// </summary>
        private void DrawLandLayout()
        {
            if (!IsLayoutMode())
                return;

            var layout = _main.LandLayout;
            var zones = layout.Zones.ToArray();
            Handles.matrix = Matrix4x4.identity;

            var newCenters = zones.Select(c => c.Center.ConvertTo3D()).ToArray();

            for (int i = 0; i < zones.Count(); i++)
            {
                var zone = zones[i];
                Handles.color = zone.Type != ZoneType.Empty
                    ? _target.LandSettings[zone.Type].LandColor
                    : Color.black;

                //Draw zone edges
                foreach (var edge in zone.Cell.Edges)
                    Handles.DrawAAPolyLine(1, edge.Vertex1.ConvertTo3D(), edge.Vertex2.ConvertTo3D());

                //Draw zone fill
                if (_target.ShowFill)
                {
                    Handles.color = new Color(Handles.color.r / 2, Handles.color.g / 2, Handles.color.b / 2,
                        Handles.color.a / 2);
                    foreach (var vert in zone.Cell.Vertices)
                        Handles.DrawLine(zone.Center.ConvertTo3D(), vert.ConvertTo3D());
                }

                /*
                newCenters[i] = Handles.Slider2D(newCenters[i], Vector3.forward, Vector3.forward, Vector3.right, 5,
                    Handles.SphereCap, 0);
                    */

                //Draw Delaunay triangles
                if (_target.ShowDelaunay)
                {
                    Handles.color = new Color(1, 0, 0, 0.5f);
                    foreach (var cellNeighbor in zone.Cell.Neighbors)
                        Handles.DrawLine(zone.Center.ConvertTo3D(), cellNeighbor.Center.ConvertTo3D());
                }
            }

            //Draw visible zones
            var observerPos = _target.Observer.Position.ConvertTo2D();
            var visibleCells = layout.CellMesh.GetCellsFor(observerPos, _target.Observer.Range);
            foreach (var visibleCell in visibleCells)
                DrawCell.ForDebug(visibleCell, Color.white);
        }

        private void DrawSelectedCluster(Vector2 worldPosition)
        {
            if (IsLayoutMode())
            {
                var selectedZone = _main.LandLayout.GetZoneFor(worldPosition, false);
                var zoneColor = _target.LandSettings[selectedZone.Type].LandColor;

                var clusterCells = _main.LandLayout.GetCluster(selectedZone).Select(zl => zl.Cell).ToArray();
                var cluster = new CellMesh.Submesh(_main.LandLayout.CellMesh, clusterCells);

                //Get cluster border
                //Get unconnected edges
                var outerEdges =
                    cluster.GetBorderCells()
                        .SelectMany(c => c.Edges)
                        .Where(e => !cluster.Cells.Contains(e.Neighbor))
                        .ToArray();

                Handles.color = zoneColor;
                foreach (var edge in outerEdges)
                    //Not very efficient to call native method for every single edge todo prepare entire border polyline
                    Handles.DrawAAPolyLine(6, edge.Vertex1.ConvertTo3D(), edge.Vertex2.ConvertTo3D());
            }
        }

        #region Info blocks

        private void ShowInfluenceInfo(Vector3 layoutPosition)
        {
            var layout = _main.LandLayout;
            var influence = layout.GetInfluence(layoutPosition.ConvertTo2D());

            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Absolute");
                var labelText = String.Join("\n",
                    influence.Select(z => String.Format("[{0}] - {1}", z.Zone, z.Value)).ToArray());
                GUILayout.TextArea(labelText);

                if (_target.LandSettings.InfluenceLimit == 0)
                    influence = influence.Pack(_target.LandSettings.InfluenceThreshold);
                else
                    influence = influence.Pack(_target.LandSettings.InfluenceLimit);

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
            var layout = _main.LandLayout;
            //Calc zone under cursor
            if (layout != null && layout.Zones.Any())
            {
                var selectedPosition = position.ConvertTo2D();
                var selectedZone = layout.Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - selectedPosition)).First();

                GUI.WindowFunction windowFunc = id =>
                {
                    GUILayout.Label(string.Format("Id, cluster, type: {0} - {1} - {2}", selectedZone.Cell.Id, selectedZone.ClusterId, selectedZone.Type));
                    GUILayout.Label(string.Format("Is closed: {0}", selectedZone.Cell.IsClosed));
                };

                Handles.BeginGUI();
                GUILayout.Window(4, new Rect(0, Screen.height - 400, 200, 110), windowFunc, "Zone");
                Handles.EndGUI();
            }
        }

        private void ShowChunkInfo(Vector3 position)
        {
            var selectPoint = position.ConvertTo2D();
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

        #region HUD

        private void ShowLayoutHints()
        {
            if (Application.isPlaying && IsLayoutMode())
            {
                GUIStyle style = new GUIStyle {normal = {textColor = Color.white}};

                //Draw zone id's
                foreach (var zone in _main.LandLayout.Zones)
                    Handles.Label(zone.Center.ConvertTo3D(), zone.Cell.Id.ToString(), style);
            }
        }

        #endregion


        private bool IsLayoutMode()
        {
            var topDownView = SceneView.currentDrawingSceneView.camera.orthographic &&
                              Vector3.Angle(SceneView.currentDrawingSceneView.camera.transform.forward, Vector3.down) <
                              1;
            return _main.LandLayout != null && (_main.Map == null || topDownView);
        }

        private bool IsMapMode()
        {
            return _main.Map.Map.Count > 0;
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
    }
}
