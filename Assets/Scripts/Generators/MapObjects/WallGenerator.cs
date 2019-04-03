using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Generators.MapObjects
{
    public class WallGenerator : ObjectMapGenerator
    {
        private readonly int _xSize;
        private readonly int _zSize;
        private readonly float _height;
        private readonly MicroMap _mainMap;

        public WallGenerator(int xSize, int zSize, float height, MicroMap mainMap)
        {
            _xSize = xSize;
            _zSize = zSize;
            _height = height;
            _mainMap = mainMap;
        }

        protected override Bounds2i CalculateBounds(Vector2 instancePosition)
        {
            return new Bounds2i((Vector2i)instancePosition, _xSize, _zSize);
        }

        protected override bool IsBlockExist(Vector2i blockPosition)
        {
            return true;
        }

        protected override void GenerateHeight(Vector2i vertexPosition, float instanceHeight, out Heights heightVertex)
        {
            ref readonly var mainMapHeight = ref _mainMap.GetHeightRef(vertexPosition);
            heightVertex = new Heights(mainMapHeight.Nominal + _height, mainMapHeight.Nominal);
        }

        protected override void GenerateBlock(Vector2i blockPosition, out Blocks block)
        {
            block = new Blocks(BlockType.Stone, BlockType.Empty);
        }
    }
}
