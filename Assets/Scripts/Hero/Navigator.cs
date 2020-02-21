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

		public (NavNode node, GridPos position) DebugCurrentWaypoint { get; private set; }

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

        public void Warp( GridPos position )
        {
			Cancel( true );
            Owner.Locomotor.Warp( position );
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

            //var pathIterator = path.Go( );
            var macroIterator = path.GetMacroIterator( );

            
            //_navMap.Pathfinder.GetMicroRoute( Owner.Locomotor. )

            try
            {
                while( macroIterator.Next(  ) )
                {
	                var currentSegment = macroIterator.Current;

	                if ( currentSegment.Node == path.FinishNavNode ) //мы в финишной ноде, поиск до финальной точки, ограничение в текущую ноду
	                {
		                var segmentPoints =
			                _navMap.Pathfinder.GetMicroRoute( Owner.Locomotor.BlockPosition, path.Finish, Owner.Locomotor );
	                }
                    else if()//Мы в ноде, предыдущей финишной, поиск как выше, но ограничение в 2 ноды

                        //передавать в астар ограничения в виде навнод (обычно это текущая и следующая)

                    //иначе поиск до точки между следующей и послеследующей нодами

	                if ( !currentSegment.IsRefined )
	                {
		                var from = Owner.Locomotor.BlockPosition;

		                var nextNode = path.GetNextNode( currentSegment.Node );

	                }


                    var waypoint = macroIterator.Current;
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
                DebugCurrentWaypoint = default;

                UnityEngine.Debug.Log($"{Owner} path completed, stop");
            }
            catch (TaskCanceledException)
            {
                UnityEngine.Debug.Log($"{Owner} navigation is cancelled");
            }
        }
    }
    
}
