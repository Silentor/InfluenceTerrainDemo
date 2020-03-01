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

		private void OnSceneGUI(SceneView sceneView)
		{
			var worldRay  = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			var zeroPlane = new Plane(Vector3.up, Vector3.zero);
			if (zeroPlane.Raycast(worldRay, out var length))
			{
				var worldPoint = worldRay.GetPoint(length);
				var gridPos    = (GridPos)worldPoint;
				var hex        = HexGrid.BlockToHex(gridPos);

				MousePos = gridPos;
				Hex      = hex;
			}

			var labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.active.textColor = Color.magenta;
			Handles.color = Color.magenta;
			
			for ( int q = -4; q <= 4; q++ )
				for ( int r = -4; r <= 4; r++ )
				{
					var hexPos = new HexPos( q, r );
					var hexCenter = HexGrid.GetHexCenter( hexPos );
					Handles.Label( hexCenter.ConvertTo3D(  ), hexPos.ToString(  ) );
					Handles.SphereHandleCap( 0, hexCenter, Quaternion.identity, 0.5f, EventType.Repaint );

					var hexCenterBlock = HexGrid.GetHexCenterBlock( hexPos );
					DrawRectangle.ForHandle( new Bounds2i(hexCenterBlock, 0), Color.magenta );

					//if ( hexPos == HexPos.Zero )
					{
						var hexVertices = HexGrid.GetHexVertices( hexPos );

						//for ( var i = 0; i < hexVertices.Length; i++ )
						{
							//var v1 = hexVertices[i];
							//var v2 = hexVertices[(i + 1)% 6];
							Handles.DrawPolyLine( hexVertices.Select( v => (Vector3)v ).ToArray(  ) );
						}
					}
				}
		}

		void Update( )
		{
			for ( int x = -40; x < 40; x++ )
				for ( int z = -30; z < 30; z++ )
				{
					var gridPos = new GridPos(x, z);
					var hex     = HexGrid.BlockToHex( gridPos );

					var gridPosBounds = new Bounds2i(gridPos, 0);
					if(hex == new HexPos(0, 0))
						DrawRectangle.ForDebug( gridPosBounds, Color.white, true );
					else if(hex == new HexPos(1, 0))
						DrawRectangle.ForDebug( gridPosBounds, Color.blue, true );
					else if(hex == new HexPos(0, 1))
						DrawRectangle.ForDebug( gridPosBounds, Color.green, true );
					else if(hex == new HexPos(1, 1))
						DrawRectangle.ForDebug( gridPosBounds, Color.red, true );
					else if(hex == new HexPos(-1, 0))
						DrawRectangle.ForDebug( gridPosBounds, Color.cyan, true );
					else if(hex == new HexPos(0, -1))
						DrawRectangle.ForDebug( gridPosBounds, Color.green / 2, true );
					else if(hex == new HexPos(-1, -1))
						DrawRectangle.ForDebug( gridPosBounds, Color.yellow, true );
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
		}

		public override bool RequiresConstantRepaint( ) => true;
	}
}


