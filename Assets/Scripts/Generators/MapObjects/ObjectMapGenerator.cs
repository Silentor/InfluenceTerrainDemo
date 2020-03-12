using System.Collections.Generic;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;

namespace TerrainDemo.Generators.MapObjects
{
    public abstract class ObjectMapGenerator
    {
        public MapObjectContent Generate(Vector2 instancePosition, float instanceHeight)
        {
            InstancePosition = instancePosition;
            InstanceHeight = instanceHeight;
            var preliminaryBounds = CalculateBounds(instancePosition);
            InstanceBounds = preliminaryBounds;

            var blockPositions = new List<GridPos>();
            var blocks = new Blocks[preliminaryBounds.Size.X, preliminaryBounds.Size.Z];
            var heightPositions = new List<GridPos>();
            var heights = new Heights[preliminaryBounds.Size.X + 1, preliminaryBounds.Size.Z + 1];

            for (var x = preliminaryBounds.Min.X; x <= preliminaryBounds.Max.X; x++)
                for (var z = preliminaryBounds.Min.Z; z <= preliminaryBounds.Max.Z; z++)
                {
                    var worldBlockPos = new GridPos(x, z);
                    var localX = x - preliminaryBounds.Min.X;
                    var localZ = z - preliminaryBounds.Min.Z;
                    if (IsBlockExist(worldBlockPos))
                    {
                        //Generate missing heights
                        if (heights[localX, localZ].IsEmpty)
                        {
                            GenerateHeight(worldBlockPos, instanceHeight, out heights[localX, localZ]);
                            heightPositions.Add(worldBlockPos);
                        }
                        if (heights[localX, localZ + 1].IsEmpty)
                        {
                            var worldBlockPos01 = new GridPos(x, z + 1);
                            GenerateHeight(worldBlockPos01, instanceHeight, out heights[localX, localZ + 1]);
                            heightPositions.Add(worldBlockPos01);
                        }
                        if (heights[localX + 1, localZ].IsEmpty)
                        {
                            var worldBlockPos10 = new GridPos(x + 1, z);
                            GenerateHeight(worldBlockPos10, instanceHeight, out heights[localX + 1, localZ]);
                            heightPositions.Add(worldBlockPos10);
                        }
                        if (heights[localX + 1, localZ + 1].IsEmpty)
                        {
                            var worldBlockPos11 = new GridPos(x + 1, z + 1);
                            GenerateHeight(worldBlockPos11, instanceHeight, out heights[localX + 1, localZ + 1]);
                            heightPositions.Add(worldBlockPos11);
                        }

                        //Generate block
                        GenerateBlock(worldBlockPos, out blocks[localX, localZ]);
                        blockPositions.Add(worldBlockPos);
                    }
                }

            return new MapObjectContent(preliminaryBounds, heightPositions, heights, blockPositions, blocks);
        }

        protected Vector2 InstancePosition;
        protected float InstanceHeight;
        protected Bound2i InstanceBounds;

        /// <summary>
        /// Preliminary bounds for object instance in given position
        /// </summary>
        /// <param name="instancePosition"></param>
        /// <returns></returns>
        protected abstract Bound2i CalculateBounds(Vector2 instancePosition);

        protected abstract bool IsBlockExist(GridPos blockPosition);

        protected abstract void GenerateHeight(GridPos vertexPosition, float instanceHeight, out Heights heightVertex);

        protected abstract void GenerateBlock(GridPos blockPosition, out Blocks block);

    }

    public readonly struct MapObjectContent
    {
        public readonly Bound2i Bounds;
        public readonly IEnumerable<GridPos> VertexPositions;
        public readonly Heights[,] Heightmap;
        public readonly IEnumerable<GridPos> BlockPositions;
        public readonly Blocks[,] Blockmap;

        public MapObjectContent(Bound2i bounds, IEnumerable<GridPos> vertexPositions, Heights[,] heightmap, IEnumerable<GridPos> blockPositions, Blocks[,] blockmap)
        {
            Bounds = bounds;
            VertexPositions = vertexPositions;
            Heightmap = heightmap;
            BlockPositions = blockPositions;
            Blockmap = blockmap;
        }
    }
}