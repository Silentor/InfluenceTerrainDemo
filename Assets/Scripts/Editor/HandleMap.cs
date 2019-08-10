using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Editor
{
	/// <summary>
	/// Debug visualization primitives
	/// 
	/// </summary>
	public static class HandleMap
	{
		public static void DrawNavigationNode( NavigationCell node, int width, Boolean showId )
		{
			DrawNavigationNode( node, width, Color.blue, showId );
		}

		public static void DrawNavigationNode( NavigationCell node, int width, Color color, Boolean showId )
		{
			DrawMacroCell( node.Cell.Macro, color, width, false );
			if(showId)
				DrawLabel( node.Cell.Id.ToString(  ), node.Cell.Macro.CenterPoint, color, 10 );
		}


		private static readonly Dictionary<Color32, GUIStyle> _labelStyles = new Dictionary<Color32, GUIStyle>();

		private static void DrawMacroCell(Cell cell, Color color, int width, bool filled)
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

                Handles.zTest = CompareFunction.LessEqual;
                Handles.color = color;
                Handles.DrawLines(fill);

                Handles.zTest = CompareFunction.Greater;
                Handles.color = color / 2;
                Handles.DrawLines(fill);
            }

            Handles.zTest = oldzTest;
        }

		private static void DrawLabel( string text, Vector3 position, Color color, int fontSize)
		{
			var style = GetLabelStyleFor( color );
			style.fontSize = fontSize;
			Handles.Label(position, text, style);
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
