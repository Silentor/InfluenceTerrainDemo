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

            //Set no height defined state
            for (int x = 0; x < zoneHeightmap.GetLength(0); x++)
                for (int z = 0; z < zoneHeightmap.GetLength(1); z++)
                    zoneHeightmap[x, z] = NoHeight;

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
                var blockInfluence = zoneInfluences[localPos.X, localPos.Z];
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
                                Land.GetRBFInfluence((Vector2)worldTurbulated);}
                }
                else
                {
                    var worldTurbulated = blockPos + new Vector2i(turbulenceX, turbulenceZ);
                    blockInfluence = Land.GetRBFInfluence((Vector2)worldTurbulated);
                }
                */

                zoneBlocks[localPos.X, localPos.Z] = GenerateBlock(blockPos, new Vector2i(turbulenceX, turbulenceZ), 
                    zoneNormalmap[localPos.X, localPos.Z], blockInfluence);
                
            }

            var props = DecorateZone(zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks);

            return new ZoneContent(_zone, zoneInfluences, zoneHeightmap, zoneNormalmap, zoneBlocks, props);
        }

        public static ZoneGenerator Create(ZoneLayout zone, [NotNull] LandLayout land, LandGenerator generator, [NotNull] LandSettings landSettings)
        {
            return Create(zone.Type, zone, land, generator, landSettings);
        }

        /// <summary>
        /// Its possible to create generator of other zone type for given zone to build border (cross-zone) blocks
        /// </summary>
        /// <param name="type"></param>
        /// <param name="zone"></param>
        /// <param name="land"></param>
        /// <param name="landSettings"></param>
        /// <returns></returns>
        public static ZoneGenerator Create(ClusterType type, ZoneLayout zone, [NotNull] LandLayout land, LandGenerator generator, [NotNull] LandSettings landSettings)
        {
            switch (type)
            {
                case ClusterType.Hills:
                    return new HillsGenerator(zone, land, generator, landSettings);
                case ClusterType.Lake:
                        return new LakeGenerator(zone, land, generator, landSettings);
                case ClusterType.Forest:
                        return new ForestGenerator(zone, land, generator, landSettings);
                case ClusterType.Mountains:
                        return new MountainsGenerator(zone, land, generator, landSettings);
                case ClusterType.Foothills:
                        return new FoothillsGenerator(zone, land, generator, landSettings);
                case ClusterType.Snow:
                        return new SnowGenerator(zone, land, generator, landSettings);
                case ClusterType.Desert:
                    return new DefaultGenerator(zone, land, generator, landSettings);
                case ClusterType.Checkboard:
                        return new CheckboardGenerator(zone, land, generator, landSettings);
                case ClusterType.Cone:
                        return new ConeGenerator(zone, land, generator, landSettings);
                case ClusterType.Slope:
                        return new SlopeGenerator(zone, land, generator, landSettings);
                default:
                    {
                        if (type >= ClusterType.Influence1 && type <= ClusterType.Influence8)
                            return new FlatGenerator(zone, land, generator, landSettings);

                        throw new NotImplementedException(string.Format("Generator for zone type {0} is not implemented", type));
                    }
            }
        }

        private readonly ClusterType _type;
        protected readonly ZoneLayout _zone;
        protected readonly LandLayout Land;
        private readonly LandGenerator _generator;
        protected readonly LandSettings _landSettings;
        private ZoneRatio _influence;
        private static readonly float NormalY = 2*Mathf.Sqrt(3/2f);
        private static readonly Quaternion NormalCorrectRotation = Quaternion.Euler(0, -45, 0);
        private readonly Dictionary<ClusterType, ZoneGenerator> _generators = new Dictionary<ClusterType, ZoneGenerator>();
        protected ClusterSettings _clusterSettings;
        protected readonly FastNoise _noise;
        private const float NoHeight = -10000;

        protected ZoneGenerator(ClusterType type, ZoneLayout zone, [NotNull] LandLayout land, LandGenerator generator, [NotNull] LandSettings landSettings)
        {
            if (land == null) throw new ArgumentNullException("land");
            if (landSettings == null) throw new ArgumentNullException("landSettings");

            _type = type;
            _zone = zone;
            Land = land;
            _generator = generator;
            _landSettings = landSettings;
            _clusterSettings = landSettings[type];
            DefaultBlock = _clusterSettings.DefaultBlock;

            _noise = new FastNoise(landSettings.Seed);
            //_noise.SetFrequency(_clusterSettings.NoiseFreq);
            //_noise.SetFractalGain(_clusterSettings.NoiseGain);
            //_noise.SetFractalLacunarity(_clusterSettings.NoiseLacunarity);
            //_noise.SetFractalOctaves(_clusterSettings.NoiseOctaves);
            //_noise.SetFractalType(_clusterSettings.NoiseFractal);
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

        public virtual double GenerateBaseHeight(float worldX, float worldZ)
        {
            var yValue = 0d;

            //if (_clusterSettings.NoiseAmp > 0.001)
                //yValue = _noise.GetSimplexFractal(worldX, worldZ)*_clusterSettings.NoiseAmp;

            yValue += Land.GetBaseHeight(worldX, worldZ);

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

            //Already calculated
            if (zoneHeightmap[localPos.X, localPos.Z] > NoHeight)
                return;

            if (zoneInfluences[localPos.X, localPos.Z].IsEmpty)
            {
                zoneInfluences[localPos.X, localPos.Z] = Land.GetInfluence((Vector2) vertPos);
                //var settings = Land.GetZoneNoiseSettings(zoneInfluences[localPos.X, localPos.Z]);
            }

            //var height = NoHeight;
            var height = 0d;
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
                        generator = Create(inf.Zone, _zone, Land, _generator, _landSettings);
                        _generators.Add(inf.Zone, generator);
                    }
                }

                var generatedHeight = 0d;
                switch (_landSettings.HeightGeneration)         //Some debug height calculation algorithms and full generation
                {
                    case LandSettings.HeightGenerationType.NoHeight:
                        generatedHeight = 0;
                        break;

                    case LandSettings.HeightGenerationType.LandHeight:
                    {
                        generatedHeight = Land.GetBaseHeight(vertPos.X, vertPos.Z);
                        break;
                    }

                    case LandSettings.HeightGenerationType.FullHeight:
                        generatedHeight = generator.GenerateBaseHeight(vertPos.X, vertPos.Z);
                        break;
                }

                //if(generateBaseHeight > height)
                //height = generateBaseHeight;
                height += generatedHeight * inf.Value;
            }

            zoneHeightmap[localPos.X, localPos.Z] = (float)height;
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
