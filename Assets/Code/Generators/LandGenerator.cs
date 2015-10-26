using System.Collections.Generic;
using Assets.Code.Layout;
using Assets.Code.Settings;

namespace Assets.Code.Generators
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
        public Dictionary<Vector2i, Chunk> Generate()
        {
            var result = new Dictionary<Vector2i, Chunk>();

            foreach (var zoneMarkup in _land.Zones)
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
                else if (zoneMarkup.Type >= ZoneType.Hills && zoneMarkup.Type <= ZoneType.Lake)
                    generator = new DefaultGenerator(zoneMarkup, _land, _settings);
                else if (zoneMarkup.Type >= ZoneType.Influence1 && zoneMarkup.Type <= ZoneType.Influence8)
                    generator = new FlatGenerator(zoneMarkup, _land, _settings);

                if (generator != null)
                    generator.Generate(result);
            }

            return result;
        }

        private readonly LandLayout _land;
        private readonly ILandSettings _settings;
    }
}
