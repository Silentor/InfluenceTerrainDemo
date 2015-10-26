using System.Diagnostics;

namespace Assets.Code.Tools
{
    /// <summary>
    /// Stopwatch timer for measuring average and max time of some repeating process
    /// </summary>
    public class AverageTimer
    {
        /// <summary>
        /// Average time of operation (msec)
        /// </summary>
        public float AverageTime { get { return (float)_elapsedTime / _samples; } }

        /// <summary>
        /// Average time of operation (ticks)
        /// </summary>
        public float AverageTimeTicks { get { return (float)_elapsedTimeTicks / _samples; } }

        /// <summary>
        /// Max time of operation (msec)
        /// </summary>
        public long MaxTime { get; private set; }

        /// <summary>
        /// Time of last operation (msec)
        /// </summary>
        public long LastTime { get; private set; }

        public AverageTimer()
        {
            _watch = new Stopwatch();
        }

        public void Start()
        {
            _watch.Start();
        }

        public AverageTimer Stop()
        {
            _watch.Stop();

            LastTime = _watch.ElapsedMilliseconds - _elapsedTime;
            if (MaxTime < LastTime) MaxTime = LastTime;

            _elapsedTime = _watch.ElapsedMilliseconds;
            _elapsedTimeTicks = _watch.ElapsedTicks;
            _samples++;

            return this;
        }

        private readonly Stopwatch _watch;
        private int _samples;
        private long _elapsedTime;
        private long _elapsedTimeTicks;
    }
}
