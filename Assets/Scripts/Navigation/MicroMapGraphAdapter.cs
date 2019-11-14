using System.Collections.Generic;
using TerrainDemo.Hero;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
	/// <summary>
	/// Micro map adapter for generic A*
	/// </summary>
	public class MicroMapGraphAdapter : IAStarGraph<GridPos>
	{
		private readonly MicroMap _map;

		public MicroMapGraphAdapter(MicroMap map)
		{
			_map = map;
		}

		public IEnumerable<(GridPos neighbor, float neighborCost)> Neighbors( BaseLocomotor loco, GridPos @from )
		{
			foreach (var dir in Directions.Vector2I)
			{
				var neighborPos = from + dir;
				if (_map.Bounds.Contains(neighborPos) && loco.IsPassable(from, neighborPos))
					yield return (neighborPos, 1);
			}
		}
		public float Heuristic( GridPos @from, GridPos to )
		{
			//return Vector2i.ManhattanDistance(from.Position, to.Position); //7980 blocks, 186 msec, 203 meters
			return GridPos.Distance(@from, to); //11463 blocks, 194 msec, 171 meters
			//return Vector2i.RoughDistance(from.Position, to.Position);  //11703 blocks, 198 msec, 171 meters
			//return Vector2i.DistanceSquared(from.Position, to.Position); //231 blocks, 8 msec, 177 meters

		}
	}
}