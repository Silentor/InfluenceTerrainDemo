using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
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

	    public NavNode FinishNavNode => _finishSegment.Node;

        public NavNode StartNavNode => _startSegment.Node;

        #region Debug

		public IEnumerable<(NavNode, float)> ProcessedCosts => _sharedPath.CostsDebug.Select( c => (c.Key, c.Value) );

        #endregion

        /// <summary>
        /// Complex segmented path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <param name="segments"></param>
        internal Path(GridPos from, GridPos to, Actor actor, NavNode fromNode, NavNode toNode, PathCacheEntry sharedPath, [NotNull] NavigationMap map)
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
            else if ( map.NavGraph.Nei( fromNode ).Select( n => n.neighbor ).Contains( toNode ) ) 
            {
				_startSegment = new Segment( fromNode, from );
				_finishSegment = new Segment( toNode, to );
				_segmentsCount = 2;
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

		//public Iterator Go() => new Iterator( this );

		public MacroIterator GetMacroIterator() => new MacroIterator( this );

		public NavNode GetNextNode( NavNode node )
		{
			if ( node == FinishNavNode )
				return node;

			using ( var i = Segments.GetEnumerator( ) )
			{
				while ( i.MoveNext( ) )
				{
					if ( i.Current.Node == node )
					{
						i.MoveNext( );
						return i.Current.Node;
					}
				}
			}

			throw new ArgumentException(  );
		}

		public override string ToString( )
		{
			return
				$"Path {Start}->{Finish}({StartNavNode} -> {FinishNavNode}), is valid {IsValid}, segments {_segmentsCount}, length ~{GetPathLength( )}";
		}


		private readonly int _segmentsCount;
		private readonly NavigationMap _map;
		private readonly (NavNode, float)[] _processCosts;
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

        public class Segment
        {
	        public readonly NavNode Node;
	        public IReadOnlyList<GridPos> Points => _points;
	        public          bool           IsRefined { get; private set; }

	        internal Segment(NavNode node )
	        {
		        Node = node;
	        }

	        internal Segment(NavNode node, params GridPos[] initPoints) : this(node)
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
		        return $"Node {Node}, points {Points.ToJoinedString( )}";
	        }

			private readonly List<GridPos> _points = new List<GridPos>();
        }

        public class MacroIterator	 
        {
	        internal  MacroIterator( [NotNull] Path path)
	        {
		        _path = path ?? throw new ArgumentNullException( nameof( path ) );
	        }

	        public Segment Current
	        {
		        get
		        {
			        if ( _segmentIndex < 0 )
				        throw new InvalidOperationException( "Iterator is not initialized" );

			        if ( _isFinished )
			        {
				        return _path.GetSegment( _path._segmentsCount - 1 );
			        }

			        var segment = _path.GetSegment(_segmentIndex);
			        return segment;
		        }
	        }

	        public Segment GetNext
	        {
		        get
		        {
			        if ( _segmentIndex < 0 )
				        throw new InvalidOperationException( "Iterator is not initialized" );

			        return _path.GetSegment( Math.Min( _segmentIndex + 1, _path._segmentsCount - 1 ) );
		        }
	        }

	        public Segment GetNext2
	        {
		        get
		        {
			        if ( _segmentIndex < 0 )
				        throw new InvalidOperationException( "Iterator is not initialized" );

			        return _path.GetSegment( Math.Min( _segmentIndex + 2, _path._segmentsCount - 1 ) );
		        }
	        }


	        public bool Next( )
	        {
		        if ( _isFinished ) 
			        return false;

		        if ( _segmentIndex >= _path._segmentsCount - 1 )
		        {
			        _isFinished = true;
			        return false;
		        }

		        _segmentIndex++;

		        return true;
	        }
	        private readonly Path _path;
	        private          int  _segmentIndex = -1;
	        private          bool _isFinished;
        }
    }

    
}
