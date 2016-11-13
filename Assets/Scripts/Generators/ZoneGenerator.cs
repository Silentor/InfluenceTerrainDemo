using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Generators.Debug;
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
            var zoneNormalmap = new Vector3[zoneBounds.Size.X, zoneBounds.Size.Z];
            var zoneBlocks = new BlockType[zoneBounds.Size.X, zoneBounds.Size.Z];

            var blocks = _zone.GetBlocks2().ToArray();
            foreach (var blockPos in blocks)
            {
                var localPos = blockPos - zoneBounds.Min;

                //Build heightmap    
                CalculateVertex(blockPos, zoneInfluences, zoneHeightmap, _zone);
                CalculateVertex(blockPos + Vector2i.Right, zoneInfluences, zoneHeightmap, _zone);
                CalculateVertex(blockPos + Vector2i.Forward, zoneInfluences, zoneHeightmap, _zone);
                CalculateVertex(blockPos + Vector2i.One, zoneInfluences, zoneHeightmap, _zone);

                zoneNormalmap[localPos.X, localPos.Z] = CalculateNormal(localPos, zoneHeightmap);

                //Build block
                ZoneRatio blockInfluence = Land.GetInfluence((Vector2)blockPos);
                var turbulenceX = 0;
                var turbulenceZ = 0;

                /* disable turbulence for now
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
                */

                zoneBlocks[localPos.X, localPos.Z] = GenerateBlock(blockPos, new Vector2i(turbulenceX, turbulenceZ), 
                    zoneNormalmap[localPos.X, localPos.Z], blockInfluence);
                
            }

            var props = DecorateZone(zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks);

            return new ZoneContent(_zone, zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks, props);
        }

        public static ZoneGenerator Create(ZoneLayout zone, [NotNull] LandLayout land,
            [NotNull] ILandSettings landSettings)
        {
            return Create(zone.Type, zone, land, landSettings);
        }

        /// <summary>
        /// Its possible to create generator of other zone type for given zone to build border (cross-zone) blocks
        /// </summary>
        /// <param name="type"></param>
        /// <param name="zone"></param>
        /// <param name="land"></param>
        /// <param name="landSettings"></param>
        /// <returns></returns>
        public static ZoneGenerator Create(ZoneType type, ZoneLayout zone, [NotNull] LandLayout land, [NotNull] ILandSettings landSettings)
        {
            switch (type)
            {
                case ZoneType.Hills:
                    return new HillsGenerator(zone, land, landSettings);
                case ZoneType.Lake:
                        return new LakeGenerator(zone, land, landSettings);
                case ZoneType.Forest:
                        return new ForestGenerator(zone, land, landSettings);
                case ZoneType.Mountains:
                        return new MountainsGenerator(zone, land, landSettings);
                case ZoneType.Foothills:
                        return new FoothillsGenerator(zone, land, landSettings);
                case ZoneType.Snow:
                        return new SnowGenerator(zone, land, landSettings);
                case ZoneType.Desert:
                    return new DefaultGenerator(zone, land, landSettings);
                case ZoneType.Checkboard:
                        return new CheckboardGenerator(zone, land, landSettings);
                case ZoneType.Cone:
                        return new ConeGenerator(zone, land, landSettings);
                case ZoneType.Slope:
                        return new SlopeGenerator(zone, land, landSettings);
                default:
                    {
                        if (type >= ZoneType.Influence1 && type <= ZoneType.Influence8)
                            return new FlatGenerator(zone, land, landSettings);

                        throw new NotImplementedException(string.Format("Generator for zone type {0} is not implemented", type));
                    }
            }
        }

        private readonly ZoneType _type;
        private readonly ZoneLayout _zone;
        protected readonly LandLayout Land;
        protected readonly ILandSettings _landSettings;
        private ZoneRatio _influence;
        private static readonly float NormalY = 2*Mathf.Sqrt(3/2f);
        private static readonly Quaternion NormalCorrectRotation = Quaternion.Euler(0, -45, 0);
        private readonly Dictionary<ZoneType, ZoneGenerator> _generators = new Dictionary<ZoneType, ZoneGenerator>();
        protected ZoneSettings _zoneSettings;

        protected ZoneGenerator(ZoneType type, ZoneLayout zone, [NotNull] LandLayout land, [NotNull] ILandSettings landSettings)
        {
            if (land == null) throw new ArgumentNullException("land");
            if (landSettings == null) throw new ArgumentNullException("landSettings");

            _type = type;
            _zone = zone;
            Land = land;
            _landSettings = landSettings;
            _zoneSettings = landSettings[type];
            DefaultBlock = _zoneSettings.DefaultBlock;
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
            if (influence.IsEmpty)
                return BlockType.Empty;
            else
            {
                var zoneValue = influence[0];
                return _landSettings[zoneValue.Zone].DefaultBlock;
            }
        }

        public virtual float GenerateBaseHeight(float worldX, float worldZ, ZoneRatio influence)
        {
            //Workaround of stupid Unity PerlinNoise symmetry
            
            worldX += 1000;
            worldZ += 1000;

            var yValue = 0f;

            /*
            yValue +=
                (Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale1, worldZ*_landSettings.LandNoiseSettings.InScale1)) *
                _zoneSettings.OutScale1;
                
            yValue +=
                (Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale2, worldZ*_landSettings.LandNoiseSettings.InScale2)) *
                _zoneSettings.OutScale2;
                
            yValue +=
                (Mathf.PerlinNoise(worldX*_landSettings.LandNoiseSettings.InScale3, worldZ*_landSettings.LandNoiseSettings.InScale3)) * 
                _zoneSettings.OutScale3;
                */

            yValue += _zoneSettings.Height;
            return yValue;
        }

        private Vector3 CalculateNormal(Vector2i localBlockPos, float[,] zoneHeightmap)
        {
            //Based on http://gamedev.stackexchange.com/questions/70546/problem-calculating-normals-for-heightmaps
            var dx = zoneHeightmap[localBlockPos.X, localBlockPos.Z] - zoneHeightmap[localBlockPos.X + 1, localBlockPos.Z + 1];
            var dz = zoneHeightmap[localBlockPos.X, localBlockPos.Z + 1] - zoneHeightmap[localBlockPos.X + 1, localBlockPos.Z];
            var normal = new Vector3(dx, NormalY, -dz);
            normal = NormalCorrectRotation*normal;
            return Vector3.Normalize(normal);
        }

        private void CalculateVertex(Vector2i vertPos, ZoneRatio[,] zoneInfluences, float[,] zoneHeightmap, ZoneLayout zone)
        {
            var localPos = vertPos - zone.Bounds.Min;

            var height = 0f;
            if (zoneInfluences[localPos.X, localPos.Z].IsEmpty)
            {
                zoneInfluences[localPos.X, localPos.Z] = Land.GetInfluence((Vector2) vertPos);
                //var settings = Land.GetZoneNoiseSettings(zoneInfluences[localPos.X, localPos.Z]);
            }

            foreach (var inf in zoneInfluences[localPos.X, localPos.Z])
            {
                ZoneGenerator generator;
                if (!_generators.TryGetValue(inf.Zone, out generator))
                {
                    if (inf.Zone == _type)
                    {
                        generator = this;
                        _generators.Add(_type, this);
                    }
                    else
                    {
                        generator = Create(inf.Zone, _zone, Land, _landSettings);
                        _generators.Add(inf.Zone, generator);
                    }
                }

                var generateBaseHeight = generator.GenerateBaseHeight(vertPos.X, vertPos.Z,
                    zoneInfluences[localPos.X, localPos.Z]);

                height += generateBaseHeight * inf.Value;
            }

            zoneHeightmap[localPos.X, localPos.Z] = height;
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
