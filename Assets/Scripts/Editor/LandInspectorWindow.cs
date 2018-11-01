using System;
using System.Linq;
using System.Xml;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEditor;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Editor
{
    public class LandInspectorWindow : EditorWindow
    {
        public Color Inactive = Color.gray;
        public Color Active = Color.white;

        private TriRunner _runner;
        private MacroMap MacroMap => _runner ? _runner.Macro : null;
        private MicroMap MicroMap => _runner ? _runner.Micro : null;

        private MacroTemplate _land;
        private Vector2 _sceneViewScreenPosition;
        private Input _input;
        private bool _enabled = true;
        private bool _drawLayout;
        private bool _isShowInfluenceDebug;
        private bool _drawMicro;
        private bool _showZonesList;

        private Renderer.MicroRenderMode _microRenderMode = Renderer.MicroRenderMode.Default;

        [MenuItem("Land/Inspector")]
        static void Init()
        {
            var window = (LandInspectorWindow)GetWindow(typeof(LandInspectorWindow));
            window.titleContent = new GUIContent("Land Inspector");
            window.Show();
        }

        private Input PrepareInput(Vector2 sceneScreenPosition)
        {
            Input result = new Input
            {
                IsWorldPlaneSelected = false,
                ViewPoint = SceneView.currentDrawingSceneView.camera.transform.position,
            };

            var userInputRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(sceneScreenPosition);
            var layoutPlane = new Plane(Vector3.up, Vector3.zero);

            if (MacroMap != null)
            {
                var distance = 0f;
                if (layoutPlane.Raycast(userInputRay, out distance))
                {
                    result.IsWorldPlaneSelected = true;
                    var layoutPoint = (OpenTK.Vector2)userInputRay.GetPoint(distance);
                    result.WorldPosition = layoutPoint;
                    result.Distance = distance;
                    result.SelectedMacroCell = MacroMap.GetCellAt(layoutPoint);
                    result.SelectedVert = MacroMap.Vertices.FirstOrDefault(v => OpenTK.Vector2.Distance(v.Position, layoutPoint) < 1);

                    if (result.SelectedMacroCell != null)
                        result.SelectedMicroCell = MicroMap.GetCell(result.SelectedMacroCell);

                    if (MicroMap != null)
                    {
                        var blockPos = (Vector2i) result.WorldPosition;
                        if (MicroMap.Bounds.Contains(blockPos))
                        {
                            result.BlockPosition = blockPos;
                            result.IsBlockSelected = true;
                        }
                    }
                }
            }

            return result;
        }

        private void DrawMacroCell(Cell cell)
        {
            DrawMacroCell(cell, cell.Biome != null ? cell.Biome.LayoutColor : Inactive, false);
        }

        private void DrawMacroCell(Cell cell, Color color, bool labelVertices)
        {
            Handles.color = color;
            var isRelief = _runner.MacroCellReliefVisualization == Renderer.MacroCellReliefMode.Rude;

            Handles.DrawPolyLine(
                VertexToPosition(cell.Vertices[0], isRelief),
                VertexToPosition(cell.Vertices[1], isRelief),
                VertexToPosition(cell.Vertices[2], isRelief),
                VertexToPosition(cell.Vertices[3], isRelief),
                VertexToPosition(cell.Vertices[4], isRelief),
                VertexToPosition(cell.Vertices[5], isRelief),
                VertexToPosition(cell.Vertices[0], isRelief));

            var cellDistance = Vector3.Distance(cell.Center, SceneView.lastActiveSceneView.camera.transform.position);
            var fontSize = Mathf.RoundToInt(-cellDistance * 1 / 8 + 15);

            if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
            {
                var style = new GUIStyle(GUI.skin.label);
                var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
                style.normal.textColor = contrastColor;
                style.fontSize = fontSize;
                Handles.Label( CellToPosition(cell, isRelief), cell.Coords.ToString(), style);
                Handles.color = contrastColor;
                Handles.DrawWireDisc(CellToPosition(cell, isRelief), Vector3.up, 0.1f);

                if (labelVertices)
                {
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[0], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[0].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[1], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[1].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[2], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[2].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[3], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[3].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[4], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[4].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[5], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices[5].Id.ToString(), style);
                }
            }

        }

        private void DrawMicroCell(Micro.Cell cell, Color color)
        {
            Handles.color = color;
            foreach (var block in cell.BlockPositions)
            {
                DrawBlock(block, color);
            }
        }

        private void DrawLandBounds(Box2 bounds, Color color)
        {
            DrawRectangle.ForHandle(bounds, color);
        }

        private void DrawTriVert(MacroVert vert)
        {
            Handles.color = Color.white;
            Handles.DrawWireDisc(vert.Position, Vector3.up, 1);
        }

        private void DrawBlock(Vector2i position, Color color, uint width = 0, Vector3? normal = null)
        {
            Handles.color = color;
            DrawRectangle.ForHandle(BlockInfo.GetBounds(position), color, width);

            if (normal.HasValue)
                DrawArrow.ForDebug(BlockInfo.GetCenter(position), normal.Value);
        }

        /// <summary>
        /// Draw zone border
        /// </summary>
        /// <param name="zone"></param>
        private void DrawMacroZone(Macro.Zone zone, Color color)
        {
            Handles.color = color;
            var isRelief = _runner.MacroCellReliefVisualization == Renderer.MacroCellReliefMode.Rude;

            //Get zone outer border
            foreach (var edge in zone.Edges)
                Handles.DrawLine(VertexToPosition(edge.Vertex1, isRelief), VertexToPosition(edge.Vertex2, isRelief));
        }

        #region Window content

        private void ShowSettings()
        {
            
        }

        private void ShowMacroZoneInfo(Macro.Zone zone)
        {
            GUILayout.Label($"Macro.Zone {zone.Id}", EditorStyles.boldLabel);
            GUILayout.Label($"Cells: {zone.Cells.ToJoinedString(c => c.Coords.ToString())}");
            GUILayout.Label($"Biome: {zone.Biome.name}");
        }

        private void ShowMacroCellInfo(Cell cell)
        {
            GUILayout.Label($"Macro.Cell {cell.Coords}", EditorStyles.boldLabel);
            GUILayout.Label($"Zone: {cell.ZoneId} - {cell.Biome.name}");
            GUILayout.Label($"Height: {cell.Height:F1}");
        }

        private void ShowTriVertInfo(MacroVert vert, float distance)
        {
            GUILayout.Label("MacroVert", EditorStyles.boldLabel);
            GUILayout.Label($"Id: {vert.Id}, pos: {vert.Position.ToString(GetZoomLevel(distance))}");
            GUILayout.Label($"Cells: {vert.Cells.Select(c => c.Coords).ToJoinedString()}");
            GUILayout.Label($"Influence: {vert.Influence}");
        }

        private void ShowCursorInfo(Input input)
        {
            GUILayout.Label("Cursor", EditorStyles.boldLabel);
            GUILayout.Label($"World pos: {VectorToString(input.WorldPosition, GetZoomLevel(input.Distance))}");
            GUILayout.Label($"Camera dist: {Vector3.Distance(input.WorldPosition, input.ViewPoint):N0} m");
            if (input.IsBlockSelected)
            {
                GUILayout.Label(
                    $"Influence: {MacroMap.GetInfluence((OpenTK.Vector2) input.WorldPosition)}");
                GUILayout.Label($"Height: {MacroMap.GetHeight((OpenTK.Vector2) input.WorldPosition):N}");
            }
        }

        private void ShowBlockInfo(Vector2i blockPos)
        {
            GUILayout.Label($"Block {blockPos}", EditorStyles.boldLabel);

            var block = MicroMap.GetBlock(blockPos);
            if (block.Block.Top != BlockType.Empty)
            {
                //GUILayout.Label($"Influence: {InfluenceToString(block.Influence)}");
                GUILayout.Label($"Height: {block.Height:F1}");
                GUILayout.Label($"Type: {block.Block}");

                //Show block vertices info
                var vertices = MicroMap.GetBlockVertices(blockPos);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(MicroHeightToString(vertices.Item2));
                GUILayout.Label(MicroHeightToString(vertices.Item3));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(MicroHeightToString(vertices.Item1));
                GUILayout.Label(MicroHeightToString(vertices.Item4));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                //GUILayout.Box("Test");



            }
            else
                GUILayout.Label("Empty block");
        }


        #endregion

        private string InfluenceToBiomesString(double[] influence)
        {
            var biomes = from biome in _runner.Biomes
                where influence[biome.Index] > 0
                select $"{biome.name}: {influence[biome.Index]:G3}";
            return string.Join(", ", biomes.ToArray());
        }

        private int GetZoomLevel(float distance)
        {
            if (distance < 4)
                return 3;
            else if (distance < 10)
                return 2;
            else if (distance < 40)
                return 1;

            return 0;
        }

        private static Vector3 VertexToPosition(MacroVert vertex, bool is3d)
        {
            return new Vector3(vertex.Position.X, is3d ? vertex.Height : 0, vertex.Position.Y);
        }

        private static Vector3 CellToPosition(Cell cell, bool is3d)
        {
            return new Vector3(cell.Center.X, is3d ? cell.Height : 0, cell.Center.Y);
        }


        private string VectorToString(Vector3 vector, int precision)
        {
            switch (precision)
            {
                case 0:
                    return $"({vector.x:F0}, {vector.y:F0}, {vector.z:F0})";
                case 1:
                    return $"({vector.x:F1}, {vector.y:F1}, {vector.z:F1})";
                case 2:
                    return $"({vector.x:F2}, {vector.y:F2}, {vector.z:F2})";
                case 3:
                    return $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})";
                default:
                    return ToString();
            }
        }

        private string MicroHeightToString(MicroHeight microHeight)
        {
            return $"{microHeight.Layer1Height:F1}, {microHeight.BaseHeight:F1}";
        }

        #region Unity

        private void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGuiDelegate;
        }

        /// <summary>
        /// Process scene view handles and gizmos
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGuiDelegate(SceneView sceneView)
        {
            if (!Application.isPlaying || _runner == null || !_enabled)
                return;

            _sceneViewScreenPosition = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            _input = PrepareInput(_sceneViewScreenPosition);

            if (_drawLayout)
            {
                //Wanted bounds
                DrawLandBounds(_runner.LandBounds, Color.gray / 2);
                //Actual bounds
                DrawLandBounds(MacroMap.Bounds, Color.gray);

                //Draw macro cell outlines
                foreach (var meshCell in MacroMap.Cells)
                {
                    DrawMacroCell(meshCell);
                }
            }

            //Draw selected macrocell
            if (_input.SelectedMacroCell != null)
            {
                DrawMacroCell(_input.SelectedMacroCell, Active, true);
                DrawMacroZone(_input.SelectedMacroCell.Zone, _drawLayout ? Active : _input.SelectedMacroCell.Zone.Biome.LayoutColor);
            }

            //Draw selected vertex
            if (_input.SelectedVert != null)
            {
                DrawTriVert(_input.SelectedVert);
            }

            //Draw all blocks of selected cell
            if (_input.SelectedMicroCell != null && _drawMicro)
                DrawMicroCell(_input.SelectedMicroCell, Color.white);

            //Draw selected block
            if (_drawMicro)
            {
                var blockPos = (Vector2i) _input.WorldPosition;
                if (MicroMap != null)
                {
                    var block = MicroMap.GetBlock(blockPos);
                    DrawBlock(blockPos, Color.white, 3, block.Normal);
                }
                else
                    DrawBlock(blockPos, Color.white, 3);
            }

        }

        /// <summary>
        /// Process Land inspector window
        /// </summary>
        private void OnGUI()
        {
            _enabled = GUILayout.Toggle(_enabled, "Enabled");
            if (!_enabled)
                return;

            if (!Application.isPlaying || _runner == null || !_enabled)
                return;

            EditorGUILayout.Separator();

            _drawLayout = GUILayout.Toggle(_drawLayout, "Draw layout");
            _drawMicro = GUILayout.Toggle(_drawMicro, "Draw micro");

            _microRenderMode.BlockMode = (Renderer.BlockRenderMode)EditorGUILayout.EnumPopup(_microRenderMode.BlockMode);
            _microRenderMode.RenderMainLayer = GUILayout.Toggle(_microRenderMode.RenderMainLayer, "Render main layer");
            if (GUILayout.Button("Render"))
                _runner.Render(_microRenderMode);


            if (GUILayout.Button("Generate"))
                _runner.Generate();


            if (_input.IsWorldPlaneSelected)
            {
                ShowCursorInfo(_input);
            }

            if (_input.IsBlockSelected)
            {
                ShowBlockInfo(_input.BlockPosition);
            }

            if (_input.SelectedMacroCell != null)
            {
                ShowMacroCellInfo(_input.SelectedMacroCell);
                ShowMacroZoneInfo(_input.SelectedMacroCell.Zone);
            }

            if (_input.SelectedVert != null)
            {
                ShowTriVertInfo(_input.SelectedVert, _input.Distance);
            }

            _showZonesList = GUILayout.Toggle(_showZonesList, "Show all zones");
            if (_showZonesList)
            {
                foreach (var macroZone in MacroMap.Zones)
                    GUILayout.Label($"{macroZone.Id}, {macroZone.Biome.name}");
            }

        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                if (_runner == null)
                    _runner = FindObjectOfType<TriRunner>();

                Repaint();
            }
            else
            {
                _runner = null;
            }
        }

        private void OnDestroy()
        {
            _runner = null;
            SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;
        }

        #endregion

        private struct Input
        {
            /// <summary>
            /// Is some point of world plane selected?
            /// </summary>
            public bool IsWorldPlaneSelected;
            /// <summary>
            /// Is some land block selected?
            /// </summary>
            public bool IsBlockSelected;

            public Vector3 ViewPoint;
            public Vector3 WorldPosition;
            public Vector2i BlockPosition;
            /// <summary>
            /// Distance between <see cref="ViewPoint"/> and <see cref="WorldPosition"/>
            /// </summary>
            public float Distance;
            public Macro.Cell SelectedMacroCell;
            public Micro.Cell SelectedMicroCell;
            public MacroVert SelectedVert;
        }
    }
}
