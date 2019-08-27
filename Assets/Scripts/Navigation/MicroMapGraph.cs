using System.Collections.Generic;
using TerrainDemo.Hero;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
	public class MicroMapGraph : IWeightedGraph<Waypoint>
	{
		private readonly MicroMap _map;

		public MicroMapGraph(MicroMap map)
		{
			_map = map;
		}

		public IEnumerable<(Waypoint neighbor, float neighborCost)> Neighbors(Locomotor loco, Waypoint node)
		{
			foreach (var dir in Directions.Vector2I)
			{
				var neighborPos = node.Position + dir;
				if (_map.Bounds.Contains(neighborPos) && loco.IsPassable(node.Map, node.Position, neighborPos, out var toMap))
					yield return (new Waypoint(toMap, neighborPos), 1);
			}
		}

		public float Heuristic(Waypoint @from, Waypoint to)
		{
			//return Vector2i.ManhattanDistance(from.Position, to.Position); //7980 blocks, 186 msec, 203 meters
			return GridPos.Distance(@from.Position, to.Position); //11463 blocks, 194 msec, 171 meters
			//return Vector2i.RoughDistance(from.Position, to.Position);  //11703 blocks, 198 msec, 171 meters
			//return Vector2i.DistanceSquared(from.Position, to.Position); //231 blocks, 8 msec, 177 meters
		}
	}
}