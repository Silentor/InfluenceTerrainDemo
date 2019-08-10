using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerrainDemo.Hero;

//Based on https://www.redblobgames.com/pathfinding/from-star/implementation.html#csharp

namespace TerrainDemo.Navigation
{
    public interface IWeightedGraph<TNode> where TNode : IEquatable<TNode>
    {
        IEnumerable<(TNode neighbor, float neighborCost)> Neighbors(Actor actor, TNode from);

        float Heuristic(TNode @from, TNode to);
    }

    public class NaivePriorityQueue<T>
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

    public class AStarSearch<TGraph, TNode> 
	    where TGraph : IWeightedGraph<TNode> 
	    where TNode : IEquatable<TNode>	
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

        public SearchResult CreatePath(Actor actor, TNode from, TNode to, Predicate<TNode> isValidNode = null)
        {
            if (from.Equals(to))
				return new SearchResult( new List<TNode>(), 0, new Dictionary<TNode, TNode>(), new Dictionary<TNode, float>());

            var timer = Stopwatch.StartNew();
            int processedNodes = 0, maxFrontierCount = 0;

            _cameFrom.Clear();
            _costSoFar.Clear();

            var frontier = new NaivePriorityQueue<TNode>();
            var start = from;
            var goal = to;
            frontier.Enqueue(start, 0);

            _cameFrom[start] = start;
            _costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                processedNodes++;
                var current = frontier.Dequeue();

                if(current.Equals(to))
                    break;

                foreach (var (neighbor, neighborCost) in _graph.Neighbors(actor, current))
                {
                    if(isValidNode != null && !isValidNode(neighbor))
                        continue;

                    var newCost = _costSoFar[current] + neighborCost;

                    if (!_costSoFar.TryGetValue(neighbor, out var storedNextCost) || newCost < storedNextCost)
                    {
                        _costSoFar[neighbor] = newCost;
                        var h = _graph.Heuristic(neighbor, goal);
                        var priority = newCost + h;
                        frontier.Enqueue(neighbor, priority);
                        _cameFrom[neighbor] = current;
                    }
                }
                
                if (frontier.Count > maxFrontierCount)
                    maxFrontierCount = frontier.Count;
            }

            timer.Stop();

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
	            return new SearchResult( null, timer.ElapsedMilliseconds, _cameFrom, _costSoFar );
            }

            do
            {
                prev = _cameFrom[prev];
                result.Add(prev);
            } while (!prev.Equals(start));
            result.Reverse();

            return new SearchResult(result, timer.ElapsedMilliseconds, _cameFrom, _costSoFar);
        }

        /// <summary>
        /// Step-by-step path creation
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="cameFrom"></param>
        /// <param name="costSoFar"></param>
        /// <returns></returns>
        public IEnumerable<SearchResult> 
            CreatePathStepByStep(Actor actor, TNode from, TNode to, Predicate<TNode> isValidNode = null)
        {
            int processedNodes = 0, maxFrontierCount = 0;
            _cameFrom.Clear();
            _costSoFar.Clear();

            var frontier = new NaivePriorityQueue<TNode>();
            var start = from;
            var goal = to;
            frontier.Enqueue(start, 0);

            _cameFrom[start] = start;
            _costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                processedNodes++;
                var current = frontier.Dequeue();

                if ( current.Equals(to) )
                    break;

                foreach (var (neighbor, neighborCost) in _graph.Neighbors(actor, current))
                {
                    if (isValidNode != null && !isValidNode(neighbor))
                        continue;

                    var newCost = _costSoFar[current] + neighborCost;
                    if (!_costSoFar.TryGetValue(neighbor, out var storedNextCost) || newCost < storedNextCost)
                    {
                        _costSoFar[neighbor] = newCost;
                        var h = _graph.Heuristic(neighbor, goal);
                        var priority = newCost + h;
                        frontier.Enqueue(neighbor, priority);
                        _cameFrom[neighbor] = current;
                    }

                    yield return new SearchResult(null, 0, _cameFrom, _costSoFar);
                }

                if (frontier.Count > maxFrontierCount)
                    maxFrontierCount = frontier.Count;
            }

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
                yield break;
            }

            do
            {
                prev = _cameFrom[prev];
                result.Add(prev);
            } while (!prev.Equals(start));
            result.Reverse();

            yield return new SearchResult(result, 0, _cameFrom, _costSoFar);
        }

        public readonly struct SearchResult
        {
	        public readonly List<TNode> Path;
	        public readonly uint ElapsedTime;	
	        public readonly IReadOnlyDictionary<TNode, TNode> CameFromDebug;
	        public readonly IReadOnlyDictionary<TNode, float> CostsDebug;

	        public SearchResult( List<TNode> path, long elapsedTime, IReadOnlyDictionary<TNode, TNode> cameFromDebug, IReadOnlyDictionary<TNode, float> costsDebug )
	        {
		        Path = path;
		        ElapsedTime = (uint)elapsedTime;
		        CameFromDebug = cameFromDebug;
		        CostsDebug = costsDebug;
	        }
        }

    }
}

