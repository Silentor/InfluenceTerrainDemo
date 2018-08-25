using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;


namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor
    {
        private Runner _runner;
        //private LandLayout _layout;
        private Main _main;
        private static RunnerEditor _self;

        private LandSettings _settings;
        private Vector3? _mapIntersection;
        private Vector2? _layoutIntersection;
        private float _mapIntersectionDistance;
        private float _layoutIntersectionDistance;

        private Ray _userInputRay;
        private Vector2i? _selectedBlock;
        private Vector2i? _selectedChunk;
        private ZoneLayout _selectedZone;
        private ClusterLayout _selectedCluster;

        //private Dictionary<string, string[]> _infoBlocks = new Dictionary<string, string[]>();
        private Rect _cursorInfoRect;
        private Rect _blockInfoRect;
        private Rect _chunkInfoRect;
        private Rect _zoneInfoRect;
        private Rect _clusterInfoRect;
        private Rect _influenceInfoRect;
        private GUIStyle _zonesIdLabelStyle;
        private Vector2[] _clusterCenters;

        const float DecalOffset = 0.01f;

        #region Unity

        void Awake()
        {
            var sceneViewSize = EditorWindow.GetWindow<SceneView>().camera.pixelRect;
            _cursorInfoRect = new Rect(0, sceneViewSize.height - 50, 0, 0);
            _blockInfoRect = new Rect(0, _cursorInfoRect.y - 120, 0, 0);
            _chunkInfoRect = new Rect(0, _blockInfoRect.y - 80, 0, 0);
            _zoneInfoRect = new Rect(0, _chunkInfoRect.y - 90, 0, 0);
            _clusterInfoRect = new Rect(0, _zoneInfoRect.y - 90, 0, 0);
            _influenceInfoRect = new Rect(0, _clusterInfoRect.y - 90, 0, 0);
        }

        void OnEnable()
        {
            _runner = (Runner)target;
            _self = this;

            if (Application.isPlaying)
            {
                _main = _runner.Main;
                _settings = _runner.LandSettings;
            }
            else
            {
                _settings = _runner.GetComponent<LandSettings>();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && _runner != null)
            {
                if (GUILayout.Button("Rebuild layout"))
                    _runner.GenerateLayout();

                if (GUILayout.Button("Rebuild map"))
                    _runner.GenerateMap();

                if (GUILayout.Button("Rebuild mesh"))
                    _runner.GenerateMesh();

                GUILayout.BeginHorizontal();
                if (_main.LandLayout != null && GUILayout.Button("Save mesh"))
                {
                    var fileName = string.Format("cellmesh{0}.json", _settings.Seed);
                    var savePath = EditorUtility.SaveFilePanelInProject("Save cellmesh", fileName, "json",
                        "Save current cellmesh to file");
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        _runner.SaveMesh(savePath);
                    }
                }

                if (_main.LandLayout != null && GUILayout.Button("Load mesh"))
                {
                    var loadPath = EditorUtility.OpenFilePanelWithFilters("Load cellmesh", Application.dataPath,
                        new[] {"Cellmesh data", "json"});
                    if (!string.IsNullOrEmpty(loadPath))
                    {
                        _runner.LoadMesh(loadPath);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        void OnSceneGUI()
        {
            UpdateUserInput();

            //Most basic land properties
            DrawLandBound(_settings.LandBounds);



            if (LandState < LandState.Mesh || IsTopDown)
                DrawChunkGrid(_settings.LandBounds);

            if (_selectedBlock.HasValue && _selectedChunk.HasValue)
            {
                DrawSelectedChunk(_selectedChunk.Value);
                DrawSelectedBlock(_selectedBlock.Value);
            }

            if (LandState == LandState.Layout || LandState == LandState.Map ||
                (LandState == LandState.Mesh && IsTopDown))
            {
                DrawLandLayout();

                if(RunnerEditorMainMenu.IsShowCellId)
                    DrawZonesId();

                if (RunnerEditorMainMenu.IsShowClusterId)
                    DrawClusterId();

                if (_selectedZone != null)
                {
                    DrawSelectedZone(_selectedZone);
                }

                if(_selectedCluster != null)
                    DrawSelectedCluster(_selectedCluster);
            }

            if (LandState >= LandState.Layout)
                DrawBaseHeightPoints(_main.LandLayout);

            if (LandState == LandState.Mesh && _mapIntersection.HasValue)
            {
                ShowCursorInfo(_mapIntersection.Value);
                DrawCursor(_mapIntersection.Value);
            }
            else if (_layoutIntersection.HasValue)
                ShowCursorInfo(_layoutIntersection.Value);

            if (_selectedBlock.HasValue)
            {
                if (LandState >= LandState.Map)
                {
                    var block = _main.Map.GetBlock(_selectedBlock.Value);
                    ShowBlockInfo(block);
                }
                else
                {
                    ShowBlockInfo(BlockInfo.Empty, _selectedBlock);
                }

                ShowChunkInfo(null, _selectedBlock.Value);
            }

            if (_selectedZone != null)
                ShowZoneInfo(_selectedZone);

            if(_selectedCluster != null)
                ShowClusterInfo(_selectedCluster);

            if (LandState >= LandState.Layout && _layoutIntersection.HasValue)
            {
                var influence = _main.LandLayout.GetInfluence(_layoutIntersection.Value);
                ShowInfluenceInfo(influence);
            }
        }

        #endregion

        #region Debug graphics

        private void DrawSelectedChunk(Vector2i chunkPosition)
        {
            if(LandState <= LandState.Map)
                DrawRectangle.ForHandle(Chunk.GetBounds(chunkPosition), Color.red);
            else
            {
                Chunk selectedChunk;
                if (_main.Map.Chunks.TryGetValue(chunkPosition, out selectedChunk))
                {
                    DrawPolyline.ForDebug(GetChunkDecalBound(selectedChunk, DecalOffset), Color.red, false);
                }
            }
        }

        private void DrawSelectedBlock(Vector2i blockPosition)
        {
            if (LandState <= LandState.Map)
                DrawRectangle.ForHandle(BlockInfo.GetBounds(blockPosition), Color.red);
            else
            {
                //Draw block bounds
                var localPos = Chunk.GetLocalPosition(blockPosition);
                var chunkPos = Chunk.GetPositionFromBlock(blockPosition);

                Chunk chunk;
                if (_main.Map.Chunks.TryGetValue(chunkPos, out chunk))
                {
                    var r1 = new Vector3(blockPosition.X, chunk.HeightMap[localPos.X, localPos.Z] + DecalOffset, blockPosition.Z);
                    var r2 = new Vector3(blockPosition.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z] + DecalOffset, blockPosition.Z);
                    var r3 = new Vector3(blockPosition.X, chunk.HeightMap[localPos.X, localPos.Z + 1] + DecalOffset, blockPosition.Z + 1);
                    var r4 = new Vector3(blockPosition.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z + 1] + DecalOffset, blockPosition.Z + 1);

                    DrawRectangle.ForDebug(r1, r2, r4, r3, Color.red);

                    //Draw block normal
                    var n1 = chunk.NormalMap[localPos.X, localPos.Z];
                    var blockCenter = (r1 + r2 + r3 + r4) / 4;
                    Debug.DrawRay(blockCenter, n1, new Color(1f, 0f, 0f, 0.2f));
                }
            }
        }

        void DrawCursor(Vector3 cursorPosition)
        {
            DebugExtension.DebugPoint(cursorPosition, Color.red, 0.3f);
        }

        private void DrawSelectedZone([NotNull] ZoneLayout zone)        //todo pass zone
        {
            if (zone == null) throw new ArgumentNullException("zone");

            //Calc zone under cursor

            //var zoneColor = _settings[zone.Type].LandColor;

            //var zonePoints = new Vector3[zone.Cell.Vertices.Length + 1];
            //for (var i = 0; i < zone.Cell.Vertices.Length; i++)
              //  zonePoints[i] = zone.Cell.Vertices[i].ConvertTo3D();
            //zonePoints[zonePoints.Length - 1] = zone.Cell.Vertices.First().ConvertTo3D();
            //Handles.color = Color.red;
            //Handles.DrawPolyLine(zonePoints);

            //Draw zone bounds
            //foreach (var chunk in selectedZone.ChunkBounds)
            //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

            //foreach (var chunk in _main.LandLayout.GetChunks(selectedZone))
                //DrawRectangle.ForGizmo(Chunk.GetBounds(chunk), zoneColor);

            //foreach (var block in selectedZone.GetBlocks2())
            //{
                //var blockBounds = new Bounds2i(block, 1, 1);
                //DrawRectangle.ForGizmo(blockBounds, zoneColor);
            //}
        }

        /// <summary>
        /// Draw basic land layout: zones and clusters
        /// </summary>
        private void DrawLandLayout()
        {
            var layout = _main.LandLayout;
            var zones = layout.Zones.ToArray();
            Handles.matrix = Matrix4x4.identity;

            foreach (var cluster in layout.Clusters)
            {
                Handles.color = _settings[cluster.Type].LandColor;

                //Draw zones
                var zonesSegments = new List<Vector3>();                //todo consider cache and remove duplicate segments
                foreach (var zoneLayout in cluster.Zones)
                {
                    foreach (var cellEdge in zoneLayout.Face.Edges)
                    {
                        //zonesSegments.Add(cellEdge.Vertex1.ConvertTo3D());
                        //zonesSegments.Add(cellEdge.Vertex2.ConvertTo3D());
                    }

                    if (RunnerEditorMainMenu.IsShowFill)
                    {
                        //foreach (var cellVertex in zoneLayout.Cell.Vertices)
                        {
                            //zonesSegments.Add(zoneLayout.Center.ConvertTo3D());
                            //zonesSegments.Add(cellVertex.ConvertTo3D());
                        }
                    }
                }

                Handles.DrawLines(zonesSegments.ToArray());

                //Draw cluster, brute force implementation todo optimize
                foreach (var clusterEdge in cluster.Edges)
                {
                    Handles.DrawAAPolyLine(6, clusterEdge.Vertex1.ConvertTo3D(), clusterEdge.Vertex2.ConvertTo3D());
                }
            }

            //Draw Delaunay triangles
            if (RunnerEditorMainMenu.IsShowDelaunay)
            {
                var delaunaySegments = new List<Vector3>();
                foreach (var zone in layout.Zones)
                {
                    foreach (var cellNeighbor in zone.Face.Neighbors)
                    {
                        delaunaySegments.Add(zone.Center.ConvertTo3D());
                        //delaunaySegments.Add(cellNeighbor.Center.ConvertTo3D());
                    }
                }

                Handles.color = Color.gray;
                Handles.DrawLines(delaunaySegments.ToArray());                
            }

            //Draw visible zones

//            var observerPos = _runner.Observer.Position.ConvertTo2D();
  //          var visibleCells = layout.CellMesh.GetCellsFor(observerPos, _runner.Observer.Range);
    //        foreach (var visibleCell in visibleCells)
      //          DrawCell.ForDebug(visibleCell, Color.white);
        }

        private void DrawSelectedCluster(ClusterLayout cluster)
        {
            Handles.color = Color.red;
            foreach (var edge in cluster.Edges)
                //Not very efficient to call native method for every single edge todo prepare entire border polyline
            {
                Handles.DrawAAPolyLine(3, edge.Vertex1.ConvertTo3D(), edge.Vertex2.ConvertTo3D());
            }
        }

        private void DrawBaseHeightPoints(LandLayout layout)
        {
            Handles.color = Color.red;
            foreach (var heightPoint in layout.Heights)
                Handles.SphereCap(0, heightPoint, Quaternion.identity, 2);
        }

        private void DrawLandBound(Bounds2i bounds)
        {
            DrawRectangle.ForDebug(bounds, Color.gray);
        }

        /// <summary>
        /// Draw chunks grid inside land bounds
        /// </summary>
        /// <param name="bounds"></param>
        private void DrawChunkGrid(Bounds2i bounds)
        {
            Gizmos.color = Color.gray;
            var corner1 = new Vector3(bounds.Min.X, 0, bounds.Min.Z);
            var corner2 = new Vector3(bounds.Min.X, 0, bounds.Max.Z + 1);
            var corner3 = new Vector3(bounds.Max.X + 1, 0, bounds.Max.Z + 1);
            var corner4 = new Vector3(bounds.Max.X + 1, 0, bounds.Min.Z);

            var segments = new List<Vector3>();

            for (var x = corner1.x; x < corner4.x; x += Chunk.Size)
            {
                segments.Add(new Vector3(x, 0, corner1.z));
                segments.Add(new Vector3(x, 0, corner2.z));
            }

            for (var z = corner1.z; z < corner2.z; z += Chunk.Size)
            {
                segments.Add(new Vector3(corner1.x, 0, z));
                segments.Add(new Vector3(corner4.x, 0, z));
            }

            Handles.color = Color.gray;
            Handles.DrawLines(segments.ToArray());
        }

        #endregion

        #region Info blocks

        private void ShowInfluenceInfo(ZoneRatio influence)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Absolute");
                var labelText = String.Join("\n",
                    influence.Select(z => String.Format("[{0}] - {1:F3}", z.Zone, z.Value)).ToArray());
                GUILayout.Label(labelText);

                if (_runner.LandSettings.InfluenceLimit == 0)
                    influence = influence.Pack(_runner.LandSettings.InfluenceThreshold);
                else
                    influence = influence.Pack(_runner.LandSettings.InfluenceLimit);

                GUILayout.Label("Packed");
                labelText = String.Join("\n",
                    influence.Select(z => String.Format("[{0}] - {1:F3}", z.Zone, z.Value)).ToArray());
                GUILayout.Label(labelText);
                GUI.DragWindow();
            };

            Handles.BeginGUI();
            _influenceInfoRect.width = 0;
            _influenceInfoRect.height = 0;
            _influenceInfoRect = GUILayout.Window(6, _influenceInfoRect, windowFunc, "Influence");
            Handles.EndGUI();
        }

        private void ShowZoneInfo(ZoneLayout zone)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label(string.Format("Id, type: {0} - {1}", zone.Face.Id, zone.Type));
                GUILayout.Label(string.Format("Cluster: {0}", zone.ClusterId));
                //GUILayout.Label(string.Format("Is closed: {0}", zone.Cell.IsClosed));
            };

            Handles.BeginGUI();
            _zoneInfoRect.width = 0;
            _zoneInfoRect.height = 0;
            _zoneInfoRect = GUILayout.Window(4, _zoneInfoRect, windowFunc, "Zone");
            Handles.EndGUI();
        }

        private void ShowClusterInfo(ClusterLayout cluster)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label(string.Format("Id, type: {0} - {1}", cluster.Id, cluster.Type));
                GUILayout.Label(string.Format("Zones: {0}", cluster.Zones.Count()));
                //GUILayout.Label(string.Format("NeighborsSafe: {0}", string.Join(", ", cluster.NeighborsSafe.Select(c => c.Id.ToString()).ToArray())));
            };

            Handles.BeginGUI();
            _clusterInfoRect.width = 0;
            _clusterInfoRect.height = 0;
            _clusterInfoRect = GUILayout.Window(5, _clusterInfoRect, windowFunc, "Cluster");
            Handles.EndGUI();
        }

        private void ShowChunkInfo(Chunk chunk, Vector2i selectedBlockPosition)
        {
            var chunkPos = chunk != null ? chunk.Position : Chunk.GetPositionFromBlock(selectedBlockPosition);
            var chunkBounds = Chunk.GetBounds(chunkPos);
            var localPos = Chunk.GetLocalPosition(selectedBlockPosition);

            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Pos: " + chunkPos + " : " + localPos);
                GUILayout.Label("Bounds: " + chunkBounds.Min + "-" + chunkBounds.Max);
                GUI.DragWindow();
            };

            Handles.BeginGUI();
            _chunkInfoRect.width = 0;
            _chunkInfoRect.height = 0;
            _chunkInfoRect = GUILayout.Window(3, _chunkInfoRect, windowFunc, "Chunk");
            Handles.EndGUI();
        }

        private void ShowBlockInfo(BlockInfo block, Vector2i? blockPosition = null)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                if (blockPosition == null)
                {
                    GUILayout.Label("Pos: " + block.Position);
                    GUILayout.Label("Type: " + block.Type);
                    GUILayout.Label(string.Format("Height: {0:F1}", block.Height));
                    GUILayout.Label(string.Format("Normal: {0}", block.Normal));
                    GUILayout.Label("Inclination: " + (int) Vector3.Angle(block.Normal, Vector3.up));
                }
                else
                {
                    GUILayout.Label("Pos: " + blockPosition.Value);
                }
                GUI.DragWindow();
            };

            Handles.BeginGUI();
            _blockInfoRect.width = 0;
            _blockInfoRect.height = 0;
            _blockInfoRect = GUILayout.Window(1, _blockInfoRect, windowFunc, "Block");
            Handles.EndGUI();
        }

        private void ShowCursorInfo(Vector3 cursorPosition)
        {
            GUI.WindowFunction windowFunc = id =>
            {
                GUILayout.Label("Pos: " + cursorPosition);
                GUILayout.Label(string.Format("Distance: {0:F1}", Vector3.Distance(_userInputRay.origin, cursorPosition)));
                GUI.DragWindow();
            };

            Handles.BeginGUI();
            _cursorInfoRect.width = 0;
            _cursorInfoRect.height = 0;
            _cursorInfoRect = GUILayout.Window(0, _cursorInfoRect, windowFunc, "Cursor");
            Handles.EndGUI();
        }

        #endregion

        #region Texts

        private void DrawZonesId()
        {
            if(_zonesIdLabelStyle == null)
                _zonesIdLabelStyle = new GUIStyle {normal = {textColor = Color.white}};

            //Draw zone id's
            foreach (var zone in _main.LandLayout.Zones)
                Handles.Label(zone.Center.ConvertTo3D(), zone.Face.Id.ToString(), _zonesIdLabelStyle);
        }

        private void DrawClusterId()
        {
            if (_zonesIdLabelStyle == null)
                _zonesIdLabelStyle = new GUIStyle { normal = { textColor = Color.white } };

            UpdateClustersCenter();

            //Draw cluster id's
            foreach (var cluster in _main.LandLayout.Clusters)
            {
                var clusterCenter = _clusterCenters[cluster.Id];           
                Handles.Label(clusterCenter.ConvertTo3D(), cluster.Id.ToString(), _zonesIdLabelStyle);
            }
        }

        #endregion




        private LandState LandState
        {
            get
            {
                if(!Application.isPlaying)
                    return LandState.None;

                if(_main.VisualizedChunks.Count > 0)
                    return LandState.Mesh;

                if(_runner.Main.Map.Chunks.Count > 0)
                    return LandState.Map;

                return LandState.Layout;
            }
        }

        private bool IsTopDown
        {
            get
            {
                return SceneView.currentDrawingSceneView.camera.orthographic &&
                              Vector3.Angle(SceneView.currentDrawingSceneView.camera.transform.forward, Vector3.down) <
                              1;
            }
        }

        /// <summary>
        /// Update actual user input: mouse ray, selected objects etc
        /// </summary>
        private void UpdateUserInput()
        {
            var pos = Event.current.mousePosition;
            var userInputRay = HandleUtility.GUIPointToWorldRay(pos);

            //Update only if mouse is used
            if (userInputRay.origin != _userInputRay.origin || userInputRay.direction != _userInputRay.direction)
            {
                _userInputRay = userInputRay;

                //Invalidate all
                _mapIntersection = _layoutIntersection = null;
                _mapIntersectionDistance = _layoutIntersectionDistance = float.PositiveInfinity;
                _selectedBlock = _selectedChunk = null;
                _selectedZone = null;
                _selectedCluster = null;

                //Update user input intersection points and distances
                if (LandState == LandState.Mesh)
                {
                    UpdateMapIntersection(userInputRay);
                }
                else
                {
                    UpdateLayoutIntersection(userInputRay);
                }

                //Calc selected block, chunk, zone and cluster
                if (_layoutIntersection.HasValue)
                {
                    _selectedBlock = (Vector2i) _layoutIntersection.Value;
                    _selectedChunk = Chunk.GetPositionFromBlock(_selectedBlock.Value);

                    if (LandState >= LandState.Layout)
                    {
                        _selectedZone = _main.LandLayout.GetZoneFor(_layoutIntersection.Value);
                        _selectedCluster = _main.LandLayout.GetClusterFor(_selectedZone);
                    }
                }
            }
        }

        /// <summary>
        /// Get cursor-map intersection point
        /// </summary>
        /// <returns>null if cursor is not on map of map is not generated</returns>
        private void UpdateMapIntersection(Ray worldRay)
        {
            _mapIntersection = _main.Map.GetRayMapIntersection(worldRay);
            if (_mapIntersection.HasValue)
            {
                _mapIntersectionDistance = Vector3.Distance(worldRay.origin, _mapIntersection.Value);
                _layoutIntersection = new Vector2(_mapIntersection.Value.x, _mapIntersection.Value.z);
                _layoutIntersectionDistance = Vector3.Distance(worldRay.origin, _layoutIntersection.Value.ConvertTo3D());
            }
            else
                UpdateLayoutIntersection(worldRay);
        }

        /// <summary>
        /// Get cursor-layout intersection point
        /// </summary>
        /// <returns>null if cursor is not on map of map is not generated</returns>
        private void UpdateLayoutIntersection(Ray worldRay)
        {
            var worldPlane = new Plane(Vector3.up, Vector3.zero);
            float hitDistance;
            if (worldPlane.Raycast(worldRay, out hitDistance))
            {
                var layoutHitPoint3d = worldRay.GetPoint(hitDistance);
                var layoutHitPoint = new Vector2(layoutHitPoint3d.x, layoutHitPoint3d.z);
                if (_settings.LandBounds.Contains((Vector2i) layoutHitPoint))
                {
                    _layoutIntersection = layoutHitPoint;
                    _layoutIntersectionDistance = hitDistance;
                }
            }
        }

        /// <summary>
        /// Get chunk bound decal rectangle
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        private Vector3[] GetChunkDecalBound(Chunk chunk, float yOffset)
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

        private void UpdateClustersCenter()
        {
            if (_clusterCenters == null)
            {
                _clusterCenters = new Vector2[_main.LandLayout.Clusters.Count()];
                for (int i = 0; i < _clusterCenters.Length; i++)
                {
                    var cluster = _main.LandLayout.Clusters.ElementAt(i);
                    //Get most centered cell of cluster
                    var borders = cluster.Mesh.GetBorderCells().ToArray();
                    var floodFill = cluster.Mesh.FloodFill(borders);
                    /*
                    Cell[] mostCenteredCells = floodFill.GetNeighbors(0).ToArray();
                    for (int floodFillStep = 1; floodFillStep < 10; floodFillStep++)
                    {
                        var floodFillResult = floodFill.GetNeighbors(floodFillStep).ToArray();
                        if (!floodFillResult.Any())
                            break;
                        else
                            mostCenteredCells = floodFillResult;
                    }
                    */

                    //Find most centered cell by geometrical center distance;
                    Vector2 center = Vector2.zero;
                    //foreach (var clusterCell in cluster.Mesh)
                        //center += clusterCell.Center;
                    //center /= cluster.Mesh.Cells.Count();

                    //Select from mostCenteredCells nearest to geomertical center
                    float distance = float.MaxValue;
                    Cell centerCell = null;

                    /*
                    for (int j = 0; j < mostCenteredCells.Length; j++)
                    {
                        var dist = Vector2.Distance(mostCenteredCells[j].Center, center);
                        if (dist < distance)
                        {
                            distance = dist;
                            centerCell = mostCenteredCells[j];
                        }
                    }
                    */

                    _clusterCenters[i] = centerCell.Center;
                }
            }
        }
    }

    /// <summary>
    /// Land generation stage
    /// </summary>
    public enum LandState
    {
        /// <summary>
        /// None generated at all
        /// </summary>
        None,
        /// <summary>
        /// Layout generated
        /// </summary>
        Layout,
        /// <summary>
        /// Heightmap generated
        /// </summary>
        Map,
        /// <summary>
        /// Mesh generated and visualized
        /// </summary>
        Mesh
    }
}
