using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace TerrainDemo.Spatial
{
	public partial class HexGrid<TCell, TEdge, TVertex>
	{
		public struct FloodFillEnumerator
		{
			private readonly HexGrid<TCell, TEdge, TVertex> _grid;
			private readonly List<List<HexPos>>             _neighbors;
			private readonly Predicate<TCell>               _fillCondition;

			/// <summary>
			/// Create flood-fill around <see cref="start"> cell
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="start"></param>
			public FloodFillEnumerator(HexGrid<TCell, TEdge, TVertex> grid, HexPos start, Predicate<TCell> fillCondition = null)
			{
				Assert.IsTrue(grid.IsContains(start));
				Assert.IsTrue(fillCondition == null || fillCondition(grid[start]));

				_grid          = grid;
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
				foreach (var neigh1 in faces)
				{
					foreach (var neigh2 in _grid.GetNeighborPositions( neigh1 ) )
					{
						if (_grid.IsContains(neigh2) 
						    && ( _fillCondition == null || _fillCondition(_grid[neigh2]) )
						    && !result.Contains(neigh2) && !faces.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
							result.Add(neigh2);
					}
				}

				return result;
			}
		}
	}
}
