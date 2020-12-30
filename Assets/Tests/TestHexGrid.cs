using System;
using System.Linq;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tests
{
	public class TestHexGrid : MonoBehaviour
	{
		public GridPos MousePos;
		public HexPos  Hex;
		private HexGrid<HexPos, int, int> _hex;

		private void Awake( )
		{
			_hex = new HexGrid<HexPos, int, int>( 10, 5 );

			for ( int q = -10; q < 10; q++ )
			{
				for ( int r = -10; r < 10; r++ )
				{
					var h = new HexPos(q, r);
					if ( _hex.IsContains( h ) )
						_hex[h] = h;
				}
				
			}
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			var worldRay  = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			var zeroPlane = new Plane(Vector3.up, Vector3.zero);
			if (zeroPlane.Raycast(worldRay, out var length))
			{
				var worldPoint = worldRay.GetPoint(length);
				var gridPos    = (GridPos)worldPoint;
				var hex        = _hex.BlockToHex(gridPos);

				MousePos = gridPos;
				Hex      = hex;
			}

			var labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.active.textColor = Color.magenta;

			foreach ( var pos in _hex )
			{
				DrawHex( pos, Color.magenta );
			}

			//Show rasterized line
			Handles.color = Color.white;
			var line = _hex.RasterizeLine( HexPos.Zero, Hex );
			for ( var i = 0; i < line.Length - 1; i++ )
			{
				var linePos = line[i];
				DrawHex( linePos, Color.white );

				Handles.DrawLine( _hex.GetHexCenter(line[i]).ToVector3( ), _hex.GetHexCenter(line[i + 1]).ToVector3(  ) );
			}
			DrawHex( line.Last(), Color.white );
		}
		private void DrawHex( HexPos hexPos, Color color )
		{
			Handles.color = color;
			var hexCenter = _hex.GetHexCenter( hexPos );
			Handles.Label( hexCenter.ToVector3( ), hexPos.ToString( ) );
			Handles.SphereHandleCap( 0, hexCenter, Quaternion.identity, 0.5f, EventType.Repaint );

			var hexCenterBlock = _hex.GetHexCenterBlock( hexPos );
			DrawRectangle.ForHandle( new Bound2i( hexCenterBlock, 0 ), color );

			var hexVertices = _hex.GetHexVerticesPosition( hexPos );
			Handles.DrawPolyLine( hexVertices.Select( v => (Vector3) v ).ToArray( ) );
		}

		void Update( )
		{
			for ( int x = -40; x < 40; x++ )
				for ( int z = -30; z < 30; z++ )
				{
					var gridPos = new GridPos(x, z);
					var hex     = _hex.BlockToHex( gridPos );

					var gridPosBounds = new Bound2i(gridPos, 0);
					if(hex == new HexPos(0, 0))
						DrawRectangle.ForDebug( gridPosBounds, Color.white, true );
					//else if(hex == new HexPos(1, 0))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.blue, true );
					//else if(hex == new HexPos(0, 1))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.green, true );
					//else if(hex == new HexPos(1, 1))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.red, true );
					//else if(hex == new HexPos(-1, 0))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.cyan, true );
					//else if(hex == new HexPos(0, -1))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.green / 2, true );
					//else if(hex == new HexPos(-1, -1))
					//	DrawRectangle.ForDebug( gridPosBounds, Color.yellow, true );
					//else
					//{
					//	var hash = Math.Abs((hex.Q + hex.R) % 3) / 2;
					//	var color = Color.white * hash;
					//	color.a = 1;
					//	DrawRectangle.ForDebug( gridPosBounds, color, true );
					//}


				}
		}

		private void OnEnable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;
		}

		private void OnDisable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
		}
	}

	[CustomEditor(typeof(TestHexGrid))]
	public class TestHexGridInspector : Editor
	{
		private TestHexGrid _target;
		private void OnEnable( )
		{
			_target = (TestHexGrid) target;
		}

		public override void OnInspectorGUI( )
		{
			base.OnInspectorGUI( );

			EditorGUILayout.LabelField( "Mouse:", _target.MousePos.ToString(  ) );
			EditorGUILayout.LabelField( "Hex:", _target.Hex.ToString(  ) );
			EditorGUILayout.LabelField( "Cube:", _target.Hex.ToCube(  ).ToString(  ) );
			EditorGUILayout.LabelField( "Distance:", _target.Hex.Distance( HexPos.Zero ).ToString() );
		}

		public override bool RequiresConstantRepaint( ) => true;
	}
}


