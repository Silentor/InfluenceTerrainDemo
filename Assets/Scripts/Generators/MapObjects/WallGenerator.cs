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
            return new Bounds2i((GridPos)instancePosition, _xSize, _zSize);
        }

        protected override bool IsBlockExist(GridPos blockPosition)
        {
            return true;
        }

        protected override void GenerateHeight(GridPos vertexPosition, float instanceHeight, out Heights heightVertex)
        {
            ref readonly var mainMapHeight = ref _mainMap.GetHeightRef(vertexPosition);

            //if(vertexPosition.X == 0 || vertexPosition.X == 1 || vertexPosition.Z == 0 || vertexPosition.Z == 1)
                heightVertex = new Heights(mainMapHeight.Nominal + _height, mainMapHeight.Nominal);
            //else
                //heightVertex = new Heights(mainMapHeight.Nominal, mainMapHeight.Nominal);
        }

        protected override void GenerateBlock(GridPos blockPosition, out Blocks block)
        {
            if(blockPosition.X == InstanceBounds.Min.X || blockPosition.Z == InstanceBounds.Min.Z)
                block = new Blocks(BlockType.Stone, BlockType.Empty);
            else
                block = Blocks.Empty;
        }
    }
}
