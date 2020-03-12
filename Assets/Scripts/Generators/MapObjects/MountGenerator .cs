using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Generators.MapObjects
{
    public class MountGenerator : ObjectMapGenerator
    {
        private readonly int _radius;
        private readonly float _height;
        private readonly MicroMap _parentMap;

        public MountGenerator(int radius, float height, MicroMap parentMap)
        {
            _radius = radius;
            _height = height;
            _parentMap = parentMap;
        }

        protected override Bound2i CalculateBounds(Vector2 instancePosition)
        {
            return new Bound2i((GridPos)instancePosition, _radius);
        }

        protected override bool IsBlockExist(GridPos blockPosition)
        {
            return Vector2.Distance(BlockInfo.GetWorldCenter(blockPosition), InstancePosition) < _radius;
        }

        protected override void GenerateHeight(GridPos vertexPosition, float instanceHeight, out Heights heightVertex)
        {
            ref readonly var parentHeightVertex = ref _parentMap.GetHeightRef(vertexPosition);
            var mainMapBlockHeight = parentHeightVertex.Nominal;
            var mainHeight = Interpolation.InQuad(Mathf.InverseLerp(_radius, 0, Vector2.Distance(InstancePosition, vertexPosition))) * _height + mainMapBlockHeight + instanceHeight;

            heightVertex = new Heights(mainHeight, mainMapBlockHeight - 3);
        }

        protected override void GenerateBlock(GridPos blockPosition, out Blocks block)
        {
            block = new Blocks(BlockType.Grass, BlockType.Empty);
        }
    }
}
