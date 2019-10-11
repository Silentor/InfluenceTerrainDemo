using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Hero;

//Based on https://www.redblobgames.com/pathfinding/from-star/implementation.html#csharp

namespace TerrainDemo.Navigation
{
    public interface IWeightedGraph<TNode> where TNode : IEquatable<TNode>
    {
        IEnumerable<(TNode neighbor, float neighborCost)> Neighbors(BaseLocomotor loco, TNode from);

        float Heuristic(TNode @from, TNode to);
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
				return new SearchResult( new List<TNode>(), SearchState.Success, 0, new Dictionary<TNode, TNode>(), new Dictionary<TNode, float>());

            var timer = Stopwatch.StartNew();
            int processedNodes = 0, maxFrontierCount = 0;

            _cameFrom.Clear();
            _costSoFar.Clear();

            var frontier = new SimplePriorityQueue<TNode>();
            var start = from;
            var goal = to;
            frontier.Enqueue(start, 0);

            _cameFrom[start] = start;		//consider store tuple in one dicionary
            _costSoFar[start] = 0;

            while (frontier.Count > 0)
            {
                processedNodes++;
                var current = frontier.Dequeue();

                if(current.Equals(to))
                    break;

                var currentCost = _costSoFar[current];
				foreach (var (neighbor, neighborCost) in _graph.Neighbors(actor.Locomotor, current))
                {
                    if(isValidNode != null && !isValidNode(neighbor))
                        continue;
                    
                    var newCost = currentCost + neighborCost;

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

            //Reconstruct path
            SearchState searchState;
            var result = new List<TNode>();
            TNode prev;
            if (_cameFrom.ContainsKey(goal))
            {
	            searchState = SearchState.Success;
                prev = goal;
                result.Add(prev);
            }
            else
            {
				//Incomplete path, return path to nearest node to goal
				searchState = SearchState.Incomplete;
				TNode nearestNode = start;
				float nearestCost = float.MaxValue;
				foreach ( var cost in _costSoFar )
				{
					var neighborCost = /*cost.Value + */_graph.Heuristic( cost.Key, goal );
					if ( neighborCost < nearestCost )
					{
						nearestCost = neighborCost;
						nearestNode = cost.Key;
					}
				}

				prev = nearestNode;
				result.Add( prev );
            }

            do
            {
                prev = _cameFrom[prev];
                result.Add(prev);
            } while (!prev.Equals(start));
            result.Reverse();

            timer.Stop();

            return new SearchResult(result, searchState, timer.ElapsedMilliseconds, _cameFrom, _costSoFar);
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

            var frontier = new SimplePriorityQueue<TNode>();
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

                foreach (var (neighbor, neighborCost) in _graph.Neighbors(actor.Locomotor, current))
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

                    yield return new SearchResult(null, SearchState.Searching, 0, _cameFrom, _costSoFar);
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

            yield return new SearchResult(result, SearchState.Success, 0, _cameFrom, _costSoFar);
        }

        public class SearchResult
        {
	        public readonly List<TNode> Route;
	        public readonly SearchState Result;
	        public readonly uint ElapsedTimeMs;	
	        public readonly IReadOnlyDictionary<TNode, TNode> CameFromDebug;
	        public readonly IReadOnlyDictionary<TNode, float> CostsDebug;

	        public SearchResult( List<TNode> route, SearchState result, long elapsedTimeMs, IReadOnlyDictionary<TNode, TNode> cameFromDebug, IReadOnlyDictionary<TNode, float> costsDebug )
	        {
		        Route = route;
		        Result = result;
		        ElapsedTimeMs = (uint)elapsedTimeMs;
		        CameFromDebug = cameFromDebug;
		        CostsDebug = costsDebug;
	        }
        }

    }

    public enum SearchState
    {
		Searching,
		Success,
		Incomplete
    }
}

