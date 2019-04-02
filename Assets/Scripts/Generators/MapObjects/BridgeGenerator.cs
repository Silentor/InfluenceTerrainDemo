﻿using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Generators.MapObjects
{
    public class BridgeGenerator : ObjectMapGenerator
    {
        private readonly int _length;
        private readonly int _width;
        private readonly float _archHeight;
        private int _bridgeMiddle;

        public BridgeGenerator(int length, int width, float archHeight)
        {
            _length = length;
            _width = width;
            _archHeight = archHeight;
        }

        protected override Bounds2i CalculateBounds(Vector2 instancePosition)
        {
            var start = -_length / 2;
            var finish = _length / 2;
            var result = new Bounds2i(
                new Vector2i(start + instancePosition.X, instancePosition.Y), 
                new Vector2i(finish + instancePosition.X, _width - 1 + instancePosition.Y));
            _bridgeMiddle = (result.Min.X + result.Max.X) / 2;
            return result;
        }

        protected override bool IsBlockExist(Vector2i blockPosition)
        {
            //Bounds is described bridge blocks perfectly
            return true;
        }

        protected override void GenerateHeight(Vector2i vertexPosition, float instanceHeight, out Heights heightVertex)
        {
            var bounds = InstanceBounds;
            var stairwayBlockHeight =
                (Interpolation.SmoothStep(Mathf.InverseLerp(bounds.Min.X, _bridgeMiddle, vertexPosition.X))
                 - Interpolation.SmoothStep(Mathf.InverseLerp(_bridgeMiddle, bounds.Max.X, vertexPosition.X))) 
                * _archHeight + instanceHeight;

            var baseBlockHeight = stairwayBlockHeight - 2;

            //Some curvy sides
            if (vertexPosition.Z <= bounds.Min.Z || vertexPosition.Z >= bounds.Max.Z + 1)
            {
                stairwayBlockHeight -= 0.75f;
                baseBlockHeight += 0.75f;
            }
           
            heightVertex = new Heights(stairwayBlockHeight, baseBlockHeight, baseBlockHeight);
        }

        protected override void GenerateBlock(Vector2i blockPosition, out Blocks block)
        {
            block = new Blocks(BlockType.Stone, BlockType.Empty);
        }
    }
}
