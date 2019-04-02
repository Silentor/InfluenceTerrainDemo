using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Generators.MapObjects
{
    public class BridgeGenerator : ObjectMapGenerator
    {
        private readonly int _length;
        private readonly int _width;
        private readonly float _archHeight;

        public BridgeGenerator(int length, int width, float archHeight, float angle = 0)
        {
            _length = length;
            _width = width;
            _archHeight = archHeight;
        }

        public override (Bounds2i bounds, IEnumerable<Vector2i> vertexPositions, Heights[,] heightmap, IEnumerable<Vector2i> blockPositions, Blocks[,] blockmap) Generate(Vector2i position, float height)
        {
            var start = -_length / 2;
            var finish = _length / 2;
            int center = (start + finish) / 2;

            var bounds = new Bounds2i((start, 0), (finish, _width - 1)).Translate(position);
            var blockPositions = new List<Vector2i>();
            var blocks = new Blocks[bounds.Size.X, bounds.Size.Z];
            var heightPositions = new List<Vector2i>();
            var heights = new Heights[bounds.Size.X + 1, bounds.Size.Z + 1];

            for (int x = bounds.Min.X; x <= bounds.Max.X; x++) //length
            {
                for (int z = bounds.Min.Z; z <= bounds.Max.Z; z++)    //width
                {
                    var localPosX = x - bounds.Min.X;
                    var localPosZ = z - bounds.Min.Z;

                    //Generate heights
                    AddHeight(localPosX, localPosZ);
                    AddHeight(localPosX, localPosZ + 1);
                    AddHeight(localPosX + 1, localPosZ);
                    AddHeight(localPosX + 1, localPosZ + 1);

                    blockPositions.Add(new Vector2i(Mathf.RoundToInt(x), Mathf.RoundToInt(z)));
                    blocks[localPosX, localPosZ] = new Blocks(BlockType.Stone, BlockType.Empty);
                }
            }

            return (bounds, heightPositions, heights, blockPositions, blocks);

            void AddHeight(int localPosX, int localPosZ)
            {
                if (heights[localPosX, localPosZ].IsEmpty)
                {
                    var worldPosition = new Vector2i(localPosX + bounds.Min.X, localPosZ + bounds.Min.Z);

                    //Create pulse from two smoothsteps
                    var stairwayBlockHeight =
                        (Interpolation.SmoothStep(Mathf.InverseLerp(start, center, worldPosition.X))
                         - Interpolation.SmoothStep(Mathf.InverseLerp(center, finish, worldPosition.X))) * _archHeight + height;

                    if (localPosZ == 0 || localPosZ == _width)
                        stairwayBlockHeight -= 0.5f;

                    var baseBlockHeight = stairwayBlockHeight - 2;
                    if (localPosZ == 0 || localPosZ == _width)
                        baseBlockHeight += 0.5f;

                    heights[localPosX, localPosZ] = new Heights(stairwayBlockHeight, baseBlockHeight, baseBlockHeight);
                    heightPositions.Add(worldPosition);
                }
            }
        }
    }
}
