using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using UnityEngine.Assertions;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Store hierarchical path data
    /// </summary>
    public class Path
    {
        public Waypoint Start { get; }
        public Waypoint Finish { get; }
        public Actor Actor { get; }

        public Segment CurrentSegment { get; private set; }
        public Waypoint CurrentPoint { get; private set; }

        public bool IsValid { get; }

        public IEnumerable<Waypoint> Waypoints
        {
            get
            {
                if(!IsValid)
                    yield break;

                yield return Start;

                foreach (var segment in _segments)
                    foreach (var waypoint in segment.AllWaypoints)
                        yield return waypoint;

                yield return Finish;
            }
        }

        public IEnumerable<Segment> Segments
        {
            get
            {
                foreach (var segment in _segments)
                    yield return segment;

                yield return _finishSegment;
            }
        }

        #region Debug

        public readonly HashSet<Waypoint> TotalProcessed = new HashSet<Waypoint>();

        #endregion

        /// <summary>
        /// Simple linear path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        public Path(Waypoint from, Waypoint to, Actor actor)
        {
            Start = from;
            Finish = to;
            Actor = actor;
            IsValid = true;

            var startSegment = new Segment(from);
            _segments.Add(startSegment);

            _finishSegment = new Segment(to);
            startSegment.SetNextSegment(_finishSegment);
            
            CurrentSegment = startSegment;
            CurrentPoint = CurrentSegment.AllWaypoints.First();
        }

        /// <summary>
        /// Complex segmented path
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="actor"></param>
        /// <param name="segments"></param>
        public Path(Waypoint from, Waypoint to, Actor actor, IEnumerable<Waypoint> segments)
        {
            Start = from;
            Finish = to;
            Actor = actor;
            IsValid = true;

            _segments.Add(new Segment(from));
            _segments.AddRange(segments.Where(p => p != from && p != to).Select(startPoint => new Segment(startPoint)));
            _finishSegment = new Segment(to);

            _segments.Last().SetNextSegment(_finishSegment);
            for (var i = _segments.Count - 2; i >= 0; i--)
                _segments[i].SetNextSegment(_segments[i + 1]);

            CurrentSegment = _segments.First();
            CurrentPoint = CurrentSegment.From;
        }

        private Path(Waypoint from, Waypoint to, Actor actor, bool isInvalid)
        {
            Start = from;
            Finish = to;
            Actor = actor;
            IsValid = false;
        }


        public float GetPathLength()
        {
            if (!IsValid)
                return 0;

            var result = 0f;
            foreach (var segment in Segments)
                result += segment.GetLength();

            return result;
        }

        public static Path CreateInvalidPath(Waypoint from, Waypoint to, Actor actor)
        {
            return new Path(from, to, actor, true);
        }

        public (Segment segment, Waypoint point) Next()
        {
            if(!IsValid)
                return (CurrentSegment, CurrentPoint);

            if (CurrentSegment == _finishSegment)
                return (CurrentSegment, CurrentPoint);

            _pointIndex++;

            if (_pointIndex > CurrentSegment.InterWaypoints.Count)
            {
                _segmentIndex++;
                _pointIndex = 0;

                if (_segmentIndex < _segments.Count)
                    CurrentSegment = _segments[_segmentIndex];
                else
                {
                    CurrentSegment = _finishSegment;
                    CurrentPoint = CurrentSegment.From;
                    return (CurrentSegment, CurrentPoint);
                }
            }

            if (_pointIndex < CurrentSegment.InterWaypoints.Count)
            {
                CurrentPoint = CurrentSegment.InterWaypoints[_pointIndex];
                return (CurrentSegment, CurrentPoint);
            }
            else if (_pointIndex == CurrentSegment.InterWaypoints.Count)
            {
                CurrentPoint = CurrentSegment.To;
                return (CurrentSegment, CurrentPoint);
            }

            //Should not execute
            return (CurrentSegment, CurrentPoint);
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
        private readonly List<Segment> _segments = new List<Segment>();
        private int _segmentIndex;
        private int _pointIndex;
    }

    public class Segment
    {
        public Waypoint From { get; }
        public Waypoint To => _next.From;

        public Segment Next => _next;

        public IReadOnlyList<Waypoint> InterWaypoints => _interPoints;

        public IEnumerable<Waypoint> AllWaypoints
        {
            get
            {
                yield return From;
                foreach (var point in _interPoints)
                    yield return point;
            }
        }

        public bool IsRefined { get; private set; }

        public Segment(Waypoint from)
        {
            From = from;
            _next = this;
        }

        public void SetNextSegment([NotNull] Segment nextSegment)
        {
            _next = nextSegment ?? throw new ArgumentNullException(nameof(nextSegment));
        }

        /// <summary>
        /// Add intermediate point between <see cref="From"/> and <see cref="To"/> points
        /// </summary>
        /// <param name="interPoints"></param>
        public void Refine(IEnumerable<Waypoint> interPoints)
        {
            _interPoints.AddRange(interPoints.Where(p => p != From && p != To));
            IsRefined = true;
        }

        public float GetLength()
        {
            var result = 0f;
            var prevPoint = From;

            foreach (var point in _interPoints)
            {
                result += Vector2i.Distance(prevPoint.Position, point.Position);
                prevPoint = point;
            }

            result += Vector2i.Distance(prevPoint.Position, To.Position);

            return result;
        }

        public override string ToString()
        {
            if (Next == this)
                return $"Final segment {From}";
            else
                return $"From {From} to {To}, refined {IsRefined}, length {GetLength()}";
        }

        private readonly List<Waypoint> _interPoints = new List<Waypoint>();
        private Segment _next;
    }
}
