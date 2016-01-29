using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public abstract class ZoneGenerator
    {
        public virtual BlockType DefaultBlock { get; private set; }

        public ZoneContent Generate()
        {
            var zoneBounds = _zone.Bounds;
            var zoneInfluences = new ZoneRatio[zoneBounds.Size.X + 1, zoneBounds.Size.Z + 1];
            var zoneHeightmap = new float[zoneBounds.Size.X + 1, zoneBounds.Size.Z + 1];
            var zoneNormalmap = new Vector3[zoneBounds.Size.X + 1, zoneBounds.Size.Z + 1];
            var zoneBlocks = new BlockType[zoneBounds.Size.X, zoneBounds.Size.Z];

            Blocks = _zone.GetBlocks().ToArray();
            foreach (var blockPos in Blocks)
            {
                var localPos = blockPos - zoneBounds.Min;

                //Build heightmap    
                CalculateVertex(blockPos, zoneInfluences, zoneHeightmap);
                CalculateVertex(blockPos + Vector2i.Right, zoneInfluences, zoneHeightmap);
                CalculateVertex(blockPos + Vector2i.Forward, zoneInfluences, zoneHeightmap);
                CalculateVertex(blockPos + Vector2i.One, zoneInfluences, zoneHeightmap);

                zoneNormalmap[localPos.X, localPos.Z] = CalculateNormal(localPos, zoneHeightmap);

                //Build block
                ZoneRatio blockInfluence;

                var turbulenceX = (Mathf.PerlinNoise(blockPos.X * 0.1f, blockPos.Z * 0.1f) - 0.5f) * 10;
                var turbulenceZ = (Mathf.PerlinNoise(blockPos.Z * 0.1f, blockPos.X * 0.1f) - 0.5f) * 10;

                var localTurbulated = localPos + new Vector2i(turbulenceX, turbulenceZ);
                if (localTurbulated.X >= 0 && localTurbulated.X < zoneBounds.Size.X + 1
                    && localTurbulated.Z >= 0 && localTurbulated.Z < zoneBounds.Size.Z + 1)
                {
                    if (!zoneInfluences[localTurbulated.X, localTurbulated.Z].IsEmpty)
                    {
                        blockInfluence = zoneInfluences[localTurbulated.X, localTurbulated.Z];
                    }
                    else
                    {
                        var worldTurbulated = blockPos + new Vector2i(turbulenceX, turbulenceZ);
                        blockInfluence =
                            zoneInfluences[localPos.X, localPos.Z] =
                                Land.GetInfluence((Vector2)worldTurbulated);}
                }
                else
                {
                    var worldTurbulated = blockPos + new Vector2i(turbulenceX, turbulenceZ);
                    blockInfluence = Land.GetInfluence((Vector2)worldTurbulated);
                }

                zoneBlocks[localPos.X, localPos.Z] = GenerateBlock(blockPos, new Vector2i(turbulenceX, turbulenceZ), 
                    zoneNormalmap[localPos.X, localPos.Z], blockInfluence);
                
            }

            var props = DecorateZone(zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks);

            return new ZoneContent(_zone, zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks, props);
        }

        protected readonly ZoneLayout _zone;
        protected readonly LandLayout Land;
        private readonly ILandSettings _landSettings;
        private readonly int _blocksCount;
        private readonly int _blockSize;
        private readonly int _chunkSize;
        private ZoneType _zoneMaxType;
        private ZoneRatio _influence;
        protected Vector2i[] Blocks;

        protected ZoneGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] ILandSettings landSettings)
        {
            if (land == null) throw new ArgumentNullException("land");
            if (landSettings == null) throw new ArgumentNullException("landSettings");

            _zone = zone;
            Land = land;
            _landSettings = landSettings;
            _blocksCount = landSettings.BlocksCount;
            _blockSize = landSettings.BlockSize;
            _chunkSize = _blocksCount*_blockSize;
            _zoneMaxType = landSettings.ZoneTypes.Max(z => z.Type);
            DefaultBlock = landSettings.ZoneTypes.First(z => z.Type == zone.Type).DefaultBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="turbulence"></param>
        /// <param name="normal"></param>
        /// <param name="influence"></param>
        /// <returns></returns>
        protected virtual BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            var zoneValue = influence[0];
            return _landSettings[zoneValue.Zone].DefaultBlock;
        }

        protected virtual float GenerateBaseHeight(float worldX, float worldZ, IZoneNoiseSettings settings)
        {
            //Workaround of stupid Unity PerlinNoise symmetry
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

        private Vector3 CalculateNormal(Vector2i localBlockPos, float[,] zoneHeightmap)
        {
            var dx = zoneHeightmap[localBlockPos.X, localBlockPos.Z] - zoneHeightmap[localBlockPos.X + 1, localBlockPos.Z + 1];
            var dz = zoneHeightmap[localBlockPos.X, localBlockPos.Z + 1] - zoneHeightmap[localBlockPos.X + 1, localBlockPos.Z];
            return Vector3.Normalize(new Vector3(dx, 2, dz));
        }

        private void CalculateVertex(Vector2i vertPos, ZoneRatio[,] zoneInfluences, float[,] zoneHeightmap)
        {
            var localPos = vertPos - _zone.Bounds.Min;

            if (zoneInfluences[localPos.X, localPos.Z].IsEmpty)
            {
                zoneInfluences[localPos.X, localPos.Z] = Land.GetInfluence((Vector2) vertPos);
                var settings = Land.GetZoneNoiseSettings(zoneInfluences[localPos.X, localPos.Z]);
                var yValue = GenerateBaseHeight(vertPos.X, vertPos.Z, settings);
                zoneHeightmap[localPos.X, localPos.Z] = yValue;
            }
        }

        protected virtual Vector3[] DecorateZone(ZoneRatio[,] zoneInfluences, float[,] zoneHeightmap, Vector3[,] zoneNormalmap, BlockType[,] zoneBlocks)
        {
            return new Vector3[0];
        }


        /// <summary>
        /// Zone map
        /// </summary>
        public struct ZoneContent
        {
            public readonly ZoneLayout Zone;
            public readonly ZoneRatio[,] Influences;
            public readonly float[,] HeightMap;
            public readonly Vector3[,] NormalMap;
            public readonly BlockType[,] Blocks;
            public readonly Vector3[] Objects;

            public ZoneContent(ZoneLayout zone, ZoneRatio[,] influences, float[,] heightMap, Vector3[,] normalMap, BlockType[,] blocks, Vector3[] objects)
            {
                Zone = zone;
                Influences = influences;
                HeightMap = heightMap;
                NormalMap = normalMap;
                Blocks = blocks;
                Objects = objects;
            }

            public Vector2i GetWorldPosition(Vector2i localPosition)
            {
                return Zone.Bounds.Min + localPosition;
            }

            public Vector2i GetLocalPosition(Vector2i worldPosition)
            {
                return worldPosition - Zone.Bounds.Min;
            }
        }
    }
}
