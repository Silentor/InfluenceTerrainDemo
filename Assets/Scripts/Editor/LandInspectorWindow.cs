using System;
using OpenToolkit.Mathematics;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Cell = TerrainDemo.Macro.Cell;
using Quaternion = UnityEngine.Quaternion;
using Ray = TerrainDemo.Spatial.Ray;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector2i = TerrainDemo.Spatial.Vector2i;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Editor
{
	public class LandInspectorWindow : EditorWindow
	{
		public Color Inactive = Color.gray;
		public Color Active = Color.white;

		private TriRunner _runner;
		private MacroMap MacroMap => _runner? _runner.Macro : null;
		private MicroMap MicroMap => _runner ? _runner.Micro : null;
		private NavigationMap NavMap => _runner ? _runner.NavMap : null;

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
		private (GridPos position, Blocks block, BaseBlockMap source)? _selectedBlock = null;
		private (GridPos position, Heights vertex, BaseBlockMap source)? _selectedVertex = null;
		private static readonly float Deg90ToRadians = MathHelper.DegreesToRadians(90);
		private InspectorMode _inspectorMode;

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
				View = new Ray(SceneView.currentDrawingSceneView.camera.transform.position,
					SceneView.currentDrawingSceneView.camera.transform.forward),
			};

			//Validate cursor position
			var screenSpacePos = HandleUtility.GUIPointToScreenPixelCoordinate(@event.mousePosition);
			if (_input != null && (screenSpacePos.x < 0 || screenSpacePos.y < 0 ||
								   screenSpacePos.x > SceneView.currentDrawingSceneView.camera.pixelWidth ||
								   screenSpacePos.y > SceneView.currentDrawingSceneView.camera.pixelHeight
				))
			{
				result.CursorRay = _input.CursorRay;
			}
			else
			{
				result.CursorRay = (Ray)HandleUtility.GUIPointToWorldRay(@event.mousePosition);
			}

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
						if (_selectedBlock == result.HoveredBlock)
							_selectedBlock = null;
						else
							_selectedBlock = result.HoveredBlock;

						if (_selectedBlock.HasValue)
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
								_selectedVertex.Value.position)
								_selectedVertex = null;    //Deselect
							else
							{
								_selectedVertex = result.HoveredHeightVertex.Value;   //Select
								Debug.Log($"Selected vertex {_selectedVertex.Value}");
							}
						}
						//Process block selection
						else if (result.HoveredBlock.HasValue)
						{
							if (_selectedBlock == result.HoveredBlock)
								_selectedBlock = null;           //Deselect
							else
							{
								_selectedBlock = result.HoveredBlock;      //Select
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
						var selectedCell = MacroMap.Raycast(result.CursorRay);
						if (selectedCell.Item1 != null)
						{
							result.IsMapSelected = true;
							result.CursorPosition = selectedCell.Item2;
							result.Distance = Vector3.Distance(result.CursorPosition, result.View.Origin);
							result.HoveredMacroCell = selectedCell.Item1;
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
			var hoveredBlock = MicroMap?.RaycastBlockmap(input.CursorRay);
			if (hoveredBlock.HasValue)
			{
				input.Distance = hoveredBlock.Value.distance;
				input.CursorPosition = input.CursorRay.GetPoint(input.Distance);
				input.HoveredBlock = (hoveredBlock.Value.position, hoveredBlock.Value.source.GetBlockRef(hoveredBlock.Value.position), hoveredBlock.Value.source);
			}

			return input;
		}

		private Input PrepareTerrainModeInput(Input input)
		{
			//var selectedBlock = MicroMap?.RaycastHeightmap(input.CursorRay);
			var hoveredBlock = MicroMap?.RaycastHeightmap(input.CursorRay);
			if (hoveredBlock.HasValue)
			{
				input.Distance = Vector3.Distance(input.CursorRay.Origin, hoveredBlock.Value.hitPoint);
				input.CursorPosition = hoveredBlock.Value.hitPoint;
				input.HoveredBlock = (hoveredBlock.Value.position, hoveredBlock.Value.source.GetBlockRef(hoveredBlock.Value.position), hoveredBlock.Value.source);

				var roundedCursorPosition = new Vector3(Mathf.Round(input.CursorPosition.X), input.CursorPosition.Y,
					Mathf.Round(input.CursorPosition.Z));
				if (Vector3.Distance(input.CursorPosition, roundedCursorPosition) < 0.2f)
				{
					var position = new GridPos(roundedCursorPosition.X, roundedCursorPosition.Z);
					input.HoveredHeightVertex = (position, hoveredBlock.Value.source.GetHeightRef(position), hoveredBlock.Value.source);
				}

				//Find hovered cell
				input.SelectedMicroCell = MicroMap.GetCell(hoveredBlock.Value.position);
				input.HoveredMacroCell = input.SelectedMicroCell.Macro;
			}

			return input;
		}

		#endregion

		#region Scene drawing

		private void DrawBlockModeHandles(Input input)
		{
			/*
            if (input.HoveredBlock.HasValue)
            {
                DrawCursor(input.CursorPosition);
                //DrawBlock(input.HoveredBlock.Value, MicroMap.GetBlockRef(input.HoveredBlock.Value));
                DrawBlock(input.HoveredBlock.Value.position, input.HoveredBlock.Value.block);
            }

            if (input.SelectedBlock.HasValue)
            {
                DrawBlock(input.SelectedBlock.Value.position, input.SelectedBlock.Value.block, Color.white);
            }
            */

			//DrawNormals(input);
		}

		private void DrawTerrainModeHandles(Input input)
		{
			if (input.HoveredBlock.HasValue)
			{
				DrawCursor(input.CursorPosition);

				//DEBUG draw interpolated height to compare accurateness
				var height =
					input.HoveredBlock.Value.source.GetHeight((Vector2)input.CursorPosition);

				DebugExtension.DebugWireSphere(new Vector3(input.CursorPosition.X, height, input.CursorPosition.Z), Color.blue, 0.02f, 0, true);
				//DEBUG


				var block = input.HoveredBlock.Value.block;
				var mapSource = input.HoveredBlock.Value.source;
				ref readonly var c00 = ref mapSource.GetHeightRef(input.HoveredBlock.Value.position);
				ref readonly var c01 = ref mapSource.GetHeightRef(input.HoveredBlock.Value.position + Vector2i.Forward);
				ref readonly var c11 = ref mapSource.GetHeightRef(input.HoveredBlock.Value.position + Vector2i.One);
				ref readonly var c10 = ref mapSource.GetHeightRef(input.HoveredBlock.Value.position + Vector2i.Right);
				DrawBlock(input.HoveredBlock.Value.position, block, c00, c01, c11, c10);
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
				DrawHeightVertex(input.HoveredHeightVertex.Value);
			}

			if (input.SelectedHeightVertex.HasValue)
			{
				DrawHeightVertex(input.SelectedHeightVertex.Value, Color.white);
			}

			if ( _input.SelectedMicroCell != null )
				DrawMicroCell( _input.SelectedMicroCell, Color.white );	


			//DrawNormals(input);
		}

		private void DrawNavigationModeHandles(Input input)
		{
			//Draw hovered block
			var blockInfo = input.HoveredBlock?.source.GetBlock(input.HoveredBlock.Value.position);
			if (blockInfo != null)
				DrawBlock(blockInfo.Value, Color.blue, 0, true);

			//Draw all navigation cells
			var navMap = _runner.NavMap;
			foreach (var navCell in navMap.Nodes.Values)
			{
				HandleMap.DrawNavigationNode(navCell, 0, false);
			}

			//Draw hovered nav node
			if (input.HoveredMacroCell != null)
			{
				var hoveredNode = navMap.Nodes[input.SelectedMicroCell.Id];
				HandleMap.DrawNavigationNode(hoveredNode, 10, true);
				foreach (var neighbor in navMap.MacroGraph.GetNeighbors(hoveredNode))
				{
					DrawArrow.ForDebug(neighbor.edge.From.Cell.Macro.CenterPoint, neighbor.edge.To.Cell.Macro.CenterPoint - neighbor.edge.From.Cell.Macro.CenterPoint, Color.blue, 0, false);
					HandleMap.DrawNavigationNode(neighbor.neighbor, 5, true);
				}

			}
		}

		private void DrawNormals(Input input)
		{
			//Find near blocks
			const int visualizeRadius = 20;
			var bounds = new Bounds2i((GridPos)input.CursorRay.Origin.ConvertTo2D(), visualizeRadius);
			foreach (var position in bounds)
			{
				var overlap = MicroMap.GetOverlapState(position);
				if (overlap.state == BlockOverlapState.Above)
				{
					DrawBlockNormal(position, in MicroMap.GetBlockData(position), Color.blue);
					DrawBlockNormal(position, in overlap.map.GetBlockData(position), Color.blue);
				}
				else if (overlap.state == BlockOverlapState.Overlap)
				{
					DrawBlockNormal(position, in MicroMap.GetBlockData(position), Color.blue);
				}
				else
				{
					DrawBlockNormal(position, in MicroMap.GetBlockData(position), Color.blue);
				}

				void DrawBlockNormal(GridPos pos, in BlockData blockData, Color color)
				{
					var blockCenterPoint = new Vector3(pos.X + 0.5f, blockData.Height, pos.Z + 0.5f);
					if (Vector3.Distance(input.CursorRay.Origin, blockCenterPoint) < visualizeRadius)
					{
						DrawArrow.ForDebug(blockCenterPoint, blockData.Normal, color);
					}
				}
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
			var oldzTest = Handles.zTest;

			Handles.color = color;

			var perimeter = new[]
			{
				VertexToPosition(cell.Vertices[0]),
				VertexToPosition(cell.Vertices[1]),
				VertexToPosition(cell.Vertices[2]),
				VertexToPosition(cell.Vertices[3]),
				VertexToPosition(cell.Vertices[4]),
				VertexToPosition(cell.Vertices[5]),
				VertexToPosition(cell.Vertices[0])
			};

			//Draw perimeter
			if (width == 0)
			{
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = color;
				Handles.DrawPolyLine(perimeter);
				Handles.zTest = CompareFunction.Greater;
				Handles.color = color / 2;
				Handles.DrawPolyLine(perimeter);
			}
			else
			{
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = color;
				Handles.DrawAAPolyLine(width, perimeter);
				Handles.zTest = CompareFunction.Greater;
				Handles.color = color / 2;
				Handles.DrawAAPolyLine(width, perimeter);
			}

			if (filled)
			{
				var fill = new UnityEngine.Vector3[]
				{
					VertexToPosition(cell.Vertices[0]), cell.CenterPoint,
					VertexToPosition(cell.Vertices[1]), cell.CenterPoint,
					VertexToPosition(cell.Vertices[2]), cell.CenterPoint,
					VertexToPosition(cell.Vertices[3]), cell.CenterPoint,
					VertexToPosition(cell.Vertices[4]), cell.CenterPoint,
					VertexToPosition(cell.Vertices[5]), cell.CenterPoint,
				};
				Handles.DrawLines(fill);
			}

			var cellDistance = Vector3.Distance((Vector3)cell.Center, _input.View.Origin);

			//Scale font size based on cell-camera distance
			var fontSize = Mathf.RoundToInt(Mathf.Lerp(10, 25, Mathf.InverseLerp(100, 3, cellDistance)));

			if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
			{
				Handles.zTest = CompareFunction.LessEqual;
				var colorLabelStyle = new GUIStyle(GUI.skin.label);
				//var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
				colorLabelStyle.normal.textColor = color;
				colorLabelStyle.fontSize = fontSize;
				Handles.Label(cell.CenterPoint, cell.HexPoses.ToString(), colorLabelStyle);
				Handles.color = color;
				Handles.DrawWireDisc(cell.CenterPoint, _input.View.Direction, 0.1f);

				Handles.zTest = CompareFunction.Greater;
				colorLabelStyle.normal.textColor /= 3;
				Handles.Label(cell.CenterPoint, cell.HexPoses.ToString(), colorLabelStyle);
				Handles.color /= 3;
				Handles.DrawWireDisc(cell.CenterPoint, _input.View.Direction, 0.1f);

				if (labelVertices)
				{
					Handles.zTest = CompareFunction.Always;
					Handles.color = color;
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[0]), cell.CenterPoint, 0.2f), cell.Vertices[0].Id.ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[1]), cell.CenterPoint, 0.2f), cell.Vertices[1].Id.ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[2]), cell.CenterPoint, 0.2f), cell.Vertices[2].Id.ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[3]), cell.CenterPoint, 0.2f), cell.Vertices[3].Id.ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[4]), cell.CenterPoint, 0.2f), cell.Vertices[4].Id.ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[5]), cell.CenterPoint, 0.2f), cell.Vertices[5].Id.ToString(), colorLabelStyle);
				}
			}

			Handles.zTest = oldzTest;
		}

		private void DrawNavigationCell(NavigationCell cell, Color color)
		{
			DrawMacroCell(cell.Cell.Macro, color, 0, false, false);
			DrawArrow.ForDebug(cell.Cell.Macro.CenterPoint, cell.Normal * 5, color);
		}

		private void DrawMicroCell(Micro.Cell cell, Color color)
		{
			Handles.color = color;
			foreach (var block in cell.GetBlocks())
			{
				//Cull backface blocks
				if (Vector3.CalculateAngle(_input.View.Direction, block.Normal) > Deg90ToRadians)
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
			Handles.DrawWireDisc(new Vector3(vert.Position.X, vert.Height.Nominal, vert.Position.Y), _input.View.Direction, 1);
		}

		/// <summary>
		/// Draw flat 2d block rectangle
		/// </summary>
		/// <param name="position"></param>
		/// <param name="color"></param>
		/// <param name="width"></param>
		/// <param name="normal"></param>
		private void DrawBlock(GridPos position, Color color, uint width = 0, Vector3? normal = null)
		{
			Handles.color = color;
			DrawRectangle.ForHandle(BlockInfo.GetBounds(position), color, width);

			if (normal.HasValue)
				DrawArrow.ForDebug(BlockInfo.GetWorldCenter(position), normal.Value);
		}

		private void DrawBlock(in BlockInfo block, Color color, uint width = 0, bool drawNormal = false)
		{
			Handles.color = color;

			var bounds = (Bounds)BlockInfo.GetBounds(block.Position);
			var corner00 = new Vector3(bounds.min.x, block.Corner00.Nominal, bounds.min.z);
			var corner10 = new Vector3(bounds.max.x, block.Corner10.Nominal, bounds.min.z);
			var corner11 = new Vector3(bounds.max.x, block.Corner11.Nominal, bounds.max.z);
			var corner01 = new Vector3(bounds.min.x, block.Corner01.Nominal, bounds.max.z);

			DrawRectangle.ForHandle(corner00, corner01, corner11, corner10, color, width);

			if (drawNormal && block.Normal != Vector3.Zero)
				DrawArrow.ForDebug(block.GetCenter(), block.Normal);
		}

		/*
        private void DrawBlock(Vector2i position, BaseBlockMap map, Color? overrideColor = null)
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
        */

		private void DrawBlock(GridPos position, in Blocks block, in Heights c00, in Heights c01, in Heights c11, in Heights c10, Color32? overrideColor = null)
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

			void DrawQuad(GridPos pos, Color32 color, float h00, float h01, float h11, float h10)
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

		private void DrawHeightVertex((GridPos position, Heights vertex, BaseBlockMap source) vertex, Color32? overrideColor = null)
		{
			if (Event.current.type == EventType.Repaint)
			{
				ref readonly var height = ref vertex.source.GetHeightRef(vertex.position);

				var basePos = new Vector3(vertex.position.X, height.Base, vertex.position.Z);
				var underPos = new Vector3(vertex.position.X, height.Underground, vertex.position.Z);
				var mainPos = new Vector3(vertex.position.X, height.Main, vertex.position.Z);

				//Draw base
				Handles.zTest = CompareFunction.LessEqual;
				Handles.color = overrideColor ?? Color.black;
				Handles.SphereHandleCap(0, basePos, Quaternion.identity, 0.2f, EventType.Repaint);

				Handles.zTest = CompareFunction.Greater;
				Handles.color /= 2;
				Handles.SphereHandleCap(0, basePos, Quaternion.identity, 0.2f, EventType.Repaint);

				//Draw under
				if (height.Underground > height.Base)
				{
					Handles.zTest = CompareFunction.LessEqual;
					Handles.color = overrideColor ?? Color.magenta;
					Handles.SphereHandleCap(0, underPos, Quaternion.identity, 0.2f, EventType.Repaint);
					Handles.DrawLine(basePos, underPos);

					Handles.zTest = CompareFunction.Greater;
					Handles.color /= 2;
					Handles.SphereHandleCap(0, underPos, Quaternion.identity, 0.2f, EventType.Repaint);
					Handles.DrawLine(basePos, underPos);
				}

				//Draw main
				if (height.Main > height.Base && height.Main > height.Underground)
				{
					Handles.zTest = CompareFunction.LessEqual;
					Handles.color = overrideColor ?? Color.green;
					Handles.SphereHandleCap(0, mainPos, Quaternion.identity, 0.2f, EventType.Repaint);
					Handles.DrawLine(underPos, mainPos);

					Handles.zTest = CompareFunction.Greater;
					Handles.color /= 2;
					Handles.SphereHandleCap(0, mainPos, Quaternion.identity, 0.2f, EventType.Repaint);
					Handles.DrawLine(underPos, mainPos);
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

			if (input.HoveredBlock.HasValue && input.HoveredBlock != input.SelectedBlock)
			{
				ShowBlockInfo(input.HoveredBlock.Value, false, false);
			}
		}

		private void ShowTerrainModeContent(Input input)
		{
			if (input.SelectedBlock.HasValue)
				ShowBlockInfo(input.SelectedBlock.Value, true, true);

			if (input.HoveredBlock.HasValue && input.HoveredBlock != input.SelectedBlock)
				ShowBlockInfo(input.HoveredBlock.Value, false, true);

			if (input.SelectedHeightVertex.HasValue)
				ShowHeightVertexInfo(input.SelectedHeightVertex.Value, true);

			if (input.HoveredHeightVertex.HasValue && input.HoveredHeightVertex != input.SelectedHeightVertex)
				ShowHeightVertexInfo(input.HoveredHeightVertex.Value, false);
		}

		private void ShowNavigationModeContent(Input input)
		{
			if (input.HoveredBlock != null)
			{
				ShowBlockInfo(input.HoveredBlock.Value, false, false);
				ShowNavigationBlockInfo( input.HoveredBlock.Value.position, false );
			}

			if (input.HoveredMacroCell != null)
			{
				var hoveredNode = _runner.NavMap.Nodes[input.HoveredMacroCell.HexPoses];
				ShowNavigationCellInfo(hoveredNode);

				//Show macro navigate cost info
				foreach (var (edge, neighbor) in _runner.NavMap.MacroGraph.GetNeighbors( hoveredNode ))
				{
					GUILayout.Label($"Edge to {neighbor.Cell.Id}, distance {edge.Distance:N2}, slope {edge.Slopeness}");
				}
			}
		}


		private void ShowMacroZoneInfo(Macro.Zone zone)
		{
			GUILayout.Label($"Macro.Zone {zone.Id}", EditorStyles.boldLabel);
			GUILayout.Label($"Cells: {zone.Cells.ToJoinedString(c => c.HexPoses.ToString())}");
			GUILayout.Label($"Biome: {zone.Biome.name}");
		}

		private void ShowMacroCellInfo(Cell cell)
		{
			GUILayout.Label($"Macro.Cell {cell.HexPoses}", EditorStyles.boldLabel);
			GUILayout.Label($"Zone: {cell.ZoneId} - {cell.Biome.name}");
			GUILayout.Label($"Height: {cell.DesiredHeight} desired, {cell.CenterPoint.Y:F1} true");
		}

		private void ShowTriVertInfo(MacroVert vert, float distance)
		{
			GUILayout.Label("MacroVert", EditorStyles.boldLabel);
			GUILayout.Label($"Id: {vert.Id}, pos: {vert.Position.ToString(GetZoomLevel(distance))}");
			GUILayout.Label($"Cells: {vert.Cells.Select(c => c.HexPoses).ToJoinedString()}");
			GUILayout.Label($"Influence: {vert.Influence}");
		}

		private void ShowCursorInfo(Input input)
		{
			GUILayout.Label("Cursor", EditorStyles.boldLabel);
			GUILayout.Label($"World pos: {input.CursorPosition.ToString(GetZoomLevel(input.Distance))}");
			GUILayout.Label($"Camera dist: {Vector3.Distance(input.CursorPosition, input.View.Origin):N} m");
			if (input.HoveredBlock.HasValue)
			{
				GUILayout.Label(
					$"Influence: {MacroMap.GetInfluence((Vector2)input.CursorPosition)}");
				GUILayout.Label($"Height: {MacroMap.GetHeight((Vector2)input.CursorPosition).Nominal:N}");
			}
		}

		private void ShowBlockInfo((GridPos position, Blocks block, BaseBlockMap source) block2, bool isSelected, bool showHeightmap)
		{
			var (position, block, source) = block2;
			GUILayout.Label(isSelected ? $"Selected block {position} - {source.Name}" : $"Hovered block {position} - {source.Name}", EditorStyles.boldLabel);

			if (!block.IsEmpty)
			{
				GUILayout.Label("Type                   Height");

				GUILayout.BeginVertical("box", GUILayout.Width(150));
				//GUILayout.BeginHorizontal();
				GUILayout.Label($"{block.Ground}", GUILayout.Width(100));
				//GUILayout.Label(block.Ground != BlockType.Empty ? $"{block.Height.Main:N1}" : "-");
				//GUILayout.EndHorizontal();

				//GUILayout.BeginHorizontal();
				GUILayout.Label($"{block.Underground}", GUILayout.Width(100));
				//GUILayout.Label(block.Underground != BlockType.Empty ? $"{block.Height.Underground:N1}" : "-");
				//GUILayout.EndHorizontal();

				//GUILayout.BeginHorizontal();
				GUILayout.Label($"{block.Base}", GUILayout.Width(100));
				//GUILayout.Label($"{block.Height.Base:N1}");
				//GUILayout.EndHorizontal();
				GUILayout.EndVertical();

				var occlusion = MicroMap.GetOverlapState(position);
				GUILayout.Label(occlusion.state != BlockOverlapState.None
					? $"{occlusion.map.Name} block is {occlusion.state}"
					: "No overlap");

				ref readonly var data = ref source.GetBlockData(position);
				var angle = MathHelper.RadiansToDegrees(Vector3.CalculateAngle(data.Normal, Vector3.UnitY));
				GUILayout.Label($"Angle {angle} °");

				if(block.IsObstacle)
					GUILayout.Label( "Is obstacle" );

				if (showHeightmap)
				{
					GUILayout.Label("Vertices:");
					//Show block vertices info
					var vertices = source.GetBlock(position);
					if (vertices.HasValue)
					{
						GUILayout.BeginVertical();
						GUILayout.BeginHorizontal();
						HeightToGUI("┌", vertices.Value.Corner01, false);
						HeightToGUI("┐", vertices.Value.Corner11, false);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						HeightToGUI("└", vertices.Value.Corner00, false);
						HeightToGUI("┘", vertices.Value.Corner10, false);
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
				}
			}
			else
				GUILayout.Label("Empty block");
		}

		private void ShowNavigationBlockInfo(GridPos position, bool isSelected )
		{
			GUILayout.Label(isSelected ? $"Selected nav block {position}" : $"Hovered nav block {position}", EditorStyles.boldLabel);

			ref readonly var navBlock = ref NavMap.NavGrid.GetBlock( position );
			
			GUILayout.Label( $"Slope: {navBlock.Normal.Slope}, orientation: {navBlock.Normal.Orientation}" );
		}

		private void ShowHeightVertexInfo((GridPos position, Heights vertex, BaseBlockMap source) vertex, bool isSelected)
		{
			if (isSelected)
				GUILayout.Label($"Selected height {vertex.position} - {vertex.source.Name}", EditorStyles.boldLabel);
			else
				GUILayout.Label($"Hovered height {vertex.position} - {vertex.source.Name}", EditorStyles.boldLabel);

			ref readonly var v = ref vertex.source.GetHeightRef(vertex.position);

			var newHeight = HeightToGUI(string.Empty, v, true);
			if (newHeight.HasValue)
			{
				vertex.source.SetHeights(new[] { vertex.position }, new[] { newHeight.Value });
				Debug.Log($"Changed height {vertex.position} to {newHeight.Value}");
			}
		}

		private void ShowNavigationCellInfo(NavigationCell cell)
		{
			GUILayout.Label($"Navigation cell {cell.Cell.Id}", EditorStyles.boldLabel);
			GUILayout.Label($"Avg speed {cell.SpeedModifier:N2}");
			GUILayout.Label($"Avg normal {cell.Normal.ToString(1)}");
			GUILayout.Label($"Roughness {cell.Rougness:N2}");
		}

		#endregion

		private string InfluenceToBiomesString(double[] influence)
		{
			var biomes = from biome in _runner.Biomes
						 where influence[biome.Index] > 0
						 select $"{biome.name}: {influence[biome.Index]:N3}";
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

		private static UnityEngine.Vector3 VertexToPosition(MacroVert vertex)
		{
			return new Vector3(vertex.Position.X, vertex.Height.Nominal, vertex.Position.Y);
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

				_defaultBlockColor.Clear();
				foreach (var blockSetting in _runner.AllBlocks)
					_defaultBlockColor[blockSetting.Block] = blockSetting.DefaultColor;
			}
			else if (state == PlayModeStateChange.ExitingPlayMode)
			{
				_runner = null;
			}
		}

		private InspectorMode GetInspectorMode(InspectorMode oldMode)
		{
			var modes = Enum.GetNames( typeof( InspectorMode ) );
			var newSelection = (InspectorMode)GUILayout.SelectionGrid((int)oldMode, modes, modes.Length, EditorStyles.radioButton);
			return newSelection;
		}

		#region Unity

		private void Awake()
		{
			_inspectorMode = (InspectorMode)PlayerPrefs.GetInt("InspectorMode", (int)InspectorMode.Default);
		}

		private void OnEnable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;

			EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
		}

		private void OnDestroy()
		{
			PlayerPrefs.SetInt("InspectorMode", (int)_inspectorMode);
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
			else if (_runner.RenderMode == Renderer.TerrainRenderMode.Terrain)
			{
				if (_inspectorMode == InspectorMode.Navigation)
					DrawNavigationModeHandles(_input);
				else
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
			if (_input.HoveredMacroCell != null)
			{
				DrawMacroCell(_input.HoveredMacroCell, Active, 5, true, true);
				DrawMacroZone(_input.HoveredMacroCell.Zone, _drawLayout ? Active : _input.HoveredMacroCell.Zone.Biome.LayoutColor);
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

			_inspectorMode = GetInspectorMode(_inspectorMode);

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
				if (_inspectorMode == InspectorMode.Default)
					ShowTerrainModeContent(_input);
				else if (_inspectorMode == InspectorMode.Navigation)
					ShowNavigationModeContent(_input);
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

				if (_input.HoveredBlock.HasValue)
				{
					ShowBlockInfo(_input.HoveredBlock.Value, false, true);
				}

				if (_input.HoveredMacroCell != null)
				{
					ShowMacroCellInfo(_input.HoveredMacroCell);
					ShowMacroZoneInfo(_input.HoveredMacroCell.Zone);
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
			public (GridPos position, Blocks block, BaseBlockMap source)? HoveredBlock;

			/// <summary>
			/// Selected block position
			/// </summary>
			public (GridPos position, Blocks block, BaseBlockMap source)? SelectedBlock;

			/// <summary>
			/// Distance between <see cref="View"/> and <see cref="CursorPosition"/>
			/// </summary>
			public float Distance;

			public Macro.Cell HoveredMacroCell;

			public Micro.Cell SelectedMicroCell;

			public MacroVert SelectedVert;

			public (GridPos position, Heights vertex, BaseBlockMap source)? HoveredHeightVertex;

			public (GridPos position, Heights vertex, BaseBlockMap source)? SelectedHeightVertex;

			public int FrameCount;
		}

		private enum InspectorMode
		{
			Default,
			Macro,
			Navigation
		}
	}
}
