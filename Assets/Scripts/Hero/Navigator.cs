using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;

namespace TerrainDemo.Hero
{
    /// <summary>
    /// Pathfinding component of Actor
    /// </summary>
    public class Navigator
    {
        private readonly MicroMap _map;
        public Actor Owner { get; }

        public Navigator(Actor owner, MicroMap map)
        {
            _map = map;
            Owner = owner;
        }

        /// <summary>
        /// Find the path to destination and start moving
        /// </summary>
        /// <param name="destination"></param>
        public void Go(Vector2i destination)
        {
            var blockDestination = (Vector2i) destination;

            if (Owner.BlockPosition == blockDestination)
                return;

            var search = new AStarSearch(new SquareGrid(_map), Owner.Map, Owner.BlockPosition, _map, destination);
            var path = search.GetPath();

            UnityEngine.Debug.Log($"Path from {Owner.BlockPosition} to {destination}, path length {path.Count()}");

            foreach (var step in path)
            {
                var blockHeight = step.Item1.GetBlockData(step.Item2).Height;
                var blockCenter = BlockInfo.GetWorldCenter(step.Item2);
                DebugExtension.DebugWireSphere(new UnityEngine.Vector3(blockCenter.X, blockHeight, blockCenter.Y), 
                    Color.white, 0.3f, 5, true);
            }
        }
    }

    
}
