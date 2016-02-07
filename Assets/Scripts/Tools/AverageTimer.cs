using System.Diagnostics;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Stopwatch timer for measuring average and max time of some repeating process
    /// </summary>
    public class AverageTimer
    {
        public int SamplesCount { get; private set; }

        /// <summary>
        /// Average time of operation (msec)
        /// </summary>
        public float AvgTimeMs { get { return _elapsedTime / SamplesCount / (Stopwatch.Frequency / 1000f); } }

        /// <summary>
        /// Average time of operation (ticks)
        /// </summary>
        public float AvgTimeTicks { get { return (float)_elapsedTime / SamplesCount; } }

        public long MinTime { get; set; }

        /// <summary>
        /// Max time of operation (msec)
        /// </summary>
        public long MaxTime { get; private set; }

        /// <summary>
        /// Time of last operation (ticks)
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

            LastTime = _watch.ElapsedTicks - _elapsedTime;
            if (MaxTime < LastTime) MaxTime = LastTime;
            if (!_isMinTimeInit)
            {
                MinTime = LastTime;
                _isMinTimeInit = true;
            }
            else 
                if (LastTime < MinTime) MinTime = LastTime;

            _elapsedTime = _watch.ElapsedTicks;
            SamplesCount++;

            return this;
        }

        public string GetMinAvgMaxTime()
        {
            return string.Format("{0}/{1}/{2}", MinTime, AvgTimeMs, MaxTime);
        }

        private readonly Stopwatch _watch;
        private long _elapsedTime;
        private bool _isMinTimeInit;
    }
}
