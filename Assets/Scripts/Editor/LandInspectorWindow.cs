using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
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
        private Input _input;
        private bool _enabled = true;
        private bool _drawLayout;
        private bool _isShowInfluenceDebug;
        private bool _drawMicro;
        private bool _showZonesList;

        private Renderer.TerrainRenderMode _oldRenderMode;
        private Renderer.TerrainLayerToRender _oldRenderLayer;
        private readonly Dictionary<BlockType, Color> _defaultBlockColor = new Dictionary<BlockType, Color>();

        [MenuItem("Land/Inspector")]
        static void Init()
        {
            var window = (LandInspectorWindow)GetWindow(typeof(LandInspectorWindow));
            window.titleContent = new GUIContent("Land Inspector");
            window.Show();
        }

        #region Input processing

        private Input PrepareInput(Vector2 sceneScreenPosition)
        {
            Input result = new Input
            {
                View = new Ray(SceneView.currentDrawingSceneView.camera.transform.position, SceneView.currentDrawingSceneView.camera.transform.forward),
            };

            var userInputRay = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(sceneScreenPosition);
            result.Cursor = userInputRay;

            switch (_runner.RenderMode)
            {
                case Renderer.TerrainRenderMode.Blocks:
                    result = PrepareBlockModeInput(result);
                    break;
                case Renderer.TerrainRenderMode.Terrain:
                    result = PrepareTerrainModeInput(result);
                    break;
                //Macro mode
                case Renderer.TerrainRenderMode.Macro:
                {
                    if (MacroMap != null)
                    {
                        var selectedCell = MacroMap.Raycast(userInputRay);
                        if (selectedCell.Item1 != null)
                        {
                            result.IsMapSelected = true;
                            result.CursorPosition = selectedCell.Item2;
                            result.Distance = Vector3.Distance(result.CursorPosition, result.View.origin);
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

                    break;
                }
            }

            return result;
        }

        private Input PrepareBlockModeInput(Input input)
        {
            var selectedBlock = MicroMap?.RaycastBlockmap(input.Cursor);
            if (selectedBlock.HasValue)
            {
                input.BlockPosition = selectedBlock.Value.hitBlock;
                input.Distance = selectedBlock.Value.distance;
                input.CursorPosition = input.Cursor.GetPoint(input.Distance);
                input.IsBlockSelected = true;
            }

            return input;
        }

        private Input PrepareTerrainModeInput(Input input)
        {
            var selectedBlock = MicroMap?.RaycastHeightmap(input.Cursor);
            if (selectedBlock.HasValue)
            {
                input.BlockPosition = selectedBlock.Value.hitBlock;
                input.Distance = Vector3.Distance(input.Cursor.origin, selectedBlock.Value.hitPoint);
                input.CursorPosition = selectedBlock.Value.hitBlock;
                input.IsBlockSelected = true;
            }

            return input;
        }

        #endregion

        private void DrawBlockModeHandles(Input input)
        {
            if (input.IsBlockSelected)
            {
                DrawCursor(input.CursorPosition);
                DrawBlock(input.BlockPosition, MicroMap.GetBlock3(input.BlockPosition));
            }
        }

        private void DrawCursor(Vector3 position)
        {
            DebugExtension.DebugPoint(position, Color.white, 0.25f, 0, true);
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

            var cellDistance = Vector3.Distance(cell.Center, _input.View.origin);
            var fontSize = Mathf.RoundToInt(-cellDistance * 1 / 8 + 15);

            if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
            {
                var contrastLabelStyle = new GUIStyle(GUI.skin.label);
                var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
                contrastLabelStyle.normal.textColor = contrastColor;
                contrastLabelStyle.fontSize = fontSize;
                Handles.Label( cell.CenterPoint, cell.Coords.ToString(), contrastLabelStyle);
                Handles.color = contrastColor;
                Handles.DrawWireDisc(cell.CenterPoint, _input.View.direction, 0.1f);

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
                if(Vector3.Angle(_input.View.direction, block.Normal) > 90)
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
            Handles.DrawWireDisc(new Vector3(vert.Position.X, vert.Height.Nominal, vert.Position.Y), _input.View.direction, 1);
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

            if (drawNormal && block.Normal != Vector3.zero)
                DrawArrow.ForDebug(block.GetCenter(), block.Normal);
        }

        private void DrawBlock(Vector2i position, Blocks block, Color? overrideColor = null)
        {
            if (block.IsEmpty)
                return;

            var bounds2d = BlockInfo.GetWorldBounds(position);

            //Draw only top of base layer
            DrawRectangle.ForHandle(
                new Vector3(bounds2d.min.X, block.Heights.BaseHeight, bounds2d.min.Y),
                new Vector3(bounds2d.min.X, block.Heights.BaseHeight, bounds2d.max.Y),
                new Vector3(bounds2d.max.X, block.Heights.BaseHeight, bounds2d.max.Y),
                new Vector3(bounds2d.max.X, block.Heights.BaseHeight, bounds2d.min.Y),
                overrideColor ?? _defaultBlockColor[block.Base]);

            //Draw underground layer
            if (block.Underground != BlockType.Empty && block.Underground != BlockType.Cave)
            {
                var underBounds = IntervalToBounds(block.GetUnderLayerWidth());
                DebugExtension.DebugBounds(underBounds, overrideColor ?? _defaultBlockColor[block.Underground]);
            }

            //Draw main layer
            if (block.Ground != BlockType.Empty)
            {
                var mainBounds = IntervalToBounds(block.GetMainLayerWidth());
                DebugExtension.DebugBounds(mainBounds, overrideColor ?? _defaultBlockColor[block.Ground]);
            }

            Bounds IntervalToBounds(Interval height)
            {
                var result = new Bounds();
                result.SetMinMax(new Vector3(bounds2d.min.X, height.Min, bounds2d.min.Y),
                    new Vector3(bounds2d.max.X, height.Max, bounds2d.max.Y));
                return result;
            }
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

        private void ShowBlockModeControls(Input input)
        {
            if(input.IsBlockSelected)
                ShowBlockInfo(input.BlockPosition);
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
            GUILayout.Label($"Camera dist: {Vector3.Distance(input.CursorPosition, input.View.origin):N0} m");
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
                GUILayout.Label($"Height: {block.Block.Heights}");
                GUILayout.Label($"Type: {block.Block}");

                //Show block vertices info
                var vertices = MicroMap.GetBlock(blockPos);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(vertices.Corner01.ToString());
                GUILayout.Label(vertices.Corner11.ToString());
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(vertices.Corner00.ToString());
                GUILayout.Label(vertices.Corner10.ToString());
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

            EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
        }

        private void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _runner = FindObjectOfType<TriRunner>();

                _oldRenderLayer = _runner.RenderLayer;
                _oldRenderMode = _runner.RenderMode;

                var blocksSettings = Resources.LoadAll<BlockSettings>("");
                _defaultBlockColor.Clear();
                foreach (var blockSetting in blocksSettings)
                    _defaultBlockColor[blockSetting.Block] = blockSetting.DefaultColor;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _runner = null;
            }
        }

        /// <summary>
        /// Process scene view handles
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGuiDelegate(SceneView sceneView)
        {
            if (!Application.isPlaying || _runner == null || !_enabled)
                return;

            var sceneViewScreenPosition = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            _input = PrepareInput(sceneViewScreenPosition);

            if (_runner.RenderMode == Renderer.TerrainRenderMode.Blocks)
            {
                DrawBlockModeHandles(_input);
                return;
            }
            else
            {
                return;
            }

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

            if (_oldRenderLayer != _runner.RenderLayer || _oldRenderMode != _runner.RenderMode)
            {
                _oldRenderLayer = _runner.RenderLayer;
                _oldRenderMode = _runner.RenderMode;

                _runner.Render(_runner);
            }

            EditorGUILayout.Separator();

            if (_runner.RenderMode == Renderer.TerrainRenderMode.Blocks)
            {
                ShowBlockModeControls(_input);
            }
            else
            {


                _drawLayout = GUILayout.Toggle(_drawLayout, "Draw layout");
                _drawMicro = GUILayout.Toggle(_drawMicro, "Draw micro");

                

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

        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void OnDestroy()
        {
            _runner = null;
            SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;
            EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
        }

        #endregion

        private class Input
        {
            /// <summary>
            /// View camera origin and direction
            /// </summary>
            public Ray View;

            /// <summary>
            /// User cursor ray
            /// </summary>
            public Ray Cursor;

            /// <summary>
            /// Is some point of map selected? Cursor position contains point
            /// </summary>
            public bool IsMapSelected;
            /// <summary>
            /// Is some land block selected?
            /// </summary>
            public bool IsBlockSelected;

            

            /// <summary>
            /// World cursor position
            /// </summary>
            public Vector3 CursorPosition;

            /// <summary>
            /// Selected block position
            /// </summary>
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
