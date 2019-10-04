using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Hero
{
    /// <summary>
    /// Pathfinding component of Actor, maybe will have Locomotor functions also
    /// </summary>
    public class Navigator
    {
        public Actor Owner { get; }
        public bool IsNavigated { get; private set; }

        public Path Path { get; private set; }

		public (NavigationCell node, GridPos position) DebugCurrentWaypoint { get; private set; }

        public Navigator(Actor owner, MicroMap map, NavigationMap navMap)
        {
            _map = map;
            _navMap = navMap;
            Owner = owner;
            //_pathfinder = Pathfinder.Instance;
        }

        /// <summary>
        /// Find the path to destination and start moving
        /// </summary>
        /// <param name="destination"></param>
        public void Go(GridPos destination)
        {
            var blockDestination = (GridPos) destination;

            if (Owner.Locomotor.BlockPosition == blockDestination)
                return;

            var path = _navMap.CreatePath( Owner.Locomotor.BlockPosition, destination, Owner);

            if (IsNavigated)
                Cancel(false);

            Path = path;
            if (path.IsValid)
            {
                IsNavigated = true;
                _navigateCancel = new CancellationTokenSource();
                _navigateTask = RunNavigatePath(path, _navigateCancel);
            }
        }

        /// <summary>
        /// Stop navigation on current path
        /// </summary>
        public void Cancel(bool stopOwner = true)
        {
            if (IsNavigated)
            {
                _navigateCancel?.Cancel();
                IsNavigated = false;

                if(stopOwner)
                    Owner.Locomotor.Stop();
            }
        }

        private readonly MicroMap _map;
        private readonly NavigationMap _navMap;
        //private readonly Pathfinder _pathfinder;
        private CancellationTokenSource _navigateCancel;
        private Task _navigateTask;

        private async Task RunNavigatePath([NotNull] Path path, [NotNull] CancellationTokenSource ct)
        {
            var task = NavigatePath(path, ct.Token);

            using (ct)
            {
                await task;
            }
        }

        private async Task NavigatePath([NotNull] Path path, CancellationToken ct)
        {
            Assert.IsTrue(IsNavigated);

            if (!path.IsValid)
            {
                Owner.Stop();
                IsNavigated = false;
                UnityEngine.Debug.Log($"{Owner} path invalid, do not move");
                return;
            }

            var pathIterator = path.Go( );

            try
            {
                while( pathIterator.Next(  ) )
                {
                    var waypoint = pathIterator.Current;
                    DebugCurrentWaypoint = waypoint;
                    var waypointPosition = BlockInfo.GetWorldCenter(waypoint.position);
					Owner.Locomotor.MoveTo( waypointPosition );
                    while (Vector2.Distance((Vector2) Owner.Position, waypointPosition) > 0.1f)
                    {
                        //Owner.Rotate(waypointPosition);
                        Owner.Locomotor.MoveTo(waypointPosition /*, waypoint.position == path.Finish*/ );
                        await Task.Delay(300, ct);
                    }

                    await Task.Delay(300, ct);
                };

				//Finish path
				Owner.Stop();
                IsNavigated = false;

                UnityEngine.Debug.Log($"{Owner} path completed, stop");
            }
            catch (TaskCanceledException)
            {
                UnityEngine.Debug.Log($"{Owner} navigation is cancelled");
            }
        }
    }
    
}
