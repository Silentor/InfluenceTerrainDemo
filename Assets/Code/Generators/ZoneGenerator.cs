using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Generators
{
    public abstract class ZoneGenerator
    {
        public abstract BlockType DefaultBLock { get; }

        private readonly Zone _zone;
        private readonly Land _land;
        private readonly int _blocksCount;
        private readonly int _blockSize;
        private readonly int _chunkSize;

        public ZoneGenerator(Zone zone, Land land, int blocksCount, int blockSize)
        {
            _zone = zone;
            _land = land;
            _blocksCount = blocksCount;
            _blockSize = blockSize;
            _chunkSize = _blocksCount*_blockSize;
        }

        public void Generate(Dictionary<Vector2i, Chunk> land)
        {
            var chunksPos = _land.GetChunks(_zone);

            foreach (var chunkPos in chunksPos)
            {
                var newChunk = GenerateChunk(chunkPos, false);
                land[chunkPos] = newChunk;
            }
        }

        private Chunk GenerateChunk(Vector2i position, bool interpolate)
        {
            ZoneRatio corner11 = null;
            ZoneRatio corner12 = null;
            ZoneRatio corner22 = null;
            ZoneRatio corner21 = null;

            if (interpolate)
            {
                //Get zones influence for chunk corners
                var chunkMin = position * _chunkSize;
                var chunkMax = (position + Vector2i.One) * _chunkSize;
                corner11 = _land.GetInfluence((Vector2)chunkMin);
                corner12 = _land.GetInfluence(new Vector2(chunkMin.X, chunkMax.Z));
                corner22 = _land.GetInfluence((Vector2)chunkMax);
                corner21 = _land.GetInfluence(new Vector2(chunkMax.X, chunkMin.Z));
            }

            var chunk = new Chunk(_blocksCount, _blockSize, position);

            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var realX = x * chunk.BlockSize + position.X * chunk.Size;
                    var realZ = z * chunk.BlockSize + position.Z * chunk.Size;

                    ZoneRatio influence;

                    if (interpolate)
                    {
                        influence = _land.GetBilinearInterpolationInfluence(new Vector2(x, z), Vector2.zero, Vector2.one * _chunkSize, corner11, corner12, corner21, corner22);
                    }
                    else
                        influence = _land.GetInfluence(new Vector2(realX, realZ));

                    var settings = _land.GetZoneSettings(influence);

                    //Workaround of Unity stupid PerlinNoise symmetry
                    realX += 1000;
                    realZ += 1000;

                    var yValue = 0f;
                    yValue +=
                        Mathf.PerlinNoise(realX * _land.InScale1, realZ * _land.InScale1) * settings.OutScale1;
                    yValue +=
                        Mathf.PerlinNoise(realX * _land.InScale2, realZ * _land.InScale2) * settings.OutScale2;
                    yValue +=
                        Mathf.PerlinNoise(realX * _land.InScale3, realZ * _land.InScale3) * settings.OutScale3;
                    yValue += settings.Height;

                    chunk.HeightMap[x, z] = yValue;
                    chunk.Influence[x, z] = influence;
                }

            for (int x = 0; x < chunk.BlocksCount; x++)
                for (int z = 0; z < chunk.BlocksCount; z++)
                    chunk.BlockType[x, z] = DefaultBLock;

            return chunk;
        }

        
    }
}
