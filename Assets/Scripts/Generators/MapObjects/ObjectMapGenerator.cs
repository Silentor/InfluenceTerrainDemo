using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Generators.MapObjects
{
    public abstract class ObjectMapGenerator
    {
        public abstract (Bounds2i bounds, IEnumerable<Vector2i> vertexPositions, Heights[,] heightmap, IEnumerable<Vector2i>
            blockPositions,
            Blocks[,] blockmap) Generate(Vector2i position, float height);
    }
}