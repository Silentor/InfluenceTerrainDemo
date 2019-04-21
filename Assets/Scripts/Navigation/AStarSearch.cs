using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Debug = UnityEngine.Debug;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

//Based on https://www.redblobgames.com/pathfinding/from-star/implementation.html#csharp

namespace TerrainDemo.Navigation
{
    public interface IWeightedGraph<TNode> where TNode : IEquatable<TNode>
    {
        float Cost(TNode @from, TNode to);

        IEnumerable<TNode> Neighbors(Actor actor, TNode node);

        float Heuristic(TNode @from, TNode to);

        void DebugVisualize(IEnumerable<TNode> nodes, Color color);
    }

    public class MicroMapGraph : IWeightedGraph<Waypoint>
    {
        private readonly MicroMap _map;

        public MicroMapGraph(MicroMap map)
        {
            _map = map;
        }

        public float Cost(Waypoint @from, Waypoint to)
        {
            return 1;       //Calculate true cost
        }

        public IEnumerable<Waypoint> Neighbors(Actor actor, Waypoint node)
        {
            foreach (var dir in Directions.Vector2I)
            {
                var neighborPos = node.Position + dir;
                if (_map.Bounds.Contains(neighborPos) && actor.IsPassable(node.Map, node.Position, neighborPos, out var toMap))
                    yield return new Waypoint(toMap, neighborPos);
            }
        }

        public float Heuristic(Waypoint @from, Waypoint to)
        {
            //return Vector2i.ManhattanDistance(from.Position, to.Position); //7980 blocks, 186 msec, 203 meters
            return Vector2i.Distance(@from.Position, to.Position); //11463 blocks, 194 msec, 171 meters
            //return Vector2i.RoughDistance(from.Position, to.Position);  //11703 blocks, 198 msec, 171 meters
            //return Vector2i.DistanceSquared(from.Position, to.Position); //231 blocks, 8 msec, 177 meters
        }

        public void DebugVisualize(IEnumerable<Waypoint> nodes, Color color)
        {
            foreach (var node in nodes)
            {
                DebugExtension.DebugPoint(BlockInfo.GetWorldCenter(node.Position), color, 0.5f, 10, false);
            }
        }
    }

    public class MacroMapGraph : IWeightedGraph<Cell>
    {
        private MacroMap _map;

        public MacroMapGraph(MacroMap map)
        {
            _map = map;
        }

        public float Cost(Cell @from, Cell to)
        {
            if (from.Equals(to))
                return 0;

            var moveVector = to.CenterPoint - from.CenterPoint;
            var heightDiff = moveVector.Y;
            moveVector.Y = 0;
            var flatDistance = moveVector.Length;

            //Remap distance based on height difference. Prefer down slopes
            var inclinationCoeff = (heightDiff / flatDistance) * 2;
            return Mathf.Clamp(flatDistance + inclinationCoeff, flatDistance / 2, float.MaxValue) ;
        }

        public IEnumerable<Cell> Neighbors(Actor actor, Cell node)
        {
            return node.NeighborsSafe;
        }

        public float Heuristic(Cell @from, Cell to)
        {
            return Vector3.Distance(@from.CenterPoint, to.CenterPoint);
        }

        public void DebugVisualize(IEnumerable<Cell> nodes, Color color)
        {
            foreach (var node in nodes)
            {
                DebugExtension.DebugPoint(node.CenterPoint, color, 2f, 10, false);
            }
        }
    }

    public class NavigationMapMacroGraph : IWeightedGraph<NavigationCell>
    {
        private readonly NavigationMap _map;

        public NavigationMapMacroGraph(NavigationMap map)
        {
            _map = map;
        }

        public float Cost(NavigationCell from, NavigationCell to)
        {
            var speedCost = 1 / to.SpeedModifier;
            //var rougnessCost = 1 + (from.Rougness + to.Rougness) / 2;
            var rougnessCost = 1 + (5 * to.Rougness);

            var moveVector = to.Cell.Macro.CenterPoint - from.Cell.Macro.CenterPoint;
            var height = moveVector.Y;              //Ascend/descent
            moveVector.Y = 0;                       
            var distance = moveVector.Length;
            var heightCost = 0f;

            //Climb height = severe movement cost penalty
            if (height > 0)
                heightCost = Interpolation.RemapUnclamped(height / distance, 0, 1, 1, 3);
            else
                //Drop height = some movement cost bonus, but not so much
                heightCost = Interpolation.RemapUnclamped(-height / distance, 0, 1, 1, 0.5f);

            return Vector3.Distance(from.Cell.Macro.CenterPoint, to.Cell.Macro.CenterPoint) * speedCost * rougnessCost * heightCost;
        }

        

