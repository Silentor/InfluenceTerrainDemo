﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Cell = TerrainDemo.Macro.Cell;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Editor
{
	/// <summary>
	/// Debug visualization primitives
	/// 
	/// </summary>
	public static class DrawMap
	{
		public static void DrawNavigationNode( NavNode node, MicroMap map, uint width, Color color, Boolean showId )
		{
			DrawGridArea( node.Area, map, color, width );
			if(showId)
				DrawLabel( node.ToString(  ), node.Position3d, color, 10 );
		}

		public static void DrawMacroCell(MacroMap map, Cell cell, Spatial.Ray view)
		{
			DrawMacroCell(map, cell, cell.Biome.LayoutColor, view, 0, false, false);
		}

		public static void DrawMacroCell(MacroMap map, Cell cell, Color color, Spatial.Ray view, int width, bool labelVertices, bool filled)
		{
			var oldzTest = Handles.zTest;

			Handles.color = color;

			var points = new[]
			             {
				             VertexToPosition( cell.Vertices[0] ),
				             VertexToPosition( cell.Vertices[1] ),
				             VertexToPosition( cell.Vertices[2] ),
				             VertexToPosition( cell.Vertices[3] ),
				             VertexToPosition( cell.Vertices[4] ),
				             VertexToPosition( cell.Vertices[5] ),
			             };
			DrawPolyline.ForHandle( points, color, width, filled );
			var cellCenter = cell.Center.ToUnityVector3( map.GetHeight( cell.Center ).Nominal );

			var cellDistance = Vector3.Distance((Vector3)cell.Center, view.Origin);

			//Scale font size based on cell-camera distance
			var fontSize = Mathf.RoundToInt(Mathf.Lerp(10, 25, Mathf.InverseLerp(100, 3, cellDistance)));

			if (cellDistance < 80 && cellDistance > 3 && fontSize > 0)
			{
				Handles.zTest = CompareFunction.LessEqual;
				var colorLabelStyle = new GUIStyle(GUI.skin.label);
				//var contrastColor = (new Color(1, 1, 1, 2) - cell.Biome.LayoutColor) * 2;
				colorLabelStyle.normal.textColor = color;
				colorLabelStyle.fontSize = fontSize;
				Handles.Label(  cellCenter, cell.Position.ToString(), colorLabelStyle);
				Handles.color = color;
				Handles.DrawWireDisc(cellCenter, view.Direction, 0.1f);

				Handles.zTest = CompareFunction.Greater;
				colorLabelStyle.normal.textColor /= 3;
				Handles.Label(cellCenter, cell.Position.ToString(), colorLabelStyle);
				Handles.color /= 3;
				Handles.DrawWireDisc(cellCenter, view.Direction, 0.1f);

				if (labelVertices)
				{
					Handles.zTest = CompareFunction.Always;
					Handles.color = color;
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[0]), cellCenter, 0.2f), cell.Vertices[0].ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[1]), cellCenter, 0.2f), cell.Vertices[1].ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[2]), cellCenter, 0.2f), cell.Vertices[2].ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[3]), cellCenter, 0.2f), cell.Vertices[3].ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[4]), cellCenter, 0.2f), cell.Vertices[4].ToString(), colorLabelStyle);
					Handles.Label(Vector3.Lerp(VertexToPosition(cell.Vertices[5]), cellCenter, 0.2f), cell.Vertices[5].ToString(), colorLabelStyle);
				}
			}

			Handles.zTest = oldzTest;
		}
		/// <summary>
		/// Draw all blocks of area
		/// </summary>
		/// <param name="area"></param>
		/// <param name="map"></param>
		/// <param name="color"></param>
		/// <param name="width"></param>
		private static void DrawGridArea( GridArea area, MicroMap map, Color color, uint width )
		{
			_sidesLines.Clear(  );
			var viewDir = SceneView.currentDrawingSceneView.camera.transform.forward;

			foreach ( var blockSide in area.GetBorderSides( ) )
			{
				var block2 = map.GetBlockInfo( blockSide.Position );
				if(block2 == null)
					continue;

				var block = block2.Value;
				//Cull backface blocks
				if ( Vector3.CalculateAngle( viewDir, block.Normal ) > Deg90ToRadians )
				{
					if ( blockSide.Side == Direction.Forward )
					{
						_sidesLines.Add( block.Position01 );
						_sidesLines.Add( block.Position11 );
					}
					else if ( blockSide.Side == Direction.Right )
					{
						_sidesLines.Add( block.Position11 );
						_sidesLines.Add( block.Position10 );
					}
					else if ( blockSide.Side == Direction.Back )
					{
						_sidesLines.Add( block.Position10 );
						_sidesLines.Add( block.Position00 );
					}
					else 
					{
						_sidesLines.Add( block.Position00 );
						_sidesLines.Add( block.Position01 );
					}
				}
			}

			Handles.color = color;
			//if(width < 2)
				Handles.DrawLines( _sidesLines.ToArray(  ) );
			//else
			//	Handles.Draw( _sidesLines.ToArray(  ) );
		}

		private static readonly Dictionary<Color32, GUIStyle> _labelStyles = new Dictionary<Color32, GUIStyle>();
		private static readonly float Deg90ToRadians = MathHelper.DegreesToRadians(90);
		private static readonly List<UnityEngine.Vector3> _sidesLines = new List<UnityEngine.Vector3>();


		private static void DrawLabel( string text, Vector3 position, Color color, int fontSize)
		{
			var style = GetLabelStyleFor( color );
			style.fontSize = fontSize;
			Handles.Label(position, text, style);
		}

		public static void DrawBlock(in BlockInfo block, Color color, uint width = 0)
		{
			Handles.color = color;

			var bounds   = (Bounds)BlockInfo.GetBounds(block.Position);
			var corner00 = new Vector3(bounds.min.x, block.Corner00.Nominal, bounds.min.z);
			var corner10 = new Vector3(bounds.max.x, block.Corner10.Nominal, bounds.min.z);
			var corner11 = new Vector3(bounds.max.x, block.Corner11.Nominal, bounds.max.z);
			var corner01 = new Vector3(bounds.min.x, block.Corner01.Nominal, bounds.max.z);

			DrawRectangle.ForHandle(corner00, corner01, corner11, corner10, color, width);
		}

		private static UnityEngine.Vector3 VertexToPosition(MacroVert vertex)
		{
			return new Vector3(vertex.Position.X, vertex.Height.Nominal, vertex.Position.Y);
		}

		private static GUIStyle GetLabelStyleFor( Color color )
		{
			if ( !_labelStyles.TryGetValue( color, out var result ) )
			{
				result = new GUIStyle(GUI.skin.label);
				result.normal.textColor = color;
				_labelStyles.Add( color, result );
			}

			return result;
		}

	}

}
