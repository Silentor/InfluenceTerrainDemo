using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
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

        public Path Path { get; private set; }

        public Navigator(Actor owner, MicroMap map)
        {
            _map = map;
            Owner = owner;
            _pathfinder = Pathfinder.Instance;
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

            var path = _pathfinder.CreatePath( Owner.BlockPosition, destination, Owner);

            if (path.IsValid)
            {
                Path = path;
                if(_navigateTask != null && !_navigateTask.IsCompleted)
                    _navigateCancel?.Cancel();

                _navigateCancel = new CancellationTokenSource();
                RunNavigatePath(path, _navigateCancel.Token);
            }
            else
                Owner.Stop();
        }

        private readonly MicroMap _map;
        private readonly Pathfinder _pathfinder;
        private Task _navigateTask;
        private CancellationTokenSource _navigateCancel;

        private async void RunNavigatePath(Path path, CancellationToken ct)
        {
            var task = NavigatePath(path, ct);
            _navigateTask = task;
            await task;
            UnityEngine.Debug.Log($"Continuation: Navigate task {task.Status}");

        }

        private async Task NavigatePath(Path path, CancellationToken ct)
        {
            if (!path.IsValid)
            {
                Owner.Stop();
                UnityEngine.Debug.Log($"Path invalid, do not move");
                return;
            }

            IsNavigated = true;
            try
            {
                do
                {
                    var waypoint = path.Next();
                    var waypointPosition = BlockInfo.GetWorldCenter(waypoint.point.Position);
                    while (Vector2.Distance((Vector2) Owner.Position, waypointPosition) > 0.1f)
                    {
                        //Owner.Rotate(waypointPosition);
                        Owner.MoveTo(waypointPosition, waypoint.point == path.Finish);
                        await Task.Delay(300, ct);
                    }
                } while (path.CurrentPoint != path.Finish);

                UnityEngine.Debug.Log($"Path completed");
            }
            catch (TaskCanceledException)
            {
                UnityEngine.Debug.Log($"Path cancelled, silently finish task");
            }
            finally
            {
                Owner.Stop();
                IsNavigated = false;
            }
        }
    }
    
}
