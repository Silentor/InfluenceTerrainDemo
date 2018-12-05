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
                IsMapSelected = false,
                ViewPoint = SceneView.currentDrawingSceneView.camera.transform.position,
                ViewDirection = SceneView.currentDrawingSceneView.camera.transform.forward,
            };

            var userInputRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(sceneScreenPosition);

            if (MacroMap != null)
            {
                var selectedCell = MacroMap.Raycast(userInputRay);
                if (selectedCell.Item1 != null)
                {
                    result.IsMapSelected = true;
                    result.CursorPosition = selectedCell.Item2;
                    result.Distance = Vector3.Distance(result.CursorPosition, result.ViewPoint);
                    result.SelectedMacroCell = selectedCell.Item1;
                    result.SelectedVert = MacroMap.Vertices.FirstOrDefault(v => Vector3.Distance(
                                                                                    new Vector3(v.Position.X, v.Height.Nominal, v.Position.Y), result.CursorPosition) < 1);

                    if (MicroMap != null)
                    {
                        result.SelectedMicroCell = MicroMap.GetCell(result.SelectedMacroCell);
                        var blockPos = (Vector2i)result.CursorPosition;
                        if (MicroMap.Bounds.Contains(blockPos))
                        {
                            result.BlockPosition = blockPos;
                            result.IsBlockSelected = true;
                        }
                    }
                }
            }

            if (MicroMap != null)
            {
                var hitPoint = MicroMap.Raycast(userInputRay);
                {
                    if (hitPoint.HasValue)
                    {
                        result.IsBlockSelected = true;
                        result.CursorPosition = hitPoint.Value.Item1;
                        result.Distance = Vector3.Distance(result.CursorPosition, result.ViewPoint);
                        result.BlockPosition = hitPoint.Value.Item2;
                        //result.SelectedMacroCell = MacroMap.GetCellAt((OpenTK.Vector2) result.CursorPosition);
                        //result.SelectedMicroCell = MicroMap.GetCell(result.SelectedMacroCell);
                        result.IsMapSelected = true;
                        result.SelectedVert = MacroMap.Vertices.FirstOrDefault(v => Vector3.Distance(
                                                                                        new Vector3(v.Position.X, v.Height.Nominal, v.Position.Y), result.CursorPosition) < 1);

                    }
                }
            }

            return result;
        }

        private void DrawMacroCell(Cell cell)
        {
            DrawMacroCell(cell, cell.Biome != null ? cell.Biome.LayoutColor : Inactive, 0, false, false);
        }

        private void DrawMacroCell(Cell cell, Color color, int width, bool labelVertices, bool filled)
        {
            Handles.color = color;

            //Draw perimeter
            if(width == 0)
                Handles.DrawPolyLine(
                    VertexToPosition(cell.Vertices[0]),
                    VertexToPosition(cell.Vertices[1]),
                    VertexToPosition(cell.Vertices[2]),
                    VertexToPosition(cell.Vertices[3]),
                    VertexToPosition(cell.Vertices[4]),
                    VertexToPosition(cell.Vertices[5]),
                    VertexToPosition(cell.Vertices[0]));
            else
            {
                Handles.DrawAAPolyLine(width,
                    VertexToPosition(cell.Vertices[0]),
                    VertexToPosition(cell.Vertices[1]),
                    VertexToPosition(cell.Vertices[2]),
                    VertexToPosition(cell.Vertices[3]),
                    VertexToPosition(cell.Vertices[4]),
                    VertexToPosition(cell.Vertices[5]),
                    VertexToPosition(cell.Vertices[0]));
            }

            if (filled)
            {
                Handles.DrawLines(new Vector3[]
                {
                    VertexToPosition(cell.Vertices[0]), cell.CenterPoint,
                    VertexToPosition(cell.Vertices[1]), cell.CenterPoint,
                    VertexToPosition(cell.Vertices[2]), cell.CenterPoint,
                    VertexToPosition(cell.Vertices[3]), cell.CenterPoint,
                    VertexToPosition(cell.Vertices[4]), cell.CenterPoint,
                    VertexToPosition(cell.Vertices[5]), cell.CenterPoint,
                });
            }

            var cellDistance = Vector3.Distance(cell.Center, _input.ViewPoint);
            var fontSize = Mathf.RoundToInt(-cellDistance * 1 / 8 + 15);

            if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
            {
                var contrastLabelStyle = new GUIStyle(GUI.skin.label);
                var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
                contrastLabelStyle.normal.textColor = contrastColor;
                contrastLabelStyle.fontSize = fontSize;
                Handles.Label( cell.CenterPoint, cell.Coords.ToString(), contrastLabelStyle);
                Handles.color = contrastColor;
                Handles.DrawWireDisc(cell.CenterPoint, _input.ViewDirection, 0.1f);

                if (labelVertices)
                {
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[0]), cell.CenterPoint, 0.2f), cell.Vertices[0].Id.ToString(), contrastLabelStyle);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[1]), cell.CenterPoint, 0.2f), cell.Vertices[1].Id.ToString(), contrastLabelStyle);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[2]), cell.CenterPoint, 0.2f), cell.Vertices[2].Id.ToString(), contrastLabelStyle);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[3]), cell.CenterPoint, 0.2f), cell.Vertices[3].Id.ToString(), contrastLabelStyle);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[4]), cell.CenterPoint, 0.2f), cell.Vertices[4].Id.ToString(), contrastLabelStyle);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[5]), cell.CenterPoint, 0.2f), cell.Vertices[5].Id.ToString(), contrastLabelStyle);
                }
            }

            

        }

        private void DrawMicroCell(Micro.Cell cell, Color color)
        {
            Handles.color = color;
            foreach (var block in cell.GetBlocks())
            {
                //Cull backface blocks
                if(Vector3.Angle(_input.ViewDirection, block.Normal) > 90)
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
            Handles.DrawWireDisc(new Vector3(vert.Position.X, vert.Height.Nominal, vert.Position.Y), _input.ViewDirection, 1);
        }

        private void DrawBlock(Vector2i position, Color color, uint width = 0, Vector3? normal = null)
        {
            Handles.color = color;
            DrawRectangle.ForHandle(BlockInfo.GetBounds(position), color, width);

            if (normal.HasValue)
                DrawArrow.ForDebug(BlockInfo.GetWorldCenter(position), normal.Value);
        }

        private void DrawBlock(BlockInfo block, Color color, uint width = 0, bool drawNormal = false)
        {
            Handles.color = color;

            var bounds = (Bounds)BlockInfo.GetBounds(block.Position);
            var corner00 = new Vector3(bounds.min.x, block.Corner00.Nominal, bounds.min.z);
            var corner10 = new Vector3(bounds.max.x, block.Corner10.Nominal, bounds.min.z);
            var corner11 = new Vector3(bounds.max.x, block.Corner11.Nominal, bounds.max.z);
            var corner01 = new Vector3(bounds.min.x, block.Corner01.Nominal, bounds.max.z);

            DrawRectangle.ForHandle(corner00, corner01, corner11, corner10, color, width);

            if (drawNormal)
                DrawArrow.ForDebug(block.GetCenter(), block.Normal);
        }

        /// <summary>
        /// Draw zone border
        /// </summary>
        /// <param name="zone"></param>
        private void DrawMacroZone(Macro.Zone zone, Color color)
        {
            Handles.color = color;

            //Get zone outer border
            foreach (var edge in zone.Edges)
                Handles.DrawLine(VertexToPosition(edge.Vertex1), VertexToPosition(edge.Vertex2));
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
            GUILayout.Label($"Height: {cell.DesiredHeight:F1} desired, {cell.CenterPoint.y:F1} true");
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
            GUILayout.Label($"World pos: {VectorToString(input.CursorPosition, GetZoomLevel(input.Distance))}");
            GUILayout.Label($"Camera dist: {Vector3.Distance(input.CursorPosition, input.ViewPoint):N0} m");
            if (input.IsBlockSelected)
            {
                GUILayout.Label(
                    $"Influence: {MacroMap.GetInfluence((OpenTK.Vector2) input.CursorPosition)}");
                GUILayout.Label($"Height: {MacroMap.GetHeight((OpenTK.Vector2) input.CursorPosition):N}");
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
                var vertices = MicroMap.GetBlock(blockPos);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(MicroHeightToString(vertices.Corner01));
                GUILayout.Label(MicroHeightToString(vertices.Corner11));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(MicroHeightToString(vertices.Corner00));
                GUILayout.Label(MicroHeightToString(vertices.Corner10));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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

        private static Vector3 VertexToPosition(MacroVert vertex)
        {
            return new Vector3(vertex.Position.X, vertex.Height.Nominal, vertex.Position.Y);
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

        private string MicroHeightToString(Heights heights)
        {
            return $"{heights.Layer1Height:F1}, {heights.BaseHeight:F1}";
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

            if (_input.IsMapSelected)
            {
                DebugExtension.DebugPoint(_input.CursorPosition, Color.white, 0.5f, 0, true);
            }

            //Draw selected macrocell
            if (_input.SelectedMacroCell != null)
            {
                DrawMacroCell(_input.SelectedMacroCell, Active, 5, true, true);
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
                if (MicroMap != null)
                {
                    var block = MicroMap.GetBlock(_input.BlockPosition);

                    if(block != null)
                        DrawBlock(block, Color.white, 3, true);
                }
                else
                    DrawBlock((Vector2i) _input.BlockPosition, Color.white, 3);
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


            if (_input.IsMapSelected)
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
            /// Is some point of map selected? Cursor position contains point
            /// </summary>
            public bool IsMapSelected;
            /// <summary>
            /// Is some land block selected?
            /// </summary>
            public bool IsBlockSelected;

            public Vector3 ViewPoint;
            public Vector3 ViewDirection;

            public Vector3 CursorPosition;
            public Vector2i BlockPosition;
            /// <summary>
            /// Distance between <see cref="ViewPoint"/> and <see cref="CursorPosition"/>
            /// </summary>
            public float Distance;
            public Macro.Cell SelectedMacroCell;
            public Micro.Cell SelectedMicroCell;
            public MacroVert SelectedVert;
        }
    }
}
