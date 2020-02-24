using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Debug = UnityEngine.Debug;

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

        public void Update( float deltaTime )
        {
	        if ( _navigateTask != null && _navigateTask.IsFaulted )
		        throw _navigateTask.Exception.Flatten( );
        }

        private readonly MicroMap _map;
        private readonly NavigationMap _navMap;
        //private readonly Pathfinder _pathfinder;
        private CancellationTokenSource _navigateCancel;
        private Task _navigateTask;

        private async Task RunNavigatePath([NotNull] Path path, [NotNull] CancellationTokenSource ct)
        {
            var task = NavigatePath(path, ct.Token);

            try
            {
	            using ( ct )
	            {
		            await task;
	            }
            }
            catch ( TaskCanceledException )
            {
                //Just skip
            }
            catch ( Exception e )
            {
	            Debug.LogException( e );
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

            try
            {
                while( macroIterator.Next(  ) )
                {
	                var currentSegment = macroIterator.Current;
	                var currentNode = currentSegment.Node;

	                AStarSearch<GridPos>.SearchResult segmentPoints;

	                //мы в финишной ноде, поиск до финальной точки, ограничение в текущую ноду
	                if ( currentNode == path.FinishNavNode )
	                {
		                Func<GridPos, CheckNodeResult> checkNode = pos => path.FinishNavNode.Area.IsContains( pos )
			                                                           ? CheckNodeResult.Valid
			                                                           : CheckNodeResult.Invalid;
		                segmentPoints =
			                _navMap.Pathfinder.GetMicroRoute( Owner.Locomotor.BlockPosition, path.Finish, Owner.Locomotor, checkNode );
                        currentSegment.Refine( segmentPoints.Route );

		                Debug.Log(  $"Refined final segment, path {segmentPoints.Route.ToJoinedString()}");
	                }

	                //Мы в ноде, предыдущей финишной, поиск как выше, но ограничение в 2 ноды
                    else if ( macroIterator.GetNext.Node == path.FinishNavNode )
	                {
		                Func<GridPos, CheckNodeResult> checkPos =
			                p => currentNode.Area.IsContains( p ) || path.FinishNavNode.Area.IsContains( p )
				                ? CheckNodeResult.Valid
				                : CheckNodeResult.Invalid;

		                segmentPoints =  _navMap.Pathfinder.GetMicroRoute( Owner.Locomotor.BlockPosition, path.Finish, Owner.Locomotor, checkPos );
		                currentSegment.Refine( segmentPoints.Route );

		                Debug.Log(  $"Refined pre-final segment, path {segmentPoints.Route.ToJoinedString()}");
	                }

	                //иначе поиск до точки между следующей и послеследующей нодами
	                else
	                {
		                var next = macroIterator.GetNext.Node;
		                var next2 = macroIterator.GetNext2.Node;
		                var midpoint = GridPos.Average( next.Position, next2.Position );

		                Func<GridPos, CheckNodeResult> checkPos =
			                p => next.Area.IsContains( p )
				                ? CheckNodeResult.Finish
				                : currentNode.Area.IsContains( p )
					                ? CheckNodeResult.Valid
					                : CheckNodeResult.Invalid;

		                
		                segmentPoints = _navMap.Pathfinder.GetMicroRoute( Owner.Locomotor.BlockPosition, midpoint, Owner.Locomotor, checkPos );
		                currentSegment.Refine( segmentPoints.Route );

		                Debug.Log(  $"Refined segment {currentNode}, path {segmentPoints.Route.ToJoinedString()}");
	                }

	                Debug.Log( $"Navigating segment {currentSegment.Node}" );

	                foreach ( var point in segmentPoints.Route )
	                {
		                DebugCurrentWaypoint = (currentNode, point);

		                var position = BlockInfo.GetWorldCenter(point);

		                Debug.Log( $"Navigating to position {position}..." );

                        var moveTo = Owner.Locomotor.MoveToAsync( position );
                        await moveTo;
		                
		                Debug.Log( $"Navigated to position {position}, result {moveTo.Status}, tread {Thread.CurrentThread.ManagedThreadId}" );
	                }

	                Debug.Log( $"Finished segment {currentSegment.Node}" );
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
