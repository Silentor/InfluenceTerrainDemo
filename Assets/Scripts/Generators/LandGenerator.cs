using System.Linq;
using System.Xml.XPath;
using TerrainDemo.Generators.Debug;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using TerrainDemo.Threads;

namespace TerrainDemo.Generators
{
    public class LandGenerator
    {
        public LandGenerator(LandLayout land, ILandSettings settings, LandMap map)
        {
            _land = land;
            _settings = settings;
            _map = map;
            _worker = new LandGeneratorWorker();
            _worker.Completed +=  WorkerOnCompleted;
        }

        /// <summary>
        /// Generate land
        /// </summary>
        public void GenerateAsync()
        {
            foreach (var zoneMarkup in _land.Zones.Where(z => z.Cell.IsClosed))
            {
                ZoneGenerator generator = null;
                if (zoneMarkup.Type == ZoneType.Hills)
                    generator = new HillsGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Lake)
                    generator = new LakeGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Forest)
                    generator = new ForestGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Mountains)
                    generator = new MountainsGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Snow)
                    generator = new SnowGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type >= ZoneType.Hills && zoneMarkup.Type <= ZoneType.Lake)
                    generator = new DefaultGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type >= ZoneType.Influence1 && zoneMarkup.Type <= ZoneType.Influence8)
                    generator = new FlatGenerator(zoneMarkup, _land, _settings);
                else if(zoneMarkup.Type == ZoneType.Checkboard)
                    generator = new CheckboardGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Cone)
                    generator = new ConeGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type == ZoneType.Slope)
                    generator = new SlopeGenerator(zoneMarkup, _land, _settings);

                if (generator != null)
                {
                    _worker.AddWork(generator);
                }
            }
        }

        private readonly LandLayout _land;
        private readonly ILandSettings _settings;
        private readonly LandMap _map;
        private readonly LandGeneratorWorker _worker;

        private void WorkerOnCompleted(ZoneGenerator.ZoneContent zoneContent)
        {
            _map.Add(zoneContent);
        }
    }

    public class LandGeneratorWorker : WorkerPool.WorkerBase<ZoneGenerator, ZoneGenerator.ZoneContent>
    {
        protected override ZoneGenerator.ZoneContent WorkerLogic(ZoneGenerator data)
        {
            var result = data.Generate();

            UnityEngine.Debug.LogFormat("Processed zone {0}", result.Zone.Cell.Id);

            return result;
        }
    }
}
