using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;

namespace TerrainDemo.Generators
{
    public class LandGenerator
    {
        public LandGenerator(LandLayout land, ILandSettings settings)
        {
            _land = land;
            _settings = settings;
        }

        /// <summary>
        /// Generate land
        /// </summary>
        /// <param name="map"></param>
        public LandMap Generate(LandMap map)
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

                if (generator != null)
                {
                    var zoneMap = generator.Generate();
                    map.Add(zoneMap);
                }
            }

            return map;
        }

        private readonly LandLayout _land;
        private readonly ILandSettings _settings;
    }
}
