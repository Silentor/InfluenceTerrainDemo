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

        public override (Bounds2i bounds, IEnumerable<Vector2i> vertexPositions, Heights[,] heightmap, IEnumerable<Vector2i> blockPositions, Blocks[,] blockmap) Generate(Vector2i position, float height)
        {
            var bounds = new Bounds2i(position, _radius);
            var blockPositions = new List<Vector2i>();
            var blocks = new Blocks[bounds.Size.X, bounds.Size.Z];
            var heightPositions = new List<Vector2i>();
            var heights = new Heights[bounds.Size.X + 1, bounds.Size.Z + 1];

            for (int x = bounds.Min.X; x < bounds.Max.X; x++)
                for (int z = bounds.Min.Z; z < bounds.Max.Z; z++)
                {
                    var localPosX = x - bounds.Min.X;
                    var localPosZ = z - bounds.Min.Z;
                    var pos = new Vector2(x + 0.5f, z + 0.5f);
                    if (Vector2.Distance(pos, position) < _radius)
                    {
                        //Generate heights
                        AddHeight(localPosX, localPosZ);
                        AddHeight(localPosX, localPosZ + 1);
                        AddHeight(localPosX + 1, localPosZ);
                        AddHeight(localPosX + 1, localPosZ + 1);

                        blockPositions.Add(new Vector2i(x, z));
                        blocks[localPosX, localPosZ] = new Blocks(BlockType.Grass, BlockType.Empty);
                    }
                }

            return (bounds, heightPositions, heights, blockPositions, blocks);

            void AddHeight(int localPosX, int localPosZ)
            {
                if (heights[localPosX, localPosZ].IsEmpty)
                {
                    var worldPosition = new Vector2(localPosX + bounds.Min.X, localPosZ + bounds.Min.Z);
                    var mainHeight = Mathf.Sqrt(_radius * _radius - Vector2.DistanceSquared(position, worldPosition));
                    mainHeight = Mathf.Max(mainHeight - 2, 0.25f);

                    heights[localPosX, localPosZ] =
                        new Heights(height + mainHeight / 2, height - mainHeight);

                     heightPositions.Add((Vector2i)worldPosition);
                }
            }
        }
    }
}
