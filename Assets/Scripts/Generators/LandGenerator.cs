using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;
using TerrainDemo.Generators.Debug;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using TerrainDemo.Threads;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class LandGenerator
    {
        public readonly Dictionary<int, MountainsGenerator.MountainClusterContext> Clusters = new Dictionary<int, MountainsGenerator.MountainClusterContext>();

        public LandGenerator([NotNull] LandLayout land, [NotNull] LandSettings settings, [NotNull] LandMap map)
        {
            if (land == null) throw new ArgumentNullException("land");
            if (settings == null) throw new ArgumentNullException("settings");
            if (map == null) throw new ArgumentNullException("map");

            _land = land;
            _settings = settings;
            _map = map;
            _worker = new LandGeneratorWorker();
            _worker.Completed += WorkerOnCompleted;
        }

        /// <summary>
        /// Generate land
        /// </summary>
        public void Generate(bool isAsync)
        {
            Generate(_land.Zones.Where(z => z.Cell.IsClosed), isAsync);
        }

        /// <summary>
        /// Generate given zones
        /// </summary>
        public void Generate(IEnumerable<ZoneLayout> zones, bool isAsync)
        {
            foreach (var zoneLayout in zones)
            {
                ZoneGenerator generator = ZoneGenerator.Create(zoneLayout, _land, this, _settings);

                if (generator != null)
                {
                    if(isAsync)
                        _worker.AddWork(generator);
                    else
                    {
                        AverageTimer timer;
                        if (!_generationTimers.TryGetValue(zoneLayout.Type, out timer))
                        {
                            timer = new AverageTimer();
                            _generationTimers.Add(zoneLayout.Type, timer);
                        }

                        timer.Start();
                        var result = generator.Generate();
                        timer.Stop();

                        _map.Add(result);
                    }
                }
            }
        }

        /// <summary>
        /// Average time of zone generation by zone type
        /// </summary>
        public void PrintDebug()
        {
            var zoneTimersLog = string.Join(", ", _generationTimers.OrderBy(t => t.Key).Select(t => string.Format("{0} average time: {1} ms", t.Key, t.Value.AvgTimeMs)).ToArray());
            UnityEngine.Debug.Log("Zones generation time: " + zoneTimersLog);
        }

        private readonly LandLayout _land;
        private readonly LandSettings _settings;
        private readonly LandMap _map;
        private readonly LandGeneratorWorker _worker;
        private readonly Dictionary<ZoneType, ZoneGenerator> _generators = new Dictionary<ZoneType, ZoneGenerator>();
        private readonly Dictionary<ZoneType, AverageTimer> _generationTimers = new Dictionary<ZoneType, AverageTimer>();

        private void WorkerOnCompleted(ZoneGenerator.ZoneContent zoneContent)
        {
            _map.Add(zoneContent);
        }
    }

    public class LandGeneratorWorker : WorkerPool.WorkerBase<ZoneGenerator, ZoneGenerator.ZoneContent>
    {
        protected override ZoneGenerator.ZoneContent WorkerLogic(ZoneGenerator input)
        {
            var result = input.Generate();

            UnityEngine.Debug.LogFormat("Generated zone {0}", result.Zone.Cell.Id);

            return result;
        }
    }
}
