using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Generators.MapObjects
{
    public class LaputaGenerator : ObjectMapGenerator
    {
        private readonly int _radius;

        public LaputaGenerator(int radius)
        {
            _radius = radius;
        }

        protected override Bound2i CalculateBounds(Vector2 instancePosition)
        {
            return new Bound2i((GridPos)instancePosition, _radius);
        }

        protected override bool IsBlockExist(GridPos blockPosition)
        {
            return Vector2.Distance(blockPosition, InstancePosition) < _radius;
        }

        protected override void GenerateHeight(GridPos vertexPosition, float instanceHeight, out Heights heightVertex)
        {
            var mainHeight = Mathf.Sqrt(Math.Max(_radius - Vector2.Distance(vertexPosition, InstancePosition), 0.1f));
            heightVertex = new Heights(instanceHeight + mainHeight / 2, instanceHeight - mainHeight);
        }

        protected override void GenerateBlock(GridPos blockPosition, out Blocks block)
        {
            block = new Blocks(BlockType.Grass, BlockType.Empty);
        }
    }
}
