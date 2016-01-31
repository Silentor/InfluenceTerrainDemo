using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Generators;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Map
{
    public class LandMap
    {
        public readonly Dictionary<Vector2i, Chunk> Map = new Dictionary<Vector2i, Chunk>();

        public LandLayout Layout { get; private set; }

        public LandMap(ILandSettings settings, LandLayout layout)
        {
            _settings = settings;
            Layout = layout;
        }

        /// <summary>
        /// Append some zone content to existing map
        /// </summary>
        /// <param name="content"></param>
        public void Add(ZoneGenerator.ZoneContent content)
        {
            //Get zone content bounds
            var zoneChunkBounds = content.Zone.ChunkBounds;
            var zoneBounds = content.Zone.Bounds;

            Chunk chunk;
            //Copy zone content data
            foreach (var zoneChunk in zoneChunkBounds)
            {
                if (!Map.TryGetValue(zoneChunk, out chunk))
                {
                    chunk = new Chunk(_settings.BlocksCount, _settings.BlockSize, zoneChunk);
                    Map.Add(zoneChunk, chunk);
                }

                //Copy block data
                foreach (var worldBlockPos in Chunk.GetBounds(zoneChunk))
                {
                    if (zoneBounds.Contains(worldBlockPos))
                    {
                        var localZonePos = content.GetLocalPosition(worldBlockPos);
                        var block = content.Blocks[localZonePos.X, localZonePos.Z];
                        if (block != BlockType.Empty)
                        {
                            var localChunkPos = Chunk.GetLocalPosition(worldBlockPos);
                            //todo do not write already writed vertex data
                            chunk.HeightMap[localChunkPos.X, localChunkPos.Z] = content.HeightMap[localZonePos.X, localZonePos.Z];
                            chunk.HeightMap[localChunkPos.X + 1, localChunkPos.Z] = content.HeightMap[localZonePos.X + 1, localZonePos.Z];
                            chunk.HeightMap[localChunkPos.X, localChunkPos.Z + 1] = content.HeightMap[localZonePos.X, localZonePos.Z + 1];
                            chunk.HeightMap[localChunkPos.X + 1, localChunkPos.Z + 1] = content.HeightMap[localZonePos.X + 1, localZonePos.Z + 1];
                            chunk.Influence[localChunkPos.X, localChunkPos.Z] = content.Influences[localZonePos.X, localZonePos.Z];
                            chunk.Influence[localChunkPos.X + 1, localChunkPos.Z] = content.Influences[localZonePos.X + 1, localZonePos.Z];
                            chunk.Influence[localChunkPos.X, localChunkPos.Z + 1] = content.Influences[localZonePos.X, localZonePos.Z + 1];
                            chunk.Influence[localChunkPos.X + 1, localChunkPos.Z + 1] = content.Influences[localZonePos.X + 1, localZonePos.Z + 1];
                            chunk.NormalMap[localChunkPos.X, localChunkPos.Z] = content.NormalMap[localZonePos.X, localZonePos.Z];
                            chunk.NormalMap[localChunkPos.X + 1, localChunkPos.Z] = content.NormalMap[localZonePos.X + 1, localZonePos.Z];
                            chunk.NormalMap[localChunkPos.X, localChunkPos.Z + 1] = content.NormalMap[localZonePos.X, localZonePos.Z + 1];
                            chunk.NormalMap[localChunkPos.X + 1, localChunkPos.Z + 1] = content.NormalMap[localZonePos.X + 1, localZonePos.Z + 1];
                            chunk.BlockType[localChunkPos.X, localChunkPos.Z] = content.Blocks[localZonePos.X, localZonePos.Z];
                        }
                    }
                }

                //Copy props
                if (content.Objects.Length > 0)
                {
                    var chunkBounds = Chunk.GetBounds(zoneChunk);
                    var propsInBounds = content.Objects.Where(o => chunkBounds.Contains((Vector2i) o));
                    chunk.Flora.AddRange(propsInBounds);
                }
            }
        }

        /// <summary>
        /// Get block from land map of empty block
        /// </summary>
        /// <param name="position">World</param>
        /// <returns></returns>
        public BlockInfo GetBlock(Vector2i position)
        {
            if (_settings.LandBounds.Contains(position))
            {
                var chunkPosition = Chunk.GetPosition(position);
                Chunk chunk;
                if (Map.TryGetValue(chunkPosition, out chunk))
                {
                    var localPos = Chunk.GetLocalPosition(position);
                    var result = new BlockInfo(position, chunk.BlockType[localPos.X, localPos.Z], chunk.HeightMap[localPos.X, localPos.Z], chunk.NormalMap[localPos.X, localPos.Z]);
                    return result;
                }
            }

            return new BlockInfo(position, BlockType.Empty, 0, Vector3.zero);
        }

        private readonly ILandSettings _settings;
    }

    public struct BlockInfo
    {
        public readonly Vector2i Position;
        public readonly BlockType Type;
        public readonly float Height;
        public Vector3 Normal;

        public BlockInfo(Vector2i position, BlockType type, float height, Vector3 normal)
        {
            Position = position;
            Type = type;
            Height = height;
            Normal = normal;
        }
    }
}
