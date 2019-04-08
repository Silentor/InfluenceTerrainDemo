using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

//Based on https://www.redblobgames.com/pathfinding/a-star/implementation.html#csharp

namespace TerrainDemo.Hero
{

// A* needs only a WeightedGraph and a location type L, and does *not*
// have to be a grid. However, in the example code I am using a grid.
    public interface WeightedGraph<L>
    {
        float Cost(Location a, Location b);
        IEnumerable<Location> Neighbors(BaseBlockMap fromMap, Vector2i fromPos);
    }


    public class Location : IEquatable<Location>
    {
        // Implementation notes: I am using the default Equals but it can
        // be slow. You'll probably want to override both Equals and
        // GetHashCode in a real project.

        public readonly Vector2i Position;
        public readonly BaseBlockMap Map;

        public Location(Vector2i position, BaseBlockMap map)
        {
            Position = position;
            Map = map;
        }

        public bool Equals(Location other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Position.Equals(other.Position) && Map.Equals(other.Map);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Location) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Map.GetHashCode();
            }
        }

        public static bool operator ==(Location left, Location right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Location left, Location right)
        {
            return !Equals(left, right);
        }
    }


    public class SquareGrid : WeightedGraph<Location>
    {
        // Implementation notes: I made the fields public for convenience,
        // but in a real project you'll probably want to follow standard
        // style and make them private.

        private readonly MicroMap _map;
        private static readonly float SafeAngle = MathHelper.DegreesToRadians(60);

        public SquareGrid(MicroMap map)
        {
            _map = map;
        }

        private bool Passable(BaseBlockMap fromMap, Vector2i fromPos, Vector2i toPos, out BaseBlockMap toMap)
        {
            var (newMap, newOverlapState) = _map.GetOverlapState(toPos);

            //Check special cases with map change
            ref readonly var fromData = ref fromMap.GetBlockData(fromPos);
            ref readonly var toData = ref BlockData.Empty;
            toMap = null;
            switch (newOverlapState)
            {
                //Check from object map to main map transition
                case BlockOverlapState.Under:
                case BlockOverlapState.None:
                    {
                        toData = ref _map.GetBlockData(toPos);
                        toMap = _map;
                    }
                    break;

                case BlockOverlapState.Above:
                    {
                        ref readonly var aboveBlockData = ref newMap.GetBlockData(toPos);

                        //Can we pass under floating block?
                        if (aboveBlockData.MinHeight > fromData.MaxHeight + 2)
                        {
                            toData = ref fromMap.GetBlockData(toPos);
                            toMap = fromMap;
                        }
                        else
                        {
                            toData = ref aboveBlockData;
                            toMap = newMap;
                        }
                    }
                    break;

                case BlockOverlapState.Overlap:
                    {
                        toData = ref newMap.GetBlockData(toPos);
                        toMap = newMap;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (toData != BlockData.Empty)
            {
                if (toData.Height < fromData.Height + 1 &&
                    Vector3.CalculateAngle(Vector3.UnitY, toData.Normal) < SafeAngle)
                {
                    //Can step on new block
                    Assert.IsNotNull(toMap);
                    return true;
                }
            }

            //No pass 
            return false;
        }

        public float Cost(Location a, Location b)
        {
            return 1;       //Calculate true cost
        }

        public IEnumerable<Location> Neighbors(BaseBlockMap fromMap, Vector2i fromPos)
        {
            foreach (var dir in Directions.Vector2I)
            {
                var neighborPos = fromPos + dir;
                if (_map.Bounds.Contains(neighborPos) && Passable(fromMap, fromPos, neighborPos, out var toMap))
                    yield return new Location(neighborPos, toMap);
            }
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

        private List<Tuple<T, float>> elements = new List<Tuple<T, float>>();

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


/* NOTE about types: in the main article, in the Python code I just
 * use numbers for costs, heuristics, and priorities. In the C++ code
 * I use a typedef for this, because you might want int or double or
 * another type. In this C# code I use double for costs, heuristics,
 * and priorities. You can use an int if you know your values are
 * always integers, and you can use a smaller size number if you know
 * the values are always small. */

    public class AStarSearch
    {
        private readonly Dictionary<Location, Location> _cameFrom
            = new Dictionary<Location, Location>();

        private readonly Dictionary<Location, float> _costSoFar
            = new Dictionary<Location, float>();
        
        private readonly WeightedGraph<Location> _graph;

        // Note: a generic version of A* would abstract over Location and
        // also Heuristic
        private static float Heuristic(Location a, Location b)
        {
            return Math.Abs(a.Position.X - b.Position.X) + Math.Abs(a.Position.Z - b.Position.Z);
        }

        public AStarSearch(WeightedGraph<Location> graph)
        {
            _graph = graph;
        }

        public IReadOnlyList<(BaseBlockMap, Vector2i)> CreatePath(BaseBlockMap fromMap, Vector2i fromPos,
            BaseBlockMap toMap, Vector2i toPos)
        {
            var timer = Stopwatch.StartNew();
            int processedLocations = 0, maxFrontierCount = 0;

            _cameFrom.Clear();
            _costSoFar.Clear();

            var frontier = new PriorityQueue<Location>();
            var start = new Location(fromPos, fromMap);
            var goal = new Location(toPos, toMap);
            frontier.Enqueue(start, 0);

            _cameFrom[start] = start;
            _costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current.Position == toPos)
                {
                    break;
                }

                foreach (var next in _graph.Neighbors(current.Map, current.Position))
                {
                    var newCost = _costSoFar[current]
                                  + _graph.Cost(current, next);
                    if (!_costSoFar.TryGetValue(next, out var storedNextCost) || newCost< storedNextCost)
                    {
                        _costSoFar[next] = newCost;
                        var priority = newCost + Heuristic(next, goal);
                        frontier.Enqueue(next, priority);
                        _cameFrom[next] = current;
                    }
                }

                processedLocations++;
                if (frontier.Count > maxFrontierCount)
                    maxFrontierCount = frontier.Count;
            }

            timer.Stop();

            //Reconstruct path
            var result = new List<(BaseBlockMap, Vector2i)>();
            Location prev;
            if (_cameFrom.ContainsKey(goal))
            {
                prev = goal;
                result.Add((prev.Map, prev.Position));
            }
            else
            {
                Debug.Log($"Path from {fromPos} to {toPos} cant be found, processed {processedLocations}, max frontier size {maxFrontierCount}, time {timer.ElapsedMilliseconds} msec");
                return null;
            }

            do
            {
                prev = _cameFrom[prev];
                result.Add((prev.Map, prev.Position));
            } while (prev != start);

            result.Reverse();

            Debug.Log($"Path from {fromPos} to {toPos} is found, steps {result.Count}, processed {processedLocations}, max frontier size {maxFrontierCount}, time {timer.ElapsedMilliseconds} msec");

            return result;
        }

    }
}

