using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using TerrainDemo.Generators.Debug;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using TerrainDemo.Threads;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class LandGenerator
    {
        public readonly Dictionary<int, MountainsGenerator.MountainClusterContext> Clusters = new Dictionary<int, MountainsGenerator.MountainClusterContext>();

        public LandGenerator(LandLayout land, LandSettings settings, LandMap map)
        {
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
                        var result = generator.Generate();
                        _map.Add(result);
                    }
                }
            }
        }

        private readonly LandLayout _land;
        private readonly LandSettings _settings;
        private readonly LandMap _map;
        private readonly LandGeneratorWorker _worker;
        private readonly Dictionary<ZoneType, ZoneGenerator> _generators = new Dictionary<ZoneType, ZoneGenerator>();

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
