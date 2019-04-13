using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditorInternal;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Main pathfinding logic
    /// </summary>
    public class Pathfinder
    {
        public static Pathfinder Instance
        {
            get
            {
                if(_instance == null)
                    throw new InvalidOperationException("Pathfinder do not created properly");
                return _instance;
            }
        }

        public NavigationMap NavigationMap => _navmap;

        public Pathfinder([NotNull] MicroMap micromap, [NotNull] MacroMap macromap, TriRunner settings)
        {
            if (_instance != null)
                throw new InvalidOperationException("Pathfinder double creation");

            _instance = this;

            _map = micromap ?? throw new ArgumentNullException(nameof(micromap));
            _macromap = macromap ?? throw new ArgumentNullException(nameof(macromap));

            _macroAstar = new AStarSearch<MacroMapGraph, Macro.Cell>(new MacroMapGraph(macromap));
            _microAStar = new AStarSearch<MicroMapGraph, Waypoint>(new MicroMapGraph(micromap));

            _navmap = new NavigationMap(macromap, micromap, settings);
            _macroNavAstar = new AStarSearch<NavigationMapMacroGraph, NavigationCell>(new NavigationMapMacroGraph(_navmap));
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
            Path result;
            if (IsStraightPathExists(actor, new Waypoint(actor.Map, from), new Waypoint(actor.Map, to)))
                result = new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor, true);
            else
            {

                //DEBUG
                
                var startCell =  _macromap.Cells.First(c => c.Contains(from));
                var startNavCell = _navmap.Cells[startCell.Coords];
                var finishCell = _macromap.Cells.First(c => c.Contains(to));
                var finishNavCell = _navmap.Cells[finishCell.Coords];
                var path = _macroNavAstar.CreatePath(actor, startNavCell, finishNavCell);
                if (path == null || path.Count < 2)
                    return new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor, false);
                result = new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor, true);
                result.AddSegment(result.Start, result.Finish, path.Select(cell => new Waypoint(_map, (Vector2i)cell.Cell.Macro.Center)));

                //DEBUG

                /*
                var astarPoints = _microAStar.CreatePath(actor, new Waypoint(actor.Map, actor.BlockPosition), new Waypoint(_map, to));
                if (astarPoints == null || astarPoints.Count < 2)
                    return new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor, false);

                //A* on grid produces very blocky pass and many redundant waypoints, need smoothing
                astarPoints = SimplifyStraightLines(astarPoints);
                astarPoints = SimplifyCorners(astarPoints, actor);
                result = new Path(new Waypoint(actor.Map, from), new Waypoint(actor.Map, to), actor, true);
                result.AddSegment(result.Start, result.Finish, astarPoints);
                */
            }

            Debug.Log($"Valid path created for {actor.Name}: wps {result.Waypoints.Count()}, length {result.GetPathLength()}");

            return result;
        }


        public static bool IsStraightPathExists(Actor actor, Waypoint from, Waypoint to)
        {
            if (from == to)
                return true;

            var raster = Intersections.GridIntersections(from.Position, to.Position);
            var currentMap = from.Map;
            var currentPos = from.Position;
            foreach (var intersection in raster)
            {
                var nextPosition = intersection.blockPosition;
                if (actor.IsPassable(currentMap, currentPos, nextPosition, out var nextMap))
                {
                    currentMap = nextMap;
                    currentPos = nextPosition;
                }
                else
                    return false;
            }

            return true;
        }

        private readonly MicroMap _map;
        private readonly MacroMap _macromap;
        private readonly AStarSearch<MicroMapGraph, Waypoint> _microAStar;
        private static Pathfinder _instance;
        private readonly AStarSearch<MacroMapGraph, Cell> _macroAstar;
        private readonly NavigationMap _navmap;
        private readonly AStarSearch<NavigationMapMacroGraph, NavigationCell> _macroNavAstar;

        /// <summary>
        /// Simplify path straight lines
        /// </summary>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        private List<Waypoint> SimplifyStraightLines(List<Waypoint> waypoints)
        {
            for (int i = 1; i < waypoints.Count - 1; i++)
            {
                var dir1 = waypoints[i - 1].Position - waypoints[i].Position;
                var dir2 = waypoints[i].Position - waypoints[i + 1].Position;
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
        /// <param name="actor"></param>
        /// <returns></returns>
        private List<Waypoint> SimplifyCorners(List<Waypoint> waypoints, Actor actor)
        {
            //Simplify corners
            var i = 0;
            while (i < waypoints.Count - 2)
            {
                if (IsStraightPathExists(actor, waypoints[i], waypoints[i + 2]))
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
