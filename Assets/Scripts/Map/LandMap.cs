using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Generators;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Map
{
    /// <summary>
    /// Terrain storage (chunks)
    /// </summary>
    public class LandMap
    {
        public readonly Dictionary<Vector2i, Chunk> Chunks = new Dictionary<Vector2i, Chunk>();

        public LandLayout Layout { get; private set; }

        public LandMap(LandSettings settings, LandLayout layout)
        {
            _settings = settings;
            Layout = layout;
        }

        /// <summary>
        /// Merge some zone content to existing map
        /// </summary>
        /// <param name="content"></param>
        public void Add(ZoneGenerator.ZoneContent content)
        {
            //Get zone content bounds
            //var zoneChunkBounds = content.Zone.ChunkBounds;
            var zoneChunkBounds = content.Zone.Chunks;
            var zoneBounds = content.Zone.Bounds;

            Chunk chunk;
            //Copy zone content data
            foreach (var zoneChunk in zoneChunkBounds)
            {
                //Debug.LogFormat("Merge chunk {0} of zone {1}", zoneChunk, content.Zone.Cell.Id);

                if (!Chunks.TryGetValue(zoneChunk, out chunk))
                {
                    chunk = new Chunk(_settings.BlocksCount, _settings.BlockSize, zoneChunk);
                    Chunks.Add(zoneChunk, chunk);
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

                Modified(chunk);
            }
        }

        /// <summary>
        /// Get block from land map
        /// </summary>
        /// <param name="position">World</param>
        /// <returns></returns>
        public BlockInfo GetBlock(Vector2i position)
        {
            if (_settings.LandBounds.Contains(position))
            {
                var chunkPosition = Chunk.GetPositionFromBlock(position);
                Chunk chunk;
                if (Chunks.TryGetValue(chunkPosition, out chunk))
                {
                    var localPos = Chunk.GetLocalPosition(position);
                    var result = new BlockInfo(position, chunk.BlockType[localPos.X, localPos.Z], chunk.HeightMap[localPos.X, localPos.Z], chunk.NormalMap[localPos.X, localPos.Z]);
                    return result;
                }
            }

            return new BlockInfo(position, BlockType.Empty, 0, Vector3.zero);
        }

        public Vector3? GetRayMapIntersection(Ray ray)
        {
            var from = new Vector2(ray.origin.x, ray.origin.z);
            var to = new Vector2(ray.GetPoint(1000).x, ray.GetPoint(1000).z);
            var blocks = Rasterization.DDA((OpenTK.Vector2)from, (OpenTK.Vector2)to, true);

            foreach (var blockPos in blocks)
            {
                if (_settings.LandBounds.Contains(blockPos))
                {
                    var chunkPosition = Chunk.GetPositionFromBlock(blockPos);
                    Chunk chunk;
                    if (Chunks.TryGetValue(chunkPosition, out chunk))
                    {
                        var localPos = Chunk.GetLocalPosition(blockPos);
                        var tr1 = new Vector3(blockPos.X, chunk.HeightMap[localPos.X, localPos.Z], blockPos.Z);
                        var tr2 = new Vector3(blockPos.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z], blockPos.Z);
                        var tr3 = new Vector3(blockPos.X, chunk.HeightMap[localPos.X, localPos.Z + 1], blockPos.Z + 1);
                        var tr4 = new Vector3(blockPos.X + 1, chunk.HeightMap[localPos.X + 1, localPos.Z + 1], blockPos.Z + 1);

                        Vector3 i;
                        var ir = Intersections.LineTriangleIntersection(ray, tr1, tr2, tr3, out i);
                        if (ir == 1)
                        {
                            return i;
                        }
                        else
                        {
                            ir = Intersections.LineTriangleIntersection(ray, tr2, tr3, tr4, out i);
                            if (ir == 1)
                            {
                                return i;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void Clear()
        {
            var chunksToRemove = Chunks.Values.ToArray();

            foreach (var chunk in chunksToRemove)
            {
                Chunks.Remove(chunk.Position);
                Removed(chunk.Position);
            }
        }

        public event Action<Chunk> Modified = delegate {};
        public event Action<Vector2i> Removed = delegate { };

        private readonly LandSettings _settings;
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

        public static readonly BlockInfo Empty = new BlockInfo(Vector2i.Zero, BlockType.Empty, 0, Vector3.zero);

        public static Bounds2i GetBounds(Vector2i worldPosition)
        {
            return new Bounds2i(worldPosition, 1, 1);
        }
    }
}
