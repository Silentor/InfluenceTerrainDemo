using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Store hierarchical path data
    /// </summary>
    public class Path
    {
	    public Vector2i Start  { get; }
	    public Vector2i Finish { get; }
	    public Actor    Actor  { get; }

	    public (NavigationCell node, Vector2i position)			Current { get; private set; }

	    public bool IsValid { get; }

	    public IEnumerable<Segment> Segments => _path;		//Mostly for debug, use Next() and Current

        public NavigationCell FinishNavNode => _path[_path.Length - 1].Node;

        public NavigationCell StartNavNode => _path[0].Node;

        #region Debug

		public IEnumerable<(NavigationCell, float)> ProcessedCosts => _processCosts;

        #endregion

        /// <summary>
        /// Complex segmented path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <param name="segments"></param>
        //public Path(Vector2i from, Vector2i to, Actor actor, IEnumerable<Vector2i> segments)
        //{
        //    Start = from;
        //    Finish = to;
        //    Actor = actor;
        //    IsValid = true;

        //    _segments.Add(new Segment(from));
        //    _segments.AddRange(segments.Where(p => p != from && p != to).Select(startPoint => new Segment(startPoint)));
        //    _finishSegment = new Segment(to);

        //    _segments.Last().SetNextSegment(_finishSegment);
        //    for (var i = _segments.Count - 2; i >= 0; i--)
        //        _segments[i].SetNextSegment(_segments[i + 1]);

        //    CurrentSegment = _segments.First();
        //    CurrentPoint = CurrentSegment.From;
        //}

        /// <summary>
        /// Complex segmented path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <param name="segments"></param>
        public Path(Vector2i from, Vector2i to, Actor actor, NavigationMap map)
        {
            Start   = from;
            Finish  = to;
            Actor   = actor;
            
            _map = map;

            var startNode = map.GetNavNode( @from );
            var finishNode = map.GetNavNode( to );

            Current = ( startNode, from );

            var searchResult =  map.Pathfinder.GetMacroPath( startNode, finishNode, actor );
            var macroPath = searchResult.Path;
            _processCosts = searchResult.CostsDebug.Select( c => (c.Key, c.Value)).ToArray(  );

            if ( macroPath.Count < 2 )
            {
	            IsValid = false;
				return;
            }

            IsValid = true;
            _path   = macroPath.Select( mp => new Segment( mp ) ).ToArray( );
            _path[0].Points.Add( Start );
            _path[_path.Length - 1].Points.Add( Finish );
        }

        public float GetPathLength()
        {
            if (!IsValid)
                return 0;

            var result = 0f;
            foreach (var segment in _path)
                result += segment.GetLength();

            return result;
        }

        public (NavigationCell navNode, Vector2i point) Next()
        {
	        if ( !IsValid )
	        {
		        Current = ( StartNavNode, Start );
		        return Current;
	        }

	        if ( _isFinished )
	        {
		        return Current;
	        }

	        return Current;

	        //var curSegment = _path[_segmentIndex];
	        //var nextPoint = _pointIndex + 1;
	        //if ( nextPoint >= curSegment.Points.Count )
	        //{
	        // nextPoint = 0;
	        // curSegment = Math.Min( curSegment + 1, _path.Length - 1 );
	        //}

	        //if (CurrentSegment == _finishSegment)
	        //    return (CurrentSegment, CurrentPoint);

	        //_pointIndex++;

	        //if (_pointIndex > CurrentSegment.InterWaypoints.Count)
	        //{
	        //    _segmentIndex++;
	        //    _pointIndex = 0;

	        //    if (_segmentIndex < _segments.Count)
	        //        CurrentSegment = _segments[_segmentIndex];
	        //    else
	        //    {
	        //        CurrentSegment = _finishSegment;
	        //        CurrentPoint = CurrentSegment.From;
	        //        return (CurrentSegment, CurrentPoint);
	        //    }
	        //}

	        //if (_pointIndex < CurrentSegment.InterWaypoints.Count)
	        //{
	        //    CurrentPoint = CurrentSegment.InterWaypoints[_pointIndex];
	        //    return (CurrentSegment, CurrentPoint);
	        //}
	        //else if (_pointIndex == CurrentSegment.InterWaypoints.Count)
	        //{
	        //    CurrentPoint = CurrentSegment.To;
	        //    return (CurrentSegment, CurrentPoint);
	        //}

	        ////Should not execute
	        //return (CurrentSegment, CurrentPoint);
        }

        /*
        public void AddSegment(Waypoint from, Waypoint to, IEnumerable<Waypoint> waypoints)
        {
            _segments.Add(new Segment(from, to, waypoints.ToArray()));
        }

        public void AddSegment(Waypoint from, Waypoint to)
        {
            _segments.Add(new Segment(from, to));
        }
        */

        private readonly Segment _finishSegment;
		private readonly Segment[] _path;
		private Vector2i[][] _waypoints;
        private int _segmentIndex;
        private int _pointIndex;
        private readonly NavigationMap _map;
        private bool _isFinished;
        private readonly (NavigationCell, float)[] _processCosts;

        //Calculate micro path for given nav node
        private void RefinePath( int nodeIndex )
        {
	        Vector2i prevPoint, myPoint, nextPoint;

	        var prevIndex = Math.Max( nodeIndex - 1, 0 );
	        var nextIndex = Math.Min( nodeIndex + 1, _path.Length - 1 );

	        prevPoint = prevIndex == 0 ? Start : _path[prevIndex].Node.Cell.Center;
	        nextPoint = nextIndex == _path.Length - 1 ? Finish : _path[nextIndex].Node.Cell.Center;
	        myPoint = _path[nextIndex].Node.Cell.Center;

	        var from = ( prevPoint + myPoint ) / 2;
	        var to = ( myPoint + nextPoint ) / 2;

			_waypoints[nodeIndex] = new[]{from, to};//todo call pathfinding here
        }
    }

    public class Segment
    {
	    public readonly NavigationCell Node;
	    public readonly List<Vector2i> Points = new List<Vector2i>();
	    public bool IsRefined;

	    public Segment( NavigationCell node )
	    {
		    Node = node;
	    }

        public float GetLength()
        {
            var result = 0f;

            for ( var i = 0; i < Points.Count - 1; i++ )
            {
	            result    += Vector2i.Distance( Points[i], Points[i+1] );
            }

            return result;
        }

        public override string ToString()
        {
	        return $"{Node} ({Points.ToJoinedString( )})";

        }
    }
}
