using System.Collections.Generic;

namespace Assets.Code.Generators
{
    public class LandGenerator
    {
        public LandGenerator(Land markup)
        {
            _markup = markup;
        }

        /// <summary>
        /// Generate land
        /// </summary>
        public void Generate(Dictionary<Vector2i, Chunk> land)
        {
            foreach (var zoneMarkup in _markup.Zones)
            {
                ZoneGenerator generator = null;
                if (zoneMarkup.Type == ZoneType.Hills)
                {
                    generator = new HillsGenerator(zoneMarkup, _markup, 16, 1);
                }
                else if (zoneMarkup.Type == ZoneType.Lake)
                {
                    generator = new LakeGenerator(zoneMarkup, _markup, 16, 1);
                }

                if(generator != null)
                    generator.Generate(land);
            }
        }

        private readonly Land _markup;
        private readonly Dictionary<Vector2i, Chunk> _land;

        private ZoneGenerator[] _zoneGenerators;

    }
}
