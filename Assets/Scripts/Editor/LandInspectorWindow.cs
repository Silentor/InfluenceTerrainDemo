using System;
using System.Linq;
using System.Xml;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEditor;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Editor
{
    public class LandInspectorWindow : EditorWindow
    {
        public Color Inactive = Color.gray;
        public Color Active = Color.white;

        private TriRunner _settings;
        private MacroMap _macro;
        private MicroMap _micro;
        private Vector2 _sceneViewScreenPosition;
        private Input _input;
        private bool _enabled = true;
        private bool _drawLayout;
        private MacroMap.CellWeightDebug _getHeightDebug;
        private bool _drawMicro;


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

            if (_macro != null)
            {
                var distance = 0f;
                if (layoutPlane.Raycast(userInputRay, out distance))
                {
                    result.IsWorldPlaneSelected = true;
                    var layoutPoint = (OpenTK.Vector2)userInputRay.GetPoint(distance);
                    result.WorldPosition = layoutPoint;
                    result.Distance = distance;
                    result.SelectedMacroCell = _macro.GetCellAt(layoutPoint);
                    result.SelectedVert = _macro.Vertices.FirstOrDefault(v => OpenTK.Vector2.Distance(v.Coords, layoutPoint) < 1);

                    if (result.SelectedMacroCell != null)
                        result.SelectedMicroCell = _micro.GetCell(result.SelectedMacroCell);

                    if (_micro != null)
                    {
                        var blockPos = (Vector2i) result.WorldPosition;
                        if (_micro.Bounds.Contains(blockPos))
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
            var isRelief = _settings.MacroCellReliefVisualization == TriRenderer.MacroCellReliefMode.Rude;

            Handles.DrawPolyLine(
                VertexToPosition(cell.Vertices3[0], isRelief),
                VertexToPosition(cell.Vertices3[1], isRelief),
                VertexToPosition(cell.Vertices3[2], isRelief),
                VertexToPosition(cell.Vertices3[3], isRelief),
                VertexToPosition(cell.Vertices3[4], isRelief),
                VertexToPosition(cell.Vertices3[5], isRelief),
                VertexToPosition(cell.Vertices3[0], isRelief));

            var cellDistance = Vector3.Distance(cell.Center, SceneView.lastActiveSceneView.camera.transform.position);
            var fontSize = Mathf.RoundToInt(-cellDistance * 1 / 8 + 15);

            if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
            {
                var style = new GUIStyle(GUI.skin.label);
                var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
                style.normal.textColor = contrastColor;
                style.fontSize = fontSize;
                Handles.Label( CellToPosition(cell, isRelief), cell.Position.ToString(), style);
                Handles.color = contrastColor;
                Handles.DrawWireDisc(CellToPosition(cell, isRelief), Vector3.up, 0.1f);

                if (labelVertices)
                {
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[0], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[0].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[1], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[1].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[2], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[2].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[3], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[3].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[4], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[4].Id.ToString(), style);
                    Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices3[5], isRelief), CellToPosition(cell, isRelief), 0.2f), cell.Vertices3[5].Id.ToString(), style);
                }
            }

        }

        private void DrawMicroCell(Micro.Cell cell, Color color)
        {
            Handles.color = color;
            foreach (var block in cell.Blocks)
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
            Handles.DrawWireDisc(vert.Coords, Vector3.up, 1);
        }

        private void DrawBlock(Vector2i position, Color color, int width = 0)
        {
            Handles.color = color;
            DrawRectangle.ForHandle(BlockInfo.GetBounds(position), color, width);
        }

        /// <summary>
        /// Draw zone border
        /// </summary>
        /// <param name="zone"></param>
        private void DrawMacroZone(Macro.Zone zone, Color color)
        {
            Handles.color = color;
            var isRelief = _settings.MacroCellReliefVisualization == TriRenderer.MacroCellReliefMode.Rude;

            //Get zone outer border
            foreach (var edge in zone.Edges)
                Handles.DrawLine(VertexToPosition(edge.Vertex1, isRelief), VertexToPosition(edge.Vertex2, isRelief));
        }

        #region UI

        private void ShowMacroCellInfo(Cell cell)
        {
            GUILayout.Label("Macro.Cell", EditorStyles.boldLabel);
            GUILayout.Label($"Position: {cell.Position}, {cell.Biome.name}");
            GUILayout.Label($"Zone: {cell.ZoneId}");
            GUILayout.Label($"Influence: {InfluenceToString(cell.Zone.Influence)}");
            GUILayout.Label($"Height: {cell.Height:F1}");

        }

        private void ShowTriVertInfo(MacroVert vert, float distance)
        {
            GUILayout.Label("MacroVert", EditorStyles.boldLabel);
            GUILayout.Label($"Id: {vert.Id}, pos: {vert.Coords.ToString(GetZoomLevel(distance))}");
            GUILayout.Label($"Cells: {vert.Cells.Select(c => c.Position).ToJoinedString()}");
            GUILayout.Label("Influence: " + InfluenceToBiomesString(vert.Influence));
        }

        private void ShowCursorInfo(Input input)
        {
            GUILayout.Label("Cursor", EditorStyles.boldLabel);
            GUILayout.Label($"World pos: {VectorToString(input.WorldPosition, GetZoomLevel(input.Distance))}");
            GUILayout.Label($"Camera dist: {Vector3.Distance(input.WorldPosition, input.ViewPoint):N0} m");
            if (input.IsBlockSelected)
            {
                GUILayout.Label(
                    $"Influence: {InfluenceToString(_macro.GetInfluence((OpenTK.Vector2) input.WorldPosition))}");
                MacroMap.CellWeightDebug debug;
                GUILayout.Label($"Height: {_macro.GetHeight((OpenTK.Vector2) input.WorldPosition, out debug):N}");
                var debugWeightInfo = debug.Cells.Select(d => $"Id: {d.Id}, Height: {d.Height}, Weight: {d.Weight:F7}")
                    .ToJoinedString(",\n");
                GUILayout.Label($"Debug radius: {debug.Radius:N1}, weights({debug.Cells.Length}): \n{debugWeightInfo}");
                _getHeightDebug = debug;
            }
        }

        private void ShowBlockInfo(Vector2i blockPos)
        {
            GUILayout.Label("Block", EditorStyles.boldLabel);

            var block = _micro.GetBlock(blockPos);
            GUILayout.Label($"Block pos: {blockPos}");
            if (block.Type != BlockType.Empty)
            {
                GUILayout.Label($"Influence: {InfluenceToString(block.Influence)}");
                GUILayout.Label($"Height: {block.Height:F1}");
            }
            else
                GUILayout.Label("Empty block");
        }


        #endregion

        private string InfluenceToString(double[] influence)
        {
            return $"[{influence.ToJoinedString(i => i.ToString("F2"))}]";
        }

        private string InfluenceToBiomesString(double[] influence)
        {
            var biomes = from biome in _settings.Biomes
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
            return new Vector3(vertex.Coords.X, is3d ? vertex.Height : 0, vertex.Coords.Y);
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

        #region Unity

        void Awake()
        {
            Debug.Log("Awake");
        }

        private void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGuiDelegate;
            //_settings = FindObjectOfType<TriRunner>();
            //_mesh = _settings.Mesh;
        }



        /// <summary>
        /// Process scene view handles and gizmos
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGuiDelegate(SceneView sceneView)
        {
            if (!Application.isPlaying)
                return;

            //DEBUG
            /*
            if (_enabled)
            {
                Handles.color = Color.black;
                Handles.DrawWireDisc(_getHeightDebug.Position, Vector3.up, _getHeightDebug.Radius);
                if (_getHeightDebug.Cells != null)
                    foreach (var cellInfo in _getHeightDebug.Cells)
                    {
                        var cell = _macro.Cells.Find(c => c.Position == cellInfo.Id);
                        DrawMacroCell(cell, Color.black, false);
                    }
            }
            */
            //DEBUG

            if (!_settings)
                _settings = FindObjectOfType<TriRunner>();
            if (_settings == null)
                return;

            if (_macro == null)
                _macro = _settings.Macro;

            if (_micro == null)
                _micro = _settings.Micro;

            if (!_settings || _macro == null)
                return;

            if (!_enabled)
                return;

            _sceneViewScreenPosition = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            _input = PrepareInput(_sceneViewScreenPosition);

            if (_drawLayout)
            {
                //Wanted bounds
                DrawLandBounds(_settings.LandBounds, Color.gray / 2);
                //Actual bounds
                DrawLandBounds(_macro.Bounds, Color.gray);

                //Draw macro cell outlines
                foreach (var meshCell in _macro.Cells)
                {
                    DrawMacroCell(meshCell);
                }
            }

            //Draw selected macrocell
            if (_input.SelectedMacroCell != null)
            {
                DrawMacroCell(_input.SelectedMacroCell, Active, true);
                DrawMacroZone(_input.SelectedMacroCell.Zone, Color.white);
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
            if(_drawMicro)
                DrawBlock((Vector2i)_input.WorldPosition, Color.white, 3);

        }

        /// <summary>
        /// Process Land inspector window
        /// </summary>
        private void OnGUI()
        {
            if (!Application.isPlaying)
                return;

            if (!_settings)
                _settings = FindObjectOfType<TriRunner>();
            if (_settings == null)
                return;

            if (_macro == null)
                _macro = _settings.Macro;

            if (!_settings || _macro == null)
                return;

            _enabled = GUILayout.Toggle(_enabled, "Enabled");
            if (!_enabled)
                return;

            _drawLayout = GUILayout.Toggle(_drawLayout, "Draw layout");

            _drawMicro = GUILayout.Toggle(_drawMicro, "Draw micro");

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
            }

            if (_input.SelectedVert != null)
            {
                ShowTriVertInfo(_input.SelectedVert, _input.Distance);
            }

        }

        private void OnInspectorUpdate()
        {
            if(Application.isPlaying)
                Repaint();
        }

        private void OnDestroy()
        {
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
