using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Store hierarchical path data of macro and micro level
    /// </summary>
    public class Path
    {
	    public Vector2i Start  { get; }
	    public Vector2i Finish { get; }
	    public Actor    Actor  { get; }

	    public bool IsValid { get; }

	    public IEnumerable<Segment> Segments => _segments;		//Mostly for debug, use Path Iterator to retrieve waypoints along path

        public NavigationCell FinishNavNode => _segments[_segments.Length - 1].Node;

        public NavigationCell StartNavNode => _segments[0].Node;

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
        public Path(Vector2i from, Vector2i to, Actor actor, [NotNull] NavigationMap map)
        {
            Start   = from;
            Finish  = to;
            Actor   = actor;
            
            _map = map;

            var startNode = map.GetNavNode( @from );
            var finishNode = map.GetNavNode( to );

            var searchResult =  map.Pathfinder.GetMacroPath( startNode, finishNode, actor );
            var macroPath = searchResult.Path;
            _processCosts = searchResult.CostsDebug.Select( c => (c.Key, c.Value)).ToArray(  );

            if ( macroPath.Count < 2 )
            {
	            IsValid = false;
				return;
            }

            IsValid = true;
            _segments   = macroPath.Select( mp => new Segment( mp ) ).ToArray( );
            _segments[0].Points.Add( Start );
            _segments[_segments.Length - 1].Points.Add( Finish );
        }

        public float GetPathLength()
        {
            if (!IsValid)
                return 0;

            var result = 0f;
            foreach (var segment in _segments)
                result += segment.GetLength();

            return result;
        }

		public Iterator Go() => new Iterator( this );

        private readonly Segment _finishSegment;
		private readonly Segment[] _segments;
        private readonly NavigationMap _map;
        private readonly (NavigationCell, float)[] _processCosts;

        //Calculate micro path for given nav node
        private void RefinePath( int nodeIndex )
        {
	        Vector2i prevPoint, myPoint, nextPoint;

	        var prevIndex = Math.Max( nodeIndex - 1, 0 );
	        var nextIndex = Math.Min( nodeIndex + 1, _segments.Length - 1 );

	        prevPoint = prevIndex == 0 ? Start : _segments[prevIndex].Node.Cell.Center;
	        nextPoint = nextIndex == _segments.Length - 1 ? Finish : _segments[nextIndex].Node.Cell.Center;
	        myPoint = nodeIndex == 0 
		        ? Start 
		        : nodeIndex  == _segments.Length - 1 
			        ? Finish 
			        : _segments[nodeIndex].Node.Cell.Center;

	        var from = ( prevPoint + myPoint ) / 2;
	        var to = ( myPoint + nextPoint ) / 2;

			_segments[nodeIndex].Points.AddRange( new[]{from, to});//todo call pathfinding here
			_segments[nodeIndex].IsRefined = true;
        }

        public class Segment
        {
	        public readonly NavigationCell Node;
	        public readonly List<Vector2i> Points = new List<Vector2i>();
	        public          bool           IsRefined;

	        public Segment( NavigationCell node )
	        {
		        Node = node;
	        }

	        public float GetLength()
	        {
		        var result = 0f;

		        for ( var i = 0; i < Points.Count - 1; i++ )
		        {
			        result += Vector2i.Distance( Points[i], Points[i +1] );
		        }

		        return result;
	        }

	        public override string ToString()
	        {
		        return $"Node {Node.Cell.Id}, points {Points.ToJoinedString( )}";
	        }
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

	        public (NavigationCell node, Vector2i position) Current
	        {
		        get
		        {
			        if ( _pointIndex < 0 )
				        throw new InvalidOperationException( "Iterator is not initialized" );

			        if ( _isFinished )
			        {
				        var lastSegment = _path._segments[_path._segments.Length - 1];
				        return ( lastSegment.Node, lastSegment.Points[lastSegment.Points.Count - 1] );
			        }

			        var segment = _path._segments[_segmentIndex];
			        if ( !segment.IsRefined )
			        {
				        _path.RefinePath( _segmentIndex );
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
			        if ( _segmentIndex >= _path._segments.Length )
			        {
				        _isFinished = true;
				        return false;
			        }

			        var segment = _path._segments[_segmentIndex];
			        if ( !segment.IsRefined )
			        {
				        _path.RefinePath( _segmentIndex );
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
	        private Vector2i _lastPosition;
	        private          int  _segmentIndex, _pointIndex = -1;
	        private          bool _isFinished;
        }
    }

    
}
