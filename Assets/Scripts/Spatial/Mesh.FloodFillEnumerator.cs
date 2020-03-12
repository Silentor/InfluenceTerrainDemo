using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace TerrainDemo.Spatial
{
	public partial class Mesh<TFaceData, TEdgeData, TVertexData>
	{
		public class FloodFillEnumerator
		{
			private readonly Mesh<TFaceData, TEdgeData, TVertexData> _mesh;
			private readonly List<List<Face>>                        _neighbors = new List<List<Face>>();
			private readonly Predicate<TFaceData>                    _fillCondition;

			/// <summary>
			/// Create flood-fill around <see cref="start"> cell
			/// </summary>
			/// <param name="mesh"></param>
			/// <param name="start"></param>
			public FloodFillEnumerator(Mesh<TFaceData, TEdgeData, TVertexData> mesh, Face start, Predicate<TFaceData> fillCondition = null)
			{
				Assert.IsTrue(mesh.Contains(start));
				Assert.IsTrue(fillCondition == null || fillCondition(start.Data));

				_mesh = mesh;
				_neighbors.Add(new List<Face> { start });
				_fillCondition = fillCondition;
			}

			/// <summary>
			/// Create flood-fill around <see cref="start"> cells
			/// </summary>
			/// <param name="mesh"></param>
			/// <param name="start"></param>
			public FloodFillEnumerator(Mesh<TFaceData, TEdgeData, TVertexData> mesh, Face[] start)
			{
				Assert.IsTrue(start.All(mesh.Contains));

				_mesh = mesh;
				var startStep = new List<Face>();
				startStep.AddRange(start);
				_neighbors.Add(startStep);
			}


			/// <summary>
			/// Get cells from start cell(s) by distance
			/// </summary>
			/// <param name="distance">0 - start cell(s), 1 - direct neighbors, 2 - neighbors of step 1 neighbors, etc</param>
			/// <returns></returns>
			public IEnumerable<Face> GetNeighbors(int distance)
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
					for (int i = _neighbors.Count; i < distance; i++)
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
			private List<Face> GetNeighbors(List<Face> faces, List<Face> alreadyProcessed)
			{
				var result = new List<Face>();
				foreach (var neigh1 in faces)
				{
					foreach (var neigh2 in neigh1.Neighbors)
					{
						if (_mesh.Contains(neigh2) 
						    && (_fillCondition == null || _fillCondition(neigh2.Data))
						    && !result.Contains(neigh2) && !faces.Contains(neigh2) && !alreadyProcessed.Contains(neigh2))
							result.Add(neigh2);
					}
				}

				return result;
			}
		}
	}
}
