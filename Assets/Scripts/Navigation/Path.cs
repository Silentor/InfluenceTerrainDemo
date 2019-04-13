using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;

namespace TerrainDemo.Navigation
{
    /// <summary>
    /// Store path data
    /// </summary>
    public class Path
    {
        public Waypoint Start { get; }
        public Waypoint Finish { get; }
        public Actor Actor { get; }

        public Waypoint Current { get; private set; }

        public bool IsValid { get; }

        public IEnumerable<Waypoint> Waypoints
        {
            get
            {
                if(!IsValid)
                    yield break;

                yield return Start;

                foreach (var segment in _segments)
                    foreach (var waypoint in segment.Waypoints)
                        yield return waypoint;

                yield return Finish;
            }
        }

        public float GetPathLength()
        {
            if (!IsValid)
                return 0;

            var wp1 = Start;
            var result = 0f;
            foreach (var waypoint in Waypoints.Skip(1))
            {
                result += Vector2i.Distance(wp1.Position, waypoint.Position);
                wp1 = waypoint;
            }

            return result;
        }

        public Path(Waypoint from, Waypoint to, Actor actor, bool isValid)
        {
            Start = from;
            Finish = to;
            Actor = actor;
            IsValid = isValid;

            if(isValid)
                Current = from;
        }

        public Waypoint Next()
        {
            if(!IsValid)
                return Waypoint.Empty;

            if (Current == Finish)
                return Current;

            if (_segments.Count == 0)
            {
                Current = Finish;
                return Current;
            }
            else
            {
                var segment = _segments[_segmentIndex];
                if (_pointIndex + 1 < segment.Waypoints.Count)
                {
                    _pointIndex++;
                    Current = segment.Waypoints[_pointIndex];
                    return Current;
                }
                else
                {
                    if (_segmentIndex + 1 < _segments.Count)
                    {
                        _segmentIndex++;
                        _pointIndex = 0;
                        segment = _segments[_segmentIndex];
                        Current = segment.Waypoints[_pointIndex];
                        return Current;
                    }
                    else
                    {
                        Current = Finish;
                        return Current;
                    }
                }
                
            }
        }

        public void AddSegment(Waypoint from, Waypoint to, IEnumerable<Waypoint> waypoints)
        {
            _segments.Add(new Segment(from, to, waypoints.ToArray()));
        }

        private readonly List<Segment> _segments = new List<Segment>();
        private int _segmentIndex;
        private int _pointIndex;
    }

    public class Segment
    {
        public Waypoint From { get; }
        public Waypoint To { get; }
        public IReadOnlyList<Waypoint> Waypoints { get; }

        public Segment(Waypoint @from, Waypoint to, IReadOnlyList<Waypoint> waypoints)
        {
            From = @from;
            To = to;
            Waypoints = waypoints;
        }
    }
}
