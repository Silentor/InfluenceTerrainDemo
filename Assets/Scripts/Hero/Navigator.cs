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
        public Actor Owner { get; }
        public bool IsNavigated { get; private set; }

        public IReadOnlyList<(BaseBlockMap, Vector2i)> Path => _path;
        public int WaypointIndex { get; private set; }

        private readonly MicroMap _map;
        private readonly AStarSearch _pathfinder;
        private IReadOnlyList<(BaseBlockMap, Vector2i)> _path;
        private Task _navigateTask;
        private CancellationTokenSource _navigateCancel;

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

            List<(BaseBlockMap, Vector2i)> path;
            if(_pathfinder.IsStraightPathExists(Owner, (Owner.Map, Owner.BlockPosition), (Owner.Map, blockDestination)))
                path = new List<(BaseBlockMap, Vector2i)>(){(Owner.Map, blockDestination)};
            else
                path = _pathfinder.CreatePath(Owner, Owner.Map, Owner.BlockPosition, _map, destination);

            if (path != null)
            {
                _path = path;
                if(_navigateTask != null && !_navigateTask.IsCompleted)
                    _navigateCancel?.Cancel();

                _navigateCancel = new CancellationTokenSource();
                _navigateTask = Task.Run(() => NavigatePath(path, _navigateCancel.Token), _navigateCancel.Token);
                _navigateTask.ContinueWith(navTask =>
                    UnityEngine.Debug.Log($"Navigate task {(navTask.IsCanceled ? "cancelled" : "completed")}"));
            }
        }

        /// <summary>
        /// Remove not needed waypoints
        /// </summary>
        /// <param name="path"></param>
        private void SmoothPath(List<(BaseBlockMap, Vector2i)> path)
        {

        }

        private async Task NavigatePath(IReadOnlyList<(BaseBlockMap, Vector2i)> path, CancellationToken ct)
        {
            if(path.Count == 0)
                Owner.Stop();

            IsNavigated = true;
            for (int i = 0; i < path.Count; i++)
            {
                WaypointIndex = i;
                var waypoint = path[i];
                var waypointPosition = BlockInfo.GetWorldCenter(waypoint.Item2);

                while (Vector2.Distance((Vector2) Owner.Position, waypointPosition) > 0.5f)
                {
                    //Owner.Rotate(waypointPosition);
                    Owner.MoveTo(waypointPosition, i == path.Count - 1);
                    await Task.Delay(300, ct);

                    if (ct.IsCancellationRequested)
                    {
                        Owner.Stop();
                        IsNavigated = false;
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }

            Owner.Stop();
            IsNavigated = false;

            UnityEngine.Debug.Log($"Path completed");
        }
    }

    
}
