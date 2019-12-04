using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditorInternal;
using Cell = TerrainDemo.Macro.Cell;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Main pathfinding logic
    /// </summary>
    public class Pathfinder
    {
        public Pathfinder([NotNull] NavigationMap navMap, MicroMap baseMap, TriRunner settings)
        {
            _microAStar = new AStarSearch<GridPos>(  );
            _macroNavAstar2 = new AStarSearch<NavNode>( );
        }


        /// <summary>
        /// Create path on micromap for given actor
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <returns></returns>
        public Path CreatePath(Vector2i from, Vector2i to, Actor actor)
        {
            var timer = Stopwatch.StartNew();
            Path result;
            //Simple path
            //if (IsStraightPathExists(actor, new Waypoint(actor.Map, from), new Waypoint(actor.Map, to)))
                //result = new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor);
            //else
            {
                //Complex path

                //DEBUG
                
                //var startCell =  _macromap.Cells.First(c => c.Contains(from));
                //var startNavCell = _navmap.Cells[startCell.Coords];
                //var finishCell = _macromap.Cells.First(c => c.Contains(to));
                //var finishNavCell = _navmap.Cells[finishCell.Coords];

                //var (macroPath, _, _) = _macroNavAstar2.CreatePath(actor, startNavCell, finishNavCell, true);

                //if (macroPath == null || macroPath.Count < 2)
                //{
                //    Debug.Log($"Path is invalid");
                //    return Path.CreateInvalidPath(from, to, actor);
                //}

                ////Prepare list of inter points
                //var interPoints = new List<Vector2i>();
                //for (int i = 0; i < macroPath.Count - 1; i++)
                //{
                //    var fromCell = macroPath[i].Cell;
                //    var toCell = macroPath[i + 1].Cell;

                //    var transferEdge = fromCell.Macro.Edges.First(e => e.GetOppositeOf(fromCell.Macro) == toCell.Macro);
                //    interPoints.Add((Vector2i)(transferEdge.Vertex1.Position + transferEdge.Vertex2.Position) / 2);
                //}

                //result = new Path(from, to, actor, interPoints.Select(p => new Waypoint(_map, p)));

                
                ////Refine all segments at once
                //foreach (var segment in result.Segments)
                //{
                //    //todo also check for straight path

                //    var (intraPath, _, costs) = _microAStar.CreatePath(actor, segment.From, segment.To, false, 
                //        w => Vector2i.Distance(w.Position, segment.From.Position) < 20 || Vector2i.Distance(w.Position, segment.To.Position) < 20);

                //    result.TotalProcessed.UnionWith(costs.Keys);

                //    if (intraPath == null)
                //    {
                //        Debug.LogWarning($"Path cant be refined");
                //        continue;
                //    }

                //    intraPath = SimplifyStraightLines(intraPath);
                //    intraPath = SimplifyCorners(intraPath, actor);
                //    segment.Refine(intraPath);
                //}
                
                //DEBUG

                
                //var startPoint = new Waypoint(actor.Map, from);
                //var finishPoint = new Waypoint(_map, to);
                //var astarPoints = _microAStar.CreatePath(actor, startPoint, finishPoint);
                //if (astarPoints == null || astarPoints.Count < 2)
                //    return Path.CreateInvalidPath(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor);

                ////A* on grid produces very blocky pass and many redundant waypoints, need smoothing
                //astarPoints = SimplifyStraightLines(astarPoints);
                //astarPoints = SimplifyCorners(astarPoints, actor);
                //result = new Path(startPoint, finishPoint, actor, new[]{startPoint, finishPoint});
                //result.Segments.First().Refine(astarPoints);
                
            }

            //timer.Stop();
            //Debug.Log($"Valid path created for {actor.Name}: wps {result.Waypoints.Count()}, length {result.GetPathLength()}, total time {timer.ElapsedMilliseconds}");

            //return result;

            return null;
        }

        public static bool IsStraightPathExists(BaseLocomotor loco, GridPos from, GridPos to)
        {
            if (from == to)
                return true;

            var raster = Intersections.GridIntersections(from, to);
            //var currentMap = from.Map;
            foreach (var intersection in raster)
            {
                if (loco.IsPassable(/*currentMap, */intersection.prevBlock, intersection.nextBlock/*, out var nextMap*/))
                {
                    //currentMap = nextMap;
                }
                else
                    return false;
            }

            return true;
        }

        public AStarSearch<NavNode>.SearchResult GetMacroRoute(NavNode from, NavNode to, Actor actor )
        {
	        var result = _macroNavAstar2.CreatePath( actor.Locomotor, from, to );
	        return result;
        }

        public AStarSearch<GridPos>.SearchResult GetMicroRoute( GridPos from, GridPos to, BaseLocomotor loco )
        {
			//Fast pass
			if(IsStraightPathExists( loco, from, to ))
				return new AStarSearch<GridPos>.SearchResult( 
					new List<GridPos>(){from, to},
					SearchState.Success,
					0, 
					new Dictionary<GridPos, GridPos>(  ), 
					new Dictionary<GridPos, float>(  )
					);

			const int searchRadius = 30 * 30;
			var result = _microAStar.CreatePath( loco, from, to, 
			             pos => GridPos.DistanceSquared( from, pos ) < searchRadius && GridPos.DistanceSquared( to, pos ) < searchRadius);

			var timer = Stopwatch.StartNew( );
			if ( result.Route != null )
			{
				SimplifyStraightLines( result.Route );
				SimplifyCorners( result.Route, loco );
			}
			timer.Stop( );

			return new AStarSearch<GridPos>.SearchResult( 
				result.Route, 
				result.Result,
				result.ElapsedTimeMs + timer.ElapsedMilliseconds, 
				result.CameFromDebug, result.CostsDebug );
        }

        private readonly AStarSearch<GridPos> _microAStar;
        private readonly AStarSearch<NavNode> _macroNavAstar2;

        /// <summary>
        /// Simplify path straight lines
        /// </summary>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        private List<GridPos> SimplifyStraightLines(List<GridPos> waypoints)
        {
            for (int i = 1; i < waypoints.Count - 1; i++)
            {
                var dir1 = waypoints[i - 1] - waypoints[i];
                var dir2 = waypoints[i] - waypoints[i + 1];
                if (Math.Sign(dir1.X) == Math.Sign(dir2.X) && Math.Sign(dir1.Z) == Math.Sign(dir2.Z))
                {
                    waypoints.RemoveAt(i);
                    i--;
                }
            }

            return waypoints;
        }

        /// <summary>
        /// Remove excess corners
        /// </summary>
        /// <param name="waypoints"></param>
        /// <param name="loco"></param>
        /// <returns></returns>
        private List<GridPos> SimplifyCorners(List<GridPos> waypoints, BaseLocomotor loco)
        {
            //Simplify corners
            var i = 0;
            while (i < waypoints.Count - 2)
            {
                if (IsStraightPathExists(loco, waypoints[i], waypoints[i + 2]))
                {
                    //Debug.Log($"There is path from {waypoints[i].Position} to {waypoints[i + 2].Position}, remove {waypoints[i + 1].Position}");
                    waypoints.RemoveAt(i + 1);
                }
                else
                    i++;
            }

            return waypoints;
        }

    }
    
}
