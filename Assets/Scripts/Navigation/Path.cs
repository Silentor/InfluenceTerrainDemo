using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine.Assertions;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Store hierarchical path data of macro and micro level
    /// </summary>
    public class Path
    {
	    public GridPos Start  { get; }
	    public GridPos Finish { get; }
	    public Actor    Actor  { get; }

	    public bool IsValid { get; }

	    public IEnumerable<Segment> Segments
	    {
		    get
		    {
			    for ( var i = 0; i < _segmentsCount; i++ )
			    {
				    yield return GetSegment( i );
			    }
		    }
	    }

	    public NavigationCell FinishNavNode => _finishSegment.Node;

        public NavigationCell StartNavNode => _startSegment.Node;

        #region Debug

		public IEnumerable<(NavigationCell, float)> ProcessedCosts => _sharedPath.CostsDebug.Select( c => (c.Key, c.Value) );

        #endregion

        /// <summary>
        /// Complex segmented path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <param name="segments"></param>
        internal Path(GridPos from, GridPos to, Actor actor, NavigationCell fromNode, NavigationCell toNode, PathCacheEntry sharedPath, [NotNull] NavigationMap map)
        {
            Start   = from;
            Finish  = to;
            Actor   = actor;
            
            _map = map;

            _sharedPath = sharedPath;
            if ( fromNode == toNode )
            {
	            _startSegment = new Segment( fromNode, from, to );
	            _finishSegment = _startSegment;
	            _segmentsCount = 1;
            }
            else
            {
	            _startSegment = new Segment( fromNode, from );
				_finishSegment = new Segment( toNode, to );
				_segmentsCount = sharedPath.Segments.Count + 2;
            }

			Assert.IsTrue( _segmentsCount > 0 );
			IsValid = true;
        }

		public float GetPathLength()
        {
            if (!IsValid)
                return 0;

            var result = 0f;
            for ( var i = 0; i < _segmentsCount; i++ )
            {
	            var segment = GetSegment(i);
	            result += segment.GetLength( );
            }

            return result;
        }

		public Iterator Go() => new Iterator( this );


		private readonly int _segmentsCount;
		private readonly NavigationMap _map;
		private readonly (NavigationCell, float)[] _processCosts;
		private readonly Segment _startSegment;
		private readonly Segment _finishSegment;
		private readonly PathCacheEntry _sharedPath;

		private Segment GetSegment( int i )
		{
			if ( i == 0 )
				return _startSegment;
			else if ( i == _segmentsCount - 1 )
				return _finishSegment;
			else if ( i > 0 && i < _segmentsCount - 1 )
				return _sharedPath.Segments[i - 1];
			else
			{
				throw new ArgumentOutOfRangeException( );
			}
		}

        //Calculate micro path for given nav node
        private void RefineSegment( int nodeIndex )
        {
	        GridPos prevPoint, myPoint, nextPoint;

	        var prevIndex = Math.Max( nodeIndex - 1, 0 );
	        var nextIndex = Math.Min( nodeIndex + 1, _segmentsCount - 1 );

	        prevPoint = prevIndex == 0 ? Start : GetSegment(prevIndex).Node.Cell.Center;
	        nextPoint = nextIndex == _segmentsCount - 1 ? Finish : GetSegment(nextIndex).Node.Cell.Center;
	        myPoint = nodeIndex == 0 
		        ? Start 
		        : nodeIndex  == _segmentsCount - 1 
			        ? Finish 
			        : GetSegment(nodeIndex).Node.Cell.Center;

	        var from = GridPos.Average( prevPoint, myPoint );
	        var to = GridPos.Average(myPoint, nextPoint);

	        var microRoute = _map.Pathfinder.GetMicroRoute( from, to, Actor );

	        GetSegment( nodeIndex).Refine(  microRoute.Route );
        }

        public class Segment
        {
	        public readonly NavigationCell Node;
	        public IReadOnlyList<GridPos> Points => _points;
	        public          bool           IsRefined { get; private set; }

	        internal Segment( NavigationCell node )
	        {
		        Node = node;
	        }

	        internal Segment(NavigationCell node, params GridPos[] initPoints) : this(node)
	        {
		        _points.AddRange(initPoints);
	        }


			public void Refine( IEnumerable<GridPos> points )
	        {
		        if ( !IsRefined )
		        {
					_points.Clear(  );
			        _points.AddRange( points );
			        IsRefined = true;
		        }
	        }

	        public float GetLength()
	        {
		        var result = 0f;

		        for ( var i = 0; i < Points.Count - 1; i++ )
		        {
			        result += GridPos.Distance( Points[i], Points[i +1] );
		        }

		        return result;
	        }

	        public override string ToString()
	        {
		        return $"Node {Node.Cell.Id}, points {Points.ToJoinedString( )}";
	        }

			private readonly List<GridPos> _points = new List<GridPos>();
        }

		/// <summary>
		/// Used to sequentially retrieve waypoints from path
		/// </summary>
        public class Iterator	 
        {
	        internal  Iterator( [NotNull] Path path)
	        {
		        _path = path ?? throw new ArgumentNullException( nameof( path ) );
		        _lastPosition = path.Finish;
	        }

	        public (NavigationCell node, GridPos position) Current
	        {
		        get
		        {
			        if ( _pointIndex < 0 )
				        throw new InvalidOperationException( "Iterator is not initialized" );

			        if ( _isFinished )
			        {
				        var lastSegment = _path._finishSegment;
				        return ( lastSegment.Node, lastSegment.Points[lastSegment.Points.Count - 1] );
			        }

			        var segment = _path.GetSegment(_segmentIndex);
			        if ( !segment.IsRefined )
			        {
				        _path.RefineSegment( _segmentIndex );
			        }

			        return ( segment.Node, segment.Points[_pointIndex] );
		        }
	        }

	        public bool Next( )
	        {
		        if ( _isFinished ) 
			        return false;

		        while ( true )
		        {
			        if ( _segmentIndex >= _path._segmentsCount )
			        {
				        _isFinished = true;
				        return false;
			        }

			        var segment = _path.GetSegment(_segmentIndex);
			        if ( !segment.IsRefined )
			        {
				        _path.RefineSegment( _segmentIndex );
			        }

			        _pointIndex++;

			        if ( _pointIndex > segment.Points.Count - 1 )
			        {
				        _segmentIndex++;
				        _pointIndex = 0;
				        continue;
			        }

					//Skip duplicate positions
			        var currentPoint = segment.Points[_pointIndex];
					if(currentPoint == _lastPosition)
						continue;

					_lastPosition = currentPoint;

			        return true;
		        }
	        }
	        private readonly Path _path;
	        private GridPos _lastPosition;
	        private          int  _segmentIndex, _pointIndex = -1;
	        private          bool _isFinished;
        }
    }

    
}
