using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Hero
{
    /// <summary>
    /// Pathfinding component of Actor
    /// </summary>
    public class Navigator
    {
        private readonly MicroMap _map;
        private readonly AStarSearch _pathfinder;
        private IReadOnlyList<(BaseBlockMap, Vector2i)> _path;
        public Actor Owner { get; }

        public Navigator(Actor owner, MicroMap map)
        {
            _map = map;
            Owner = owner;
            _pathfinder = new AStarSearch(new SquareGrid(_map));
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
            
            _path = _pathfinder.CreatePath(Owner.Map, Owner.BlockPosition, _map, destination);

            if (_path != null)
            {
                foreach (var step in _path)
                {
                    var blockHeight = step.Item1.GetBlockData(step.Item2).Height;
                    var blockCenter = BlockInfo.GetWorldCenter(step.Item2);
                    DebugExtension.DebugWireSphere(new UnityEngine.Vector3(blockCenter.X, blockHeight, blockCenter.Y),
                        Color.white, 0.3f, 5, true);
                }

                Task.Run(() => NavigatePath(_path))
                    .ContinueWith(t => UnityEngine.Debug.Log($"Navigate task completed"));
            }
        }

        private async Task NavigatePath(IReadOnlyList<(BaseBlockMap, Vector2i)> path)
        {
            if(path.Count == 0)
                Owner.Stop();

            for (int i = 0; i < path.Count; i++)
            {
                var waypoint = path[i];
                var waypointPosition = BlockInfo.GetWorldCenter(waypoint.Item2);

                while (Vector2.Distance((Vector2) Owner.Position, waypointPosition) > 1)
                {
                    //Owner.Rotate(waypointPosition);
                    Owner.MoveTo(waypointPosition);
                    await Task.Delay(300);
                }
            }

            Owner.Stop();

            UnityEngine.Debug.Log($"Path completed");
        }
    }

    
}