        public void DebugVisualize(IEnumerable<NavigationCell> nodes, Color color)
        {
            foreach (var node in nodes) DebugExtension.DebugPoint(node.Cell.Macro.CenterPoint, color, 2f, 10, false);
        }

        public float Heuristic(NavigationCell from, NavigationCell to)
        {
            //todo add height diff coefficient
            return Vector3.Distance(from.Cell.Macro.CenterPoint, to.Cell.Macro.CenterPoint);
        }

        public IEnumerable<NavigationCell> Neighbors(Actor actor, NavigationCell node)
        {
            foreach (var macroCell in node.Cell.Macro.NeighborsSafe)
                yield return _map.Cells[macroCell.Coords];
        }
    }


    public class PriorityQueue<T>
    {
        // I'm using an unsorted array for this example, but ideally this
        // would be a binary heap. There's an open issue for adding a binary
        // heap to the standard C# library: https://github.com/dotnet/corefx/issues/574
        //
        // Until then, find a binary heap class:
        // * https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
        // * http://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx
        // * http://xfleury.github.io/graphsearch.html
        // * http://stackoverflow.com/questions/102398/priority-queue-in-net

        private readonly List<Tuple<T, float>> elements = new List<Tuple<T, float>>();

        public int Count
        {
            get { return elements.Count; }
        }

        public void Enqueue(T item, float priority)
        {
            elements.Add(Tuple.Create(item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestIndex].Item2)
                {
                    bestIndex = i;
                }
            }

            T bestItem = elements[bestIndex].Item1;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    public class AStarSearch<TGraph, TNode> where TGraph : IWeightedGraph<TNode> where TNode : IEquatable<TNode>
    {
        private TGraph _graph;

        private readonly Dictionary<TNode, TNode> _cameFrom
            = new Dictionary<TNode, TNode>();

        private readonly Dictionary<TNode, float> _costSoFar
            = new Dictionary<TNode, float>();


        public AStarSearch(TGraph graph)
        {
            _graph = graph;
        }

        public List<TNode> CreatePath(Actor actor, TNode from, TNode to, Predicate<TNode> debugBreakOn = null)
        {
            var timer = Stopwatch.StartNew();
            int processedLocations = 0, maxFrontierCount = 0;

            _cameFrom.Clear();
            _costSoFar.Clear();

            var frontier = new PriorityQueue<TNode>();
            var start = from;
            var goal = to;
            frontier.Enqueue(start, 0);

            _cameFrom[start] = start;
            _costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                //Do not search for best path
                if (current.Equals(to))
                {
                    break;
                }

                if(debugBreakOn != null && debugBreakOn(current))
                    Debug.Log($"break on {current}");

                Debug.Log($"Check {current}...");

                foreach (var next in _graph.Neighbors(actor, current))
                {
                    var newCost = _costSoFar[current] + _graph.Cost(current, next);
                    Debug.Log($"..to {next} cost is {newCost}..");

                    if (!_costSoFar.TryGetValue(next, out var storedNextCost) || newCost < storedNextCost)
                    {
                        _costSoFar[next] = newCost;
                        var h = _graph.Heuristic(next, goal);
                        var priority = newCost + h;
                        frontier.Enqueue(next, priority);
                        _cameFrom[next] = current;
                        Debug.Log($"..Its a good cost. P = g + h, {priority}={newCost}+{h}");
                    }
                }

                processedLocations++;
                if (frontier.Count > maxFrontierCount)
                    maxFrontierCount = frontier.Count;
            }

            timer.Stop();

            //Visualize scanned blocks
            _graph.DebugVisualize(_costSoFar.Keys, Color.red);

            //Reconstruct path
            var result = new List<TNode>();
            TNode prev;
            if (_cameFrom.ContainsKey(goal))
            {
                prev = goal;
                result.Add(prev);
            }
            else
            {
                Debug.Log($"Path from {from} to {to} cant be found, processed {processedLocations}, max frontier size {maxFrontierCount}, time {timer.ElapsedMilliseconds} msec");
                return null;
            }

            do
            {
                prev = _cameFrom[prev];
                result.Add(prev);
            } while (!prev.Equals(start));
            result.Reverse();

            Debug.Log($"AStar path from {from} to {to} is found, steps {result.Count}, processed {processedLocations}, max frontier size {maxFrontierCount}, time {timer.ElapsedMilliseconds} msec");

            return result;
        }

    }
}

