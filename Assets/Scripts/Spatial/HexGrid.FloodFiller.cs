using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace TerrainDemo.Spatial
{
	public partial class HexGrid<TCell, TEdge, TVertex>
	{
		public struct FloodFiller
		{
			private readonly HexGrid<TCell, TEdge, TVertex> _grid;
			private readonly List<List<HexPos>>             _neighbors;
			private readonly CheckCellPredicate				_fillCondition;		//User condition
			private readonly Predicate<HexPos>              _boundCondition;				//System condition (clusters support)

			/// <summary>
			/// Create flood-fill around <see cref="start"> cell
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="start"></param>
			public FloodFiller(HexGrid<TCell, TEdge, TVertex> grid, HexPos start, Predicate<HexPos> boundCondition, CheckCellPredicate fillCondition = null)
			{
				Assert.IsTrue(boundCondition(start));
				Assert.IsTrue(fillCondition == null || fillCondition(start, grid[start]));

				_grid          = grid;
				_boundCondition = boundCondition;
				_neighbors     = new List<List<HexPos>> {new List<HexPos> {start}};
				_fillCondition = fillCondition;
			}

			/// <summary>
			/// Create flood-fill around <see cref="start"> cells
			/// </summary>
			/// <param name="mesh"></param>
			/// <param name="start"></param>
			//public FloodFillEnumerator(Mesh<TFaceData, TEdgeData, TVertexData> mesh, Mesh<,,>.Face[] start)
			//{
			//	Assert.IsTrue(start.All(mesh.Contains));

			//	_mesh = mesh;
			//	var startStep = new List<Mesh<,,>.Face>();
			//	startStep.AddRange(start);
			//	_neighbors.Add(startStep);
			//}


			/// <summary>
			/// Get cells from start cell(s) by distance
			/// </summary>
			/// <param name="distance">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
			/// <returns></returns>
			public IEnumerable<HexPos> GetNeighbors( int distance )
			{
				if (distance == 0)
					return _neighbors[0];

				if (distance < _neighbors.Count)
					return _neighbors[distance];

				//Calculate neighbors
				if (distance - 1 < _neighbors.Count)
				{
					var processedCellsIndex = Math.Max(0, distance - 2);
					var result              = GetNeighbors(_neighbors[distance - 1], _neighbors[processedCellsIndex]);
					_neighbors.Add(result);
					return result;
				}
				else
				{
					//Calculate previous steps (because result of step=n used for step=n+1)
					for (var i = _neighbors.Count; i < distance; i++)
						GetNeighbors(i);
					return GetNeighbors(distance);
				}
			}

			/// <summary>
			/// Get neighbors of <see cref="faces"/> doesnt contained in <see cref="alreadyProcessed"/>
			/// </summary>
			/// <param name="faces"></param>
			/// <param name="alreadyProcessed"></param>
			/// <returns></returns>
			private List<HexPos> GetNeighbors(List<HexPos> faces, List<HexPos> alreadyProcessed)
			{
				var result = new List<HexPos>();
				foreach (var checkPos in faces)
				{
					foreach (var resultPos in _grid.GetNeighborPositions( checkPos ) )
					{
						if (   _boundCondition(resultPos) 
						    && ( _fillCondition == null || _fillCondition(resultPos, _grid[resultPos]) )
						    && !result.Contains(resultPos) && !faces.Contains(resultPos) && !alreadyProcessed.Contains(resultPos))
							result.Add(resultPos);
					}
				}

				return result;
			}
		}

		public delegate bool CheckCellPredicate(HexPos position, TCell data);
	}
}
