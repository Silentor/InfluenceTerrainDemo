using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainDemo.Tools
{
    public static class DrawPolyline
    {
        public static void ForGizmo([NotNull] Vector3[] line, bool closePolygon = false)
        {
            ForGizmo(line, Gizmos.color, closePolygon);
        }

        public static void ForGizmo([NotNull] Vector3[] line, Color color, bool closePolygon)
        {
            if (line == null) throw new ArgumentNullException(nameof( line ));
            if(line.Length < 2)
                return;

            Gizmos.color = color;
            for (int i = 0; i < line.Length - 1; i++)
                Gizmos.DrawLine(line[i], line[i + 1]);
            if(closePolygon)
                Gizmos.DrawLine(line[line.Length - 1], line[0]);
        }

        public static void ForDebug([NotNull] Vector3[] line, bool closePolygon = false)
        {
            ForDebug(line, Gizmos.color, closePolygon);
        }

        public static void ForDebug([NotNull] Vector3[] line, Color color, bool closePolygon)
        {
            if (line == null) throw new ArgumentNullException(nameof( line ));
            if (line.Length < 2)
                return;

            for (int i = 0; i < line.Length - 1; i++)
                Debug.DrawLine(line[i], line[i + 1], color, 0);
            if (closePolygon)
                Debug.DrawLine(line[line.Length - 1], line[0], color, 0);
        }

        public static void ForHandle(Vector3[] points, Color color, int width, Vector3? fillCenter = null )
        {
	        var oldzTest = Handles.zTest;
	        Handles.color = color;

	        //Draw perimeter
	        if ( width == 0 )
	        {
		        Handles.zTest = CompareFunction.LessEqual;
		        Handles.color = color;
		        Handles.DrawPolyLine( points );
		        Handles.zTest = CompareFunction.Greater;
		        Handles.color = color / 2;
		        Handles.DrawPolyLine( points );
	        }
	        else
	        {
		        Handles.zTest = CompareFunction.LessEqual;
		        Handles.color = color;
		        Handles.DrawAAPolyLine( width, points );
		        Handles.zTest = CompareFunction.Greater;
		        Handles.color = color / 2;
		        Handles.DrawAAPolyLine( width, points );
	        }

	        if ( fillCenter.HasValue )
	        {
		        var centerPoint = fillCenter.Value ;
		        var fill        = points.SelectMany( p => new[] {p, centerPoint} ).ToArray( );
		        
		        Handles.zTest = CompareFunction.LessEqual;
		        Handles.color = color;
		        Handles.DrawLines( fill );
		        Handles.zTest = CompareFunction.Greater;
		        Handles.color = color / 2;
		        Handles.DrawAAPolyLine( width, points );
	        }

	        Handles.zTest = oldzTest;
        }
    }
}
