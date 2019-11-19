using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Generators;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector2i = TerrainDemo.Spatial.Vector2i;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Это типа LandGenerator?
    /// </summary>
    public class MacroTemplate
    {
        private readonly Random _random;
        private Dictionary<Vector2i, CellMixVertex> _mixBufferDebug = new Dictionary<Vector2i, CellMixVertex>();

        public MacroTemplate(Random random)
        {
            _random = random;
        }

        public MacroMap CreateMacroMap(TriRunner settings)
        {
            var timer = Stopwatch.StartNew();

            var result = new MacroMap(settings, _random);
            var zoneGenerators = RandomClusterZonesDivider(result, settings);

            foreach (var generator in zoneGenerators)
            {
                result.Generators.Add(generator);
                var zone = generator.GenerateMacroZone();
                result.Zones.Add(zone);
            }

            timer.Stop();

            Debug.LogFormat("Created macromap in {0} msec", timer.ElapsedMilliseconds);

            CheckMacroHeightFunctionQuality(result);

            return result;
        }

        /*
        /// <summary>
        /// Generate micro-presentation for given macro zone and place it to Micromap
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="outputMap"></param>
        public void GenerateMicroZone(MacroMap inputMap, Zone zone, MicroMap outputMap)
        {
            //Get generator for this zone
            var mainGenerator = inputMap.Generators.Find(g => g.Zone == zone);
            var additionalGeneratorsCache = new List<BaseZoneGenerator> ();

            foreach (var cell in zone.Cells)
            {
                var microcell = outputMap.Cells.First(c => c.Macro == cell);

                //Prepare buffers for microcell
                var blockMixBuffer = new Blocks[Influence.Capacity];
                var resultBlock = default(Blocks);
                var blockBuffer2 = new List<Blocks>(microcell.BlockPositions.Length);

                //NEW logic 2
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var blockCenterPosition = BlockInfo.GetWorldCenter(blockPosition);
                    var influence = inputMap.GetInfluence(blockCenterPosition);
                    var macroHeight = inputMap.GetHeight(blockCenterPosition);

                    //Get blocks from all influencing zones
                    for (int i = 0; i < influence.Count; i++)
                    {
                        var infl = influence.GetInfluence(i);

                        BaseZoneGenerator generator;
                        if (infl.ZoneId == zone.Id)
                            generator = mainGenerator;
                        else
                        {
                            generator = additionalGeneratorsCache.Find(g => g.Zone.Id == infl.ZoneId);
                            if (generator == null)
                            {
                                generator = inputMap.Generators.Find(g => g.Zone.Id == infl.ZoneId);
                                additionalGeneratorsCache.Add(generator);
                            }
                        }

                        Assert.IsNotNull(generator);

                        blockMixBuffer[i] = generator.GenerateBlock2(blockPosition, macroHeight);
                        if (infl.ZoneId == influence.GetMostInfluenceZone())
                            resultBlock = blockMixBuffer[i];
                    }

                    //Mix several blocks
                    if (influence.Count > 1)
                    {
                        //Common mix algorithm
                        {
                            var accum = Vector3d.Zero;
                            for (int i = 0; i < influence.Count; i++)
                            {
                                accum = accum + ((Vector3d) blockMixBuffer[i].Height) * influence.GetWeight(i);
                            }

                            var resultHeights = (Heights) accum;
                            resultBlock = resultBlock.MutateHeight(resultHeights);
                        }
                    }
                    
                    blockBuffer2.Add(resultBlock);
                }

                outputMap.SetBlocks(microcell.BlockPositions, blockBuffer2, false);
            }
            
        }
        */

        /// <summary>
        /// Generate micro-presentation for given macro zone and place it to Micromap
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="outputMap"></param>
        public void GenerateMicroZone2(MacroMap inputMap, Zone zone, MicroMap outputMap)
        {
            //Get generator for this zone
            var mainGenerator = inputMap.Generators.Find(g => g.Zone == zone);
            var additionalGeneratorsCache = new List<BaseZoneGenerator>();

            foreach (var macroCell in zone.Cells)
            {
                var microcell = outputMap.Cells.First(c => c.Macro == macroCell);

                //Prepare buffers for microcell
                var bufferSize = microcell.Bounds;
                var heightBuffer = new Heights[bufferSize.Size.X + 1, bufferSize.Size.Z + 1];
                var heightBuffer2 = new List<Heights>();
                var influenceBuffer = new Influence[bufferSize.Size.X + 1, bufferSize.Size.Z + 1];      //Shared block/vertex influence buffer
                var blockBuffer2 = new List<Blocks>(microcell.BlockPositions.Bound.Area);

                mainGenerator.BeginCellGeneration(microcell);

                //First run - generate heights
                foreach (var vertexPos in microcell.VertexPositions)
                {
                    var heightMixBuffer = Vector3.Zero;
                    var localPos = World2Local( vertexPos, microcell.Bounds);
                    var influence = inputMap.GetInfluence(vertexPos);
                    var macroHeight = inputMap.GetHeight(vertexPos);

                    //Collect height data from all influenced zones
                    for (int i = 0; i < influence.Count; i++)
                    {
                        var (zoneId, weight) = influence.GetInfluence(i);

                        BaseZoneGenerator generator;
                        if (zoneId == zone.Id)
                            generator = mainGenerator;
                        else
                        {
                            generator = additionalGeneratorsCache.Find(g => g.Zone.Id == zoneId);
                            if (generator == null)
                            {
                                generator = inputMap.Generators.Find(g => g.Zone.Id == zoneId);
                                additionalGeneratorsCache.Add(generator);
                            }
                        }

                        Assert.IsNotNull(generator);

                        heightMixBuffer += (Vector3)generator.GenerateHeight(vertexPos, macroHeight) * weight;
                    }

                    influenceBuffer[localPos.X, localPos.Z] = influence;
                    var height = (Heights)heightMixBuffer;
                    heightBuffer[localPos.X, localPos.Z] = height;
                    heightBuffer2.Add(height);
                }

                //Second run - generate blocks
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var localPos = blockPosition - microcell.Bounds.Min;
                    
                    //Get block from main zone (but in the future also analyze second influenced zone and mix blocks)
                    ref readonly var v00 = ref heightBuffer[localPos.X, localPos.Z];
                    ref readonly var v01 = ref heightBuffer[localPos.X, localPos.Z + 1];
                    ref readonly var v10 = ref heightBuffer[localPos.X + 1, localPos.Z];
                    ref readonly var v11 = ref heightBuffer[localPos.X + 1, localPos.Z + 1];

                    var blockType = mainGenerator.GenerateBlock3(blockPosition, v00, v01, v10, v11);
                    var main = blockType.Ground;
                    var under = blockType.Underground;

                    //todo validate block layers and corner heights
                    if (!v00.IsMainLayerPresent && !v01.IsMainLayerPresent && !v10.IsMainLayerPresent &&
                        !v11.IsMainLayerPresent)
                        main = BlockType.Empty;

                    if (!v00.IsUndergroundLayerPresent && !v01.IsUndergroundLayerPresent && !v10.IsUndergroundLayerPresent &&
                        !v11.IsUndergroundLayerPresent)
                        under = BlockType.Empty;

                    blockBuffer2.Add(new Blocks(main, under, blockType.Obstacle));
                }

                mainGenerator.EndCellGeneration(microcell);

                outputMap.SetHeights(microcell.VertexPositions, heightBuffer2);
                outputMap.SetBlocks(microcell.BlockPositions, blockBuffer2, false);
            }
        }

        /// <summary>
        /// Divide mesh to clustered zones completely random
        /// </summary>
        /// <param name="map"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IEnumerable<BaseZoneGenerator> RandomClusterZonesDivider(MacroMap map, TriRunner settings)
        {
            var zones = new List<BaseZoneGenerator>();
            var zoneId = 0;
            foreach (var cell in map.Cells)
            {
                if (cell.ZoneId == Zone.InvalidId)
                {
                    var biome = _random.Item(settings.Biomes);
                    var zoneSize = _random.Range(biome.SizeRange);
                    var startCell = cell;
                    var zoneCells = map.FloodFill(startCell, c => c.ZoneId == Zone.InvalidId).Take(zoneSize).ToArray();
                    var zoneMesh = map.GetSubmesh(zoneCells);

                    foreach (var triCell in zoneCells)
                    {
                        triCell.ZoneId = zoneId;
                    }

                    zones.Add(GetZoneGenerator(biome, map, zoneCells, zoneId, settings));

                    zoneId++;
                }
            }

            //Assert.IsTrue(mesh.Cells.All(c => c.Id != Cell.InvalidZoneId));
            return zones;
        }

        private BaseZoneGenerator GetZoneGenerator(BiomeSettings biome, MacroMap map, IEnumerable<Cell> cells, int zoneId, TriRunner settings)
        {
            switch (biome.Type)
            {
                case BiomeType.Mountain:
                    return new MountainsGenerator(map, cells, zoneId, biome, settings);
                case BiomeType.Forest:
                    return new ForestGenerator(map, cells, zoneId, biome, settings);
                case BiomeType.Desert:
                    return new DesertGenerator(map, cells, zoneId, biome, settings);
                case BiomeType.Caves:
                    return new CavesGenerator(map, cells, zoneId, biome, settings);
                case BiomeType.TestNavigation:
                    return new NavigationTestGenerator(map, cells, zoneId, biome, settings);

                default:
                {
                    if(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd)
                        return new TestZoneGenerator(map, cells, zoneId, biome, settings);
                    else
                        return new BaseZoneGenerator(map, cells, zoneId, biome, settings);
                }
            }
        }

        /// <summary>
        /// Combine influenced heights for given vertex
        /// </summary>
        /// <param name="influnce"></param>
        /// <param name="heights"></param>
        /// <returns></returns>
        private static Heights CombineSimple(Influence influnce, params Heights[] heights)
        {
            Assert.IsTrue(influnce.Count == heights.Length);

            //Fast pass
            if (heights.Length == 1)
                return new Heights(heights[0].Main, heights[0].Underground, heights[0].Base);      //Be sure to disable Additional layer flag

            float baseResult = 0;
            float undergroundResult = 0;
            float layer1Result = 0;
            for (int i = 0; i < heights.Length; i++)
            {
                //Assert.IsTrue(heights[i].ZoneId == influnce.GetZone(i));

                var weight = influnce.GetWeight(i);
                var baseHeight = heights[i].Base * weight;
                var undergroundHeight = heights[i].Underground * weight;
                var layer1Height = heights[i].Main * weight;

                baseResult += baseHeight;
                undergroundResult += undergroundHeight;
                layer1Result += layer1Height;
            }

            return new Heights(layer1Result, undergroundResult, baseResult);
        }

        private void CheckMacroHeightFunctionQuality(MacroMap map)
        {
            //Estimate macro height function quality
            //Узнаем, на сколько отличается функция макровысоты от заданных высот ячеек
            double maxDiff = 0, averageDiff = 0;
            Cell maxDiffCell = null;
            foreach (var macroCell in map.Cells)
            {
                var heightDiff = (Vector3d)macroCell.DesiredHeight - (Vector3d)map.GetHeight(macroCell.Center);
                if (Math.Abs(maxDiff) < Math.Abs(heightDiff.X))
                {
                    maxDiffCell = macroCell;
                    maxDiff = heightDiff.X;
                }

                if (Math.Abs(maxDiff) < Math.Abs(heightDiff.Y))
                {
                    maxDiffCell = macroCell;
                    maxDiff = heightDiff.Y;
                }

                averageDiff = averageDiff + heightDiff.X + heightDiff.Y + heightDiff.Z;
            }

            averageDiff /= map.Cells.Count * Heights.LayersCount;

            Debug.LogFormat("Average diff {0}, max diff {1} on cell {2}", averageDiff, maxDiff, maxDiffCell?.HexPoses);
        }

        protected GridPos World2Local(GridPos worldPosition, Bounds2i bounds)
        {
	        return new GridPos(worldPosition.X - bounds.Min.X, worldPosition.Z - bounds.Min.Z);
        }

		/// <summary>
		/// Helper data type to mix different zones in given vertex
		/// </summary>
		public class CellMixVertex
        {
            //1) Prepare buffer
            public Influence Influence;
            public Heights MacroHeight;

            //2) Generate microheights for every influenced zone
            public Heights[] Heightses;   //As in Influence

            //3) Generate result microheight in given vertex mixing all influenced zone microheights
            public Heights ResultHeight;
        }
    }
}
