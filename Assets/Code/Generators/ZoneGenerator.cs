using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code.Layout;
using Assets.Code.Settings;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Code.Generators
{
    public abstract class ZoneGenerator
    {
        public virtual BlockType DefaultBLock { get; private set; }

        public void Generate(Dictionary<Vector2i, Chunk> land)
        {
            var chunksPos = Land.GetChunks(_zone);

            foreach (var chunkPos in chunksPos)
            {
                var newChunk = GenerateChunk(chunkPos, _landSettings.InterpolateInfluence);
                DecorateChunk(newChunk);
                land[chunkPos] = newChunk;
            }
        }

        protected virtual void DecorateChunk(Chunk chunk)
        {
            
        }

        private readonly Zone _zone;
        protected readonly Land Land;
        private readonly ILandSettings _landSettings;
        private readonly int _blocksCount;
        private readonly int _blockSize;
        private readonly int _chunkSize;
        private ZoneType _zoneMaxType;
        private ZoneRatio _influence;

        protected ZoneGenerator([NotNull] Zone zone, [NotNull] Land land, [NotNull] ILandSettings landSettings)
        {
            if (zone == null) throw new ArgumentNullException("zone");
            if (land == null) throw new ArgumentNullException("land");
            if (landSettings == null) throw new ArgumentNullException("landSettings");

            _zone = zone;
            Land = land;
            _landSettings = landSettings;
            _blocksCount = landSettings.BlocksCount;
            _blockSize = landSettings.BlockSize;
            _chunkSize = _blocksCount*_blockSize;
            _zoneMaxType = landSettings.ZoneTypes.Max(z => z.Type);
            DefaultBLock = landSettings.ZoneTypes.First(z => z.Type == zone.Type).DefaultBlock;
        }

        private Chunk GenerateChunk(Vector2i position, bool interpolate)
        {
            //ZoneRatio corner11 = null;
            //ZoneRatio corner12 = null;
            //ZoneRatio corner22 = null;
            //ZoneRatio corner21 = null;
            var chunkBounds = Chunk.GetChunkBounds(position, _chunkSize);

            //if (interpolate)
            //{
            //    //Get zones influence for chunk corners for interpolate later
            //    corner11 = _land.GetInfluence(chunkBounds.min);
            //    corner12 = _land.GetInfluence(new Vector2(chunkBounds.min.x, chunkBounds.max.y));
            //    corner22 = _land.GetInfluence(chunkBounds.max);
            //    corner21 = _land.GetInfluence(new Vector2(chunkBounds.max.x, chunkBounds.min.y));
            //}

            var chunk = new Chunk(_blocksCount, _blockSize, position);

            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                {
                    var realX = x * chunk.BlockSize + position.X * chunk.Size;
                    var realZ = z * chunk.BlockSize + position.Z * chunk.Size;

                    if (interpolate)
                    {
                        //_influence = _land.GetBilinearInterpolationInfluence(new Vector2(realX, realZ), chunkBounds.min, chunkBounds.max, corner11, corner12, corner21, corner22);
                    }
                    else
                        _influence = Land.GetInfluence(new Vector2(realX, realZ));

                    var settings = Land.GetZoneNoiseSettings(_influence);

                    var yValue = GenerateBaseHeight(realX, realZ, settings);

                    chunk.HeightMap[x, z] = yValue;
                    chunk.Influence[x, z] = _influence;
                }

            //Generate blocks
            for (int x = 0; x < chunk.BlocksCount; x++)
                for (int z = 0; z < chunk.BlocksCount; z++)
                    //chunk.BlockType[x, z] = DefaultBLock;
                {
                    var blockX = x * chunk.BlockSize + position.X * chunk.Size;
                    var blockZ = z * chunk.BlockSize + position.Z * chunk.Size;
                    //var turbulenceX = (Mathf.PerlinNoise(realX * 0.1f, 0) - 0.5f) * 10;
                    //var turbulenceZ = (Mathf.PerlinNoise(0, realZ * 0.1f) - 0.5f) * 10;
                    //var turbulenceZ = 0;

                    var block = GenerateBlock(blockX, blockZ);
                    chunk.BlockType[x, z] = block;

                    //var rnd = Random.value;
                    //var influence = chunk.Influence[x, z].Pack(3);

                    //if (influence[0].Value > 0.7f)
                    //    chunk.BlockType[x, z] = DefaultBLock;
                    //else
                    //{
                    //    var accum = 0f;
                    //    foreach (var infl in influence)
                    //        if (rnd < infl.Value + accum)
                    //        {
                    //            var block = _landSettings[infl.Zone].DefaultBlock;
                    //            chunk.BlockType[x, z] = block;
                    //            break;
                    //        }
                    //        else
                    //            accum += infl.Value;
                    //}
                }

            return chunk;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockX">World block position X</param>
        /// <param name="blockZ">World block position Z</param>
        /// <returns></returns>
        protected virtual BlockType GenerateBlock(int blockX, int blockZ)
        {
            var turbulenceX = (Mathf.PerlinNoise(blockX * 0.1f, blockZ * 0.1f) - 0.5f) * 10;
            var turbulenceZ = (Mathf.PerlinNoise(blockZ * 0.1f, blockX * 0.1f) - 0.5f) * 10;
            var influence = Land.GetInfluence(new Vector2(blockX + turbulenceX, blockZ + turbulenceZ));
            var block = _landSettings[influence[0].Zone].DefaultBlock;
            return block;
        }

        protected virtual float GenerateBaseHeight(int worldX, int worldZ, IZoneNoiseSettings settings)
        {
            //Workaround of Unity stupid PerlinNoise symmetry
            worldX += 1000;
            worldZ += 1000;

            var yValue = 0f;
            yValue +=
                Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale1, worldZ*_landSettings.LandNoiseSettings.InScale1)*
                settings.OutScale1;
            yValue +=
                Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale2, worldZ*_landSettings.LandNoiseSettings.InScale2)*
                settings.OutScale2;
            yValue +=
                Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale3, worldZ*_landSettings.LandNoiseSettings.InScale3)*
                settings.OutScale3;
            yValue += settings.Height;
            return yValue;
        }
    }
}
