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
using UnityEngine.Rendering;
using Cell = TerrainDemo.Macro.Cell;
using Quaternion = UnityEngine.Quaternion;
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
        private (Vector2i position, Blocks block, MicroMap source)? _selectedBlock = null;
        private Vector2i? _selectedVertex = null;

        [MenuItem("Land/Inspector")]
        static void Init()
        {
            var window = (LandInspectorWindow)GetWindow(typeof(LandInspectorWindow));
            window.titleContent = new GUIContent("Land Inspector");
            window.Show();
        }

        #region Input processing

        private Input PrepareInput(Event @event)
        {
            var isSelected = false;

            Input result = new Input
            {
                View = new Ray(SceneView.currentDrawingSceneView.camera.transform.position, SceneView.currentDrawingSceneView.camera.transform.forward),
            };

            var userInputRay = HandleUtility.GUIPointToWorldRay(@event.mousePosition);
            result.CursorRay = userInputRay;

            if (@event.isKey)
            {
                if (@event.keyCode == KeyCode.Space && @event.type == EventType.KeyUp)
                {
                    isSelected = true;
                }

                if (@event.keyCode == KeyCode.B && @event.type == EventType.KeyUp && @event.control && @event.alt)
                {
                    Debug.Log("Block mode");
                    _runner.RenderMode = Renderer.TerrainRenderMode.Blocks;
                }

                if (@event.keyCode == KeyCode.T && @event.type == EventType.KeyUp && @event.control && @event.alt)
                {
                    Debug.Log("Terrain mode");
                    _runner.RenderMode = Renderer.TerrainRenderMode.Terrain;
                }

            }

            switch (_runner.RenderMode)
            {
                case Renderer.TerrainRenderMode.Blocks:
                    result = PrepareBlockModeInput(result);

                    if (isSelected)
                    {
                        if (_selectedBlock == result.HoveredBlock2)
                            _selectedBlock = null;
                        else
                            _selectedBlock = result.HoveredBlock2;

                        if(_selectedBlock.HasValue)
                            Debug.Log($"Selected block {_selectedBlock.Value}");
                    }
                    result.SelectedBlock = _selectedBlock;

                    break;
                case Renderer.TerrainRenderMode.Terrain:
                    result = PrepareTerrainModeInput(result);

                    if (isSelected)
                    {
                        //Process vertex selection
                        if (result.HoveredHeightVertex.HasValue)
                        {
                            
                            if (_selectedVertex.HasValue && result.HoveredHeightVertex.Value.position ==
                                _selectedVertex.Value)
                                _selectedVertex = null;    //Deselect
                            else 
                            {
                                _selectedVertex = result.HoveredHeightVertex.Value.position;   //Select
                                Debug.Log($"Selected vertex {_selectedVertex.Value}");
                            }
                        }
                        //Process block selection
                        else if (result.HoveredBlock2.HasValue)
                        {
                            if (_selectedBlock == result.HoveredBlock2)
                                _selectedBlock = null;           //Deselect
                            else
                            {
                                _selectedBlock = result.HoveredBlock2;      //Select
                                Debug.Log($"Selected block {_selectedBlock.Value}");
                            }
                        }
                    }
                    result.SelectedBlock = _selectedBlock;
                    result.SelectedHeightVertex = _selectedVertex;

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

                            /*
                            if (MicroMap != null)
                            {
                                result.SelectedMicroCell = MicroMap.GetCell(result.SelectedMacroCell);
                                var blockPos = (Vector2i)result.CursorPosition;
                                if (MicroMap.Bounds.Contains(blockPos))
                                {
                                    result.HoveredBlock2 = blockPos;
                                }
                            }
                            */
                        }
                    }

                    break;
                }
            }

            return result;
        }

        private Input PrepareBlockModeInput(Input input)
        {
            //var selectedBlock = MicroMap?.RaycastBlockmap(input.Cursor);
            var hoveredBlock = MicroMap?.RaycastBlockmap2(input.CursorRay);
            if (hoveredBlock.HasValue)
            {
                input.Distance = hoveredBlock.Value.distance;
                input.CursorPosition = input.CursorRay.GetPoint(input.Distance);
                input.HoveredBlock2 = (hoveredBlock.Value.position, hoveredBlock.Value.source.GetBlockRef(hoveredBlock.Value.position), hoveredBlock.Value.source);
            }

            return input;
        }

        private Input PrepareTerrainModeInput(Input input)
        {
            //var selectedBlock = MicroMap?.RaycastHeightmap(input.CursorRay);
            var hoveredBlock = MicroMap?.RaycastHeightmap2(input.CursorRay);
            if (hoveredBlock.HasValue)
            {
                input.Distance = Vector3.Distance(input.CursorRay.origin, hoveredBlock.Value.hitPoint);
                input.CursorPosition = hoveredBlock.Value.hitPoint;
                input.HoveredBlock2 = (hoveredBlock.Value.position, hoveredBlock.Value.source.GetBlockRef(hoveredBlock.Value.position), hoveredBlock.Value.source);

                var roundedCursorPosition = new Vector3(Mathf.Round(input.CursorPosition.x), input.CursorPosition.y,
                    Mathf.Round(input.CursorPosition.z));
                if (Vector3.Distance(input.CursorPosition, roundedCursorPosition) < 0.2f)
                {
                    var position = new Vector2i(roundedCursorPosition.x, roundedCursorPosition.z);
                    input.HoveredHeightVertex = (position, MicroMap.GetHeightRef(position));
                }
            }

            return input;
        }

        #endregion

        #region Scene drawing

        private void DrawBlockModeHandles(Input input)
        {
            if (input.HoveredBlock2.HasValue)
            {
                DrawCursor(input.CursorPosition);
                //DrawBlock(input.HoveredBlock.Value, MicroMap.GetBlockRef(input.HoveredBlock.Value));
                DrawBlock(input.HoveredBlock2.Value.position, input.HoveredBlock2.Value.block);
            }

            if (input.SelectedBlock.HasValue)
            {
                DrawBlock(input.SelectedBlock.Value.position, input.SelectedBlock.Value.block, Color.white);
            }
        }

        private void DrawTerrainModeHandles(Input input)
        {
            if (input.HoveredBlock2.HasValue)
            {
                DrawCursor(input.CursorPosition);

                //ref readonly var block = ref MicroMap.GetBlockRef(input.HoveredBlock.Value);
                var block = input.HoveredBlock2.Value.block;
                var mapSource = input.HoveredBlock2.Value.source;
                ref readonly var c00 = ref mapSource.GetHeightRef(input.HoveredBlock2.Value.position);
                ref readonly var c01 = ref mapSource.GetHeightRef(input.HoveredBlock2.Value.position + Vector2i.Forward);
                ref readonly var c11 = ref mapSource.GetHeightRef(input.HoveredBlock2.Value.position + Vector2i.One);
                ref readonly var c10 = ref mapSource.GetHeightRef(input.HoveredBlock2.Value.position + Vector2i.Right);
                DrawBlock(input.HoveredBlock2.Value.position, block, c00, c01, c11, c10);
            }

            if (input.SelectedBlock.HasValue)
            {
                var block = input.SelectedBlock.Value.block;
                var mapSource = input.SelectedBlock.Value.source;
                ref readonly var c00 = ref mapSource.GetHeightRef(input.SelectedBlock.Value.position);
                ref readonly var c01 = ref mapSource.GetHeightRef(input.SelectedBlock.Value.position + Vector2i.Forward);
                ref readonly var c11 = ref mapSource.GetHeightRef(input.SelectedBlock.Value.position + Vector2i.One);
                ref readonly var c10 = ref mapSource.GetHeightRef(input.SelectedBlock.Value.position + Vector2i.Right);
                DrawBlock(input.SelectedBlock.Value.position, block, c00, c01, c11, c10, Color.white);
            }

            if (input.HoveredHeightVertex.HasValue)
            {
                DrawHeightVertex(input.HoveredHeightVertex.Value.position);
            }

            if (input.SelectedHeightVertex.HasValue)
            {
                DrawHeightVertex(input.SelectedHeightVertex.Value, Color.white);
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

        private void DrawBlock(Vector2i position, in Blocks block, Color? overrideColor = null)
        {
            if (block.IsEmpty)
                return;

            var bounds2d = BlockInfo.GetWorldBounds(position);

            //Draw only top of base layer
            DrawRectangle.ForHandle(
                new Vector3(bounds2d.min.X, block.Height.Base, bounds2d.min.Y),
                new Vector3(bounds2d.min.X, block.Height.Base, bounds2d.max.Y),
                new Vector3(bounds2d.max.X, block.Height.Base, bounds2d.max.Y),
                new Vector3(bounds2d.max.X, block.Height.Base, bounds2d.min.Y),
                overrideColor ?? _defaultBlockColor[block.Base]);

            //Draw underground layer
            if (block.Underground != BlockType.Empty)
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

        private void DrawBlock(Vector2i position, in Blocks block, in Heights c00, in Heights c01, in Heights c11, in Heights c10, Color32? overrideColor = null)
        {
            if (block.IsEmpty)
                return;

            if (block.Ground != BlockType.Empty)
            {
                var color = overrideColor ?? _defaultBlockColor[block.Ground];
                DrawQuad(position, color, c00.Main, c01.Main, c11.Main, c10.Main);
            }
            else if (block.Underground != BlockType.Empty)
            {
                var color = overrideColor ?? _defaultBlockColor[block.Underground];
                DrawQuad(position, color, c00.Underground, c01.Underground, c11.Underground, c10.Underground);
            }
            else
            {
                var color = overrideColor ?? _defaultBlockColor[block.Base];
                DrawQuad(position, color, c00.Base, c01.Base, c11.Base, c10.Base);
            }

            void DrawQuad(Vector2i pos, Color32 color, float h00, float h01, float h11, float h10)
            {
                var (min, max) = BlockInfo.GetWorldBounds(pos);

                Handles.color = color;
                Handles.zTest = CompareFunction.LessEqual;
                Handles.DrawAAPolyLine(5,
                    new Vector3(min.X, h00, min.Y),
                    new Vector3(min.X, h01, max.Y),
                    new Vector3(max.X, h11, max.Y),
                    new Vector3(max.X, h10, min.Y),
                    new Vector3(min.X, h00, min.Y)
                );

                Handles.color = ((Color)color) / 2;
                Handles.zTest = CompareFunction.Greater;
                Handles.DrawAAPolyLine(3,
                    new Vector3(min.X, h00, min.Y),
                    new Vector3(min.X, h01, max.Y),
                    new Vector3(max.X, h11, max.Y),
                    new Vector3(max.X, h10, min.Y),
                    new Vector3(min.X, h00, min.Y)
                );

            }
        }

        private void DrawHeightVertex(Vector2i position, Color32? overrideColor = null)
        {
            if (Event.current.type == EventType.Repaint)
            {
                ref readonly var vertex = ref MicroMap.GetHeightRef(position);

                var pos = new Vector3(position.X, vertex.Base, position.Z);

                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = overrideColor ?? Color.black;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = Color.black / 2;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                if (vertex.Underground > vertex.Base)
                {
                    pos.y = vertex.Underground;
                    Handles.zTest = CompareFunction.LessEqual;
                    Handles.color = overrideColor ?? Color.magenta;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                    Handles.zTest = CompareFunction.Greater;
                    Handles.color = Color.magenta / 2;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
                }

                if (vertex.Main > vertex.Base && vertex.Main > vertex.Underground)
                {
                    pos.y = vertex.Main;
                    Handles.zTest = CompareFunction.LessEqual;
                    Handles.color = overrideColor ?? Color.green;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                    Handles.zTest = CompareFunction.Greater;
                    Handles.color = Color.green / 2;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
                }
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

        #endregion

#region Window content

        private void ShowBlockModeContent(Input input)
        {
            if (input.SelectedBlock.HasValue)
                ShowBlockInfo(input.SelectedBlock.Value, true, false);

            if (input.HoveredBlock2.HasValue && input.HoveredBlock2 != input.SelectedBlock)
            {
                ShowBlockInfo(input.HoveredBlock2.Value, false, false);
            }
        }

        private void ShowTerrainModeContent(Input input)
        {
            if (input.SelectedBlock.HasValue)
                ShowBlockInfo(input.SelectedBlock.Value, true, true);

            if (input.HoveredBlock2.HasValue && input.HoveredBlock2 != input.SelectedBlock)
                ShowBlockInfo(input.HoveredBlock2.Value, false, true);

            if (input.SelectedHeightVertex.HasValue)
                ShowHeightVertexInfo(input.SelectedHeightVertex.Value, true);

            if (input.HoveredHeightVertex.HasValue && input.HoveredHeightVertex.Value.position != input.SelectedHeightVertex)
                ShowHeightVertexInfo(input.HoveredHeightVertex.Value.position, false);
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
            if (input.HoveredBlock2.HasValue)
            {
                GUILayout.Label(
                    $"Influence: {MacroMap.GetInfluence((OpenTK.Vector2) input.CursorPosition)}");
                GUILayout.Label($"Height: {MacroMap.GetHeight((OpenTK.Vector2) input.CursorPosition):N}");
            }
        }

        private void ShowBlockInfo((Vector2i position, Blocks block, MicroMap source) block2, bool isSelected, bool showHeightmap)
        {
            var (position, block, source) = block2;
            GUILayout.Label(isSelected ? $"Selected block {position}" : $"Hovered block {position}", EditorStyles.boldLabel);

            if (!block.IsEmpty)
            {
                GUILayout.Label("Type                   Height");

                GUILayout.BeginVertical("box", GUILayout.Width(150));
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{block.Ground}", GUILayout.Width(100));
                GUILayout.Label(block.Ground != BlockType.Empty ? $"{block.Height.Main:N1}" : "-");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{block.Underground}", GUILayout.Width(100));
                GUILayout.Label(block.Underground != BlockType.Empty ? $"{block.Height.Underground:N1}" : "-");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{block.Base}", GUILayout.Width(100));
                GUILayout.Label($"{block.Height.Base:N1}");
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                if (showHeightmap)
                {
                    GUILayout.Label("Vertices:");
                    //Show block vertices info
                    var vertices = source.GetBlock(position);
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    HeightToGUI("┌", vertices.Corner01, false);
                    HeightToGUI("┐", vertices.Corner11, false);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    HeightToGUI("└", vertices.Corner00, false);
                    HeightToGUI("┘", vertices.Corner10,false);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
            }
            else
                GUILayout.Label("Empty block");
        }

        private void ShowHeightVertexInfo(Vector2i position, bool isSelected)
        {
            if(isSelected)
                GUILayout.Label($"Selected height {position}", EditorStyles.boldLabel);
            else
                GUILayout.Label($"Hovered height {position}", EditorStyles.boldLabel);

            ref readonly var v = ref MicroMap.GetHeightRef(position);

            var newHeight = HeightToGUI(string.Empty, v, true);
            if (newHeight.HasValue)
            {
                MicroMap.SetHeights(new[]{position}, new[]{newHeight.Value});
                Debug.Log($"Changed height {position} to {newHeight.Value}");
            }
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
            return $"{heights.Main:F1}, {heights.Base:F1}";
        }

        private Heights? HeightToGUI(string header, in Heights height, bool showEditor)
        {
            Heights? result = null;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Width(100));
            GUILayout.Label(header, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label(height.IsMainLayerPresent ? height.Main.ToString("N1") : "-");
            GUILayout.Label(height.IsUndergroundLayerPresent ? height.Underground.ToString("N1") : "-");
            GUILayout.Label(height.Base.ToString("N1"));
            GUILayout.EndVertical();

            if (showEditor)
            {
                GUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                var newMain = EditorGUILayout.DelayedFloatField(height.Main, GUILayout.Width(50));
                var newUnderground = EditorGUILayout.DelayedFloatField(height.Underground, GUILayout.Width(50));
                var newBase = EditorGUILayout.DelayedFloatField(height.Base, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    result = new Heights(newMain, newUnderground, newBase);
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            return result;
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

#region Unity

        private void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;

            EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
        }

        /// <summary>
        /// Process scene view handles
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!Application.isPlaying || _runner == null || !_enabled)
                return;

            _input = PrepareInput(Event.current);

            if (_runner.RenderMode == Renderer.TerrainRenderMode.Blocks)
            {
                DrawBlockModeHandles(_input);
                return;
            }
            else if(_runner.RenderMode == Renderer.TerrainRenderMode.Terrain)
            {
                DrawTerrainModeHandles(_input);
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
            /*
            if (_drawMicro)
            {
                if (MicroMap != null)
                {
                    var block = MicroMap.GetBlock(_input.HoveredBlock.Value);

                    if(block != null)
                        DrawBlock(block, Color.white, 3, true);
                }
                else
                    DrawBlock((Vector2i) _input.HoveredBlock, Color.white, 3);
            }
            */

        }

        /// <summary>
        /// Process Land inspector window
        /// </summary>
        private void OnGUI()
        {
            _enabled = GUILayout.Toggle(_enabled, "Enabled");
            if (!_enabled)
                return;

            if (!Application.isPlaying || _runner == null || !_enabled || _input == null)
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
                ShowBlockModeContent(_input);
            }
            else if (_runner.RenderMode == Renderer.TerrainRenderMode.Terrain)
            {
                ShowTerrainModeContent(_input);
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

                if (_input.HoveredBlock2.HasValue)
                {
                    ShowBlockInfo(_input.HoveredBlock2.Value, false, true);
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
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
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
            public Ray CursorRay;

            /// <summary>
            /// Is some point of map selected? Cursor position contains point
            /// </summary>
            public bool IsMapSelected;
            

            /// <summary>
            /// World cursor position
            /// </summary>
            public Vector3 CursorPosition;

            /// <summary>
            /// Mouse hovered block
            /// </summary>
            public (Vector2i position, Blocks block, MicroMap source)? HoveredBlock2;


            /// <summary>
            /// Selected block position
            /// </summary>
            public (Vector2i position, Blocks block, MicroMap source)? SelectedBlock;


            /// <summary>
            /// Distance between <see cref="ViewPoint"/> and <see cref="CursorPosition"/>
            /// </summary>
            public float Distance;

            public Macro.Cell SelectedMacroCell;

            public Micro.Cell SelectedMicroCell;

            public MacroVert SelectedVert;

            public (Vector2i position, Heights vertex)?  HoveredHeightVertex;

            public Vector2i? SelectedHeightVertex;
        }
    }
}
