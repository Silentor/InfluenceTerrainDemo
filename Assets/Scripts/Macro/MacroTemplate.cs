﻿using System;
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
using Vector3 = UnityEngine.Vector3;

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
                var bufferSize = microcell.Bounds;
                var cellMixBuffer = new CellMixVertex[bufferSize.Size.X + 1, bufferSize.Size.Z + 1];
                var heightBuffer = new List<Heights>(microcell.VertexPositions.Length);
                var blockBuffer = new (Blocks, Heights)[bufferSize.Size.X, bufferSize.Size.Z];
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
                        /*
                        if (blockMixBuffer.Any(b => b.Layer1 == BlockType.Sand))
                        {
                            //Special block mix - sand with another blocks
                            var sandInfluence = 0f;
                            for (int i = 0; i < influence.Count; i++)
                            {
                                if (blockMixBuffer[i].Layer1 == BlockType.Sand)
                                {
                                    sandInfluence += influence.GetWeight(i);
                                    //sandMicroHeight += (Vector3d) (blockMixBuffer[i].block.Heights);
                                }
                            }

                            for (int i = 0; i < influence.Count; i++)
                            {
                                if (blockMixBuffer[i].Layer1 == BlockType.Sand)
                                {
                                    var t = Mathf.Clamp01((sandInfluence - 0.5f) * 2);
                                    blockMixBuffer[i].Heights = new Heights(
                                        Mathf.Lerp(macroHeight.BaseHeight, blockMixBuffer[i].Heights.BaseHeight, t),
                                        Mathf.Lerp(macroHeight.UndergroundHeight, blockMixBuffer[i].Heights.UndergroundHeight,  t),
                                        Mathf.Lerp(macroHeight.Layer1Height, blockMixBuffer[i].Heights.Layer1Height, t)
                                        );
                                }*/
                                /*
                                else
                                {
                                    var t = Mathf.Clamp01(((1 - sandInfluence) - 0.5f) * 2);
                                    blockMixBuffer[i].Heights = new Heights(
                                        Mathf.Lerp(blockMixBuffer[i].Heights.BaseHeight, macroHeight.BaseHeight, t),
                                        Mathf.Lerp(blockMixBuffer[i].Heights.UndergroundHeight, macroHeight.UndergroundHeight, t),
                                        Mathf.Lerp(blockMixBuffer[i].Heights.Layer1Height, macroHeight.Layer1Height, t)
                                    );
                                }
                                */
                            /*}
                        }*/
                        

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

                /*
                //Prepare cell mix buffer
                    foreach (var vertexPosition in microcell.VertexPositions)
                {
                    var localPos = vertexPosition - bufferSize.Min;
                    var cellMixVertex = new CellMixVertex()
                    {
                        Influence = inputMap.GetInfluence(vertexPosition),
                        MacroHeight = inputMap.GetHeight(vertexPosition),
                    };
                    cellMixBuffer[localPos.X, localPos.Z] = cellMixVertex;
                }
                */

                /*
                //NEW logic
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var localPos = blockPosition - bufferSize.Min;
                    var cellMixVertex = cellMixBuffer[localPos.X, localPos.Z];
                    cellMixVertex.Heightses = new Heights[cellMixVertex.Influence.Count];
                    Blocks currentBlock;

                    //Generate block and 4 vertices for each influenced zones
                    for (int i = 0; i < cellMixVertex.Influence.Count; i++)
                    {
                        var infl = cellMixVertex.Influence.GetInfluence(i);

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

                        var block = generator.GenerateBlock(blockPosition,
                            in cellMixVertex.MacroHeight,
                            in cellMixBuffer[localPos.X, localPos.Z + 1].MacroHeight,
                            in cellMixBuffer[localPos.X + 1, localPos.Z].MacroHeight,
                            in cellMixBuffer[localPos.X + 1, localPos.Z + 1].MacroHeight,
                            ref cellMixBuffer[localPos.X, localPos.Z].Heightses[i],
                            ref cellMixBuffer[localPos.X, localPos.Z + 1].Heightses[i],
                            ref cellMixBuffer[localPos.X + 1, localPos.Z].Heightses[i],
                            ref cellMixBuffer[localPos.X + 1, localPos.Z + 1].Heightses[i]);

                        //TODO mix blocks, e.g. one zone places dirt, another places water, result block = mud
                        if (generator == mainGenerator)
                            currentBlock = block;
                    }
                }
                */

                /*
                //Calculate micro heightmap
                foreach (var vertexPosition in microcell.VertexPositions)
                {
                    var localPos = vertexPosition - bufferSize.Min;
                    var cellMixVertex = cellMixBuffer[localPos.X, localPos.Z];

                    cellMixVertex.Heightses = new Heights[cellMixVertex.Influence.Count];
                    for (var i = 0; i < cellMixVertex.Influence.Count; i++)
                    {
                        var infl = cellMixVertex.Influence.GetInfluence(i);

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

                        var microHeight = generator.GetMicroHeight(vertexPosition, cellMixVertex);
                        cellMixVertex.Heightses[i] = microHeight;
                    }

                    //Calculate result heights. Простейший способ - лерп высот на основании инфлюенса
                    var resultMicroHeight = CombineSimple(cellMixVertex.Influence, cellMixVertex.Heightses);
                    //var resultMicroHeight = Combine(cellMixVertex.Influence, cellMixVertex.MicroHeights);
                    cellMixVertex.ResultHeight = resultMicroHeight;
                    heightBuffer.Add(resultMicroHeight);

                    //Способ интереснее - выигрывает самая высокая вершина после применения веса инфлюенса,
                    //тогда можно будет управлять, как поверхности накрывают друг друга
                    //heightBuffer.Add(cellMixVertex.MicroHeights.Max() + cellMixVertex.MacroHeight);

                    _mixBufferDebug[vertexPosition] = cellMixVertex;
                }

                outputMap.SetHeights(microcell.VertexPositions, heightBuffer);

                //Generate blocks
                var blocksBuffer = new List<Blocks>(microcell.BlockPositions.Length);
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var localPos = blockPosition - bufferSize.Min;

                    var zoneGeneratorId = cellMixBuffer[localPos.X, localPos.Z].Influence.GetMostInfluenceZone();
                    


                    BaseZoneGenerator generator;
                    if (zoneGeneratorId == zone.Id)
                        generator = mainGenerator;
                    else
                        generator = additionalGeneratorsCache.Find(g => g.Zone.Id == zoneGeneratorId);

                    Assert.IsNotNull(generator);

                    var blockNormal = BlockInfo.GetBlockNormal(
                        cellMixBuffer[localPos.X, localPos.Z].MacroHeight.Nominal,
                        cellMixBuffer[localPos.X + 1, localPos.Z + 1].MacroHeight.Nominal,
                        cellMixBuffer[localPos.X, localPos.Z + 1].MacroHeight.Nominal,
                        cellMixBuffer[localPos.X + 1, localPos.Z].MacroHeight.Nominal
                    );
                                      
                    var block = generator.GetBlocks(blockPosition, blockNormal);
                    block.Normal = blockNormal;

                    //Fix blocks DEBUG

                    ref var v00 = ref cellMixBuffer[localPos.X, localPos.Z].ResultHeight;
                    ref var v11 = ref cellMixBuffer[localPos.X + 1, localPos.Z + 1].ResultHeight;
                    ref var v01 = ref cellMixBuffer[localPos.X, localPos.Z + 1].ResultHeight;
                    ref var v10 = ref cellMixBuffer[localPos.X + 1, localPos.Z].ResultHeight;

                    if (!v00.IsLayer1Present && !v01.IsLayer1Present && !v10.IsLayer1Present && !v11.IsLayer1Present)
                        block.Layer1 = BlockType.Empty;
                    if (!v00.IsUndergroundLayerPresent 
                        && !v01.IsUndergroundLayerPresent 
                        && !v10.IsUndergroundLayerPresent 
                        && !v11.IsUndergroundLayerPresent)
                        block.Underground = BlockType.Empty;

                    if (block.Underground == BlockType.Cave)
                    {
                        
                        //Fix blocks with visible "underground" vertices - cave entrances
                        if (v00.UndergroundHeight > v00.Layer1Height
                            || v01.UndergroundHeight > v01.Layer1Height
                            || v10.UndergroundHeight > v10.Layer1Height
                            || v11.UndergroundHeight > v11.Layer1Height)
                        {
                            if(v00.UndergroundHeight < v00.Layer1Height)
                            {
                                var edgeHeight = (v00.UndergroundHeight + v00.Layer1Height) / 2;
                                v00 = new Heights(v00.BaseHeight, edgeHeight, edgeHeight);
                            }

                            if (v01.UndergroundHeight < v01.Layer1Height)
                            {
                                var edgeHeight = (v01.UndergroundHeight + v01.Layer1Height) / 2;
                                v01 = new Heights(v01.BaseHeight, edgeHeight, edgeHeight);
                            }

                            if (v10.UndergroundHeight < v10.Layer1Height)
                            {
                                var edgeHeight = (v10.UndergroundHeight + v10.Layer1Height) / 2;
                                v10 = new Heights(v10.BaseHeight, edgeHeight, edgeHeight);
                            }

                            if (v11.UndergroundHeight < v11.Layer1Height)
                            {
                                var edgeHeight = (v11.UndergroundHeight + v11.Layer1Height) / 2;
                                v11 = new Heights(v11.BaseHeight, edgeHeight, edgeHeight);
                            }

                            block.Layer1 = BlockType.Empty;
                        }
                        
                    }

                    blocksBuffer.Add(block);
                }
                
                outputMap.SetBlocks(microcell.BlockPositions, blocksBuffer);
                */
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
                case BiomeType.Desert:
                    return new DesertGenerator(map, cells, zoneId, biome, settings);
                case BiomeType.Caves:
                    return new CavesGenerator(map, cells, zoneId, biome, settings);

                default:
                {
                    if(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd)
                        return new TestZoneGenerator(map, cells, zoneId, biome, settings);
                    else
                        return new BaseZoneGenerator(map, cells, zoneId, biome, settings);
                }
            }
        }

        /*
        /// <summary>
        /// Combine influenced heights for given vertex
        /// </summary>
        /// <param name="influnce"></param>
        /// <param name="heights"></param>
        /// <returns></returns>
        private static MicroHeight Combine(Influence influnce, params MicroHeight[] heights)
        {
            Assert.IsTrue(influnce.Count == heights.Length);

            //Fast pass
            //if (heights.Length == 1)
                //return new MicroHeight(heights[0].BaseHeight, heights[0].Layer1Height, heights[0].ZoneId);      //Be sure to disable Additional layer flag

            float maxHeight = float.MinValue;
            int maxHeightZoneId = Macro.Zone.InvalidId;

            float baseResult = 0;

            for (int i = 0; i < heights.Length; i++)
            {
                Assert.IsTrue(heights[i].ZoneId == influnce.GetZone(i));

                var baseHeight = heights[i].BaseHeight * influnce.GetWeight(i);

                //if (baseHeight > maxHeight)
                //{
                //    maxHeight = baseHeight;
                //    maxHeightZoneId = heights[i].ZoneId;
                //}
                baseResult += baseHeight;
            }

            float layer1Result = 0;
            float? layer2Result = null;

            for (int i = 0; i < heights.Length; i++)
            {
                //var layer1Height = heights[i].Layer1Height * influnce.GetWeight(i);

                var layerH = heights[i].Layer1Height - baseResult;
                var layer1Height = layerH * influnce.GetWeight(i);

                if (layer1Height + baseResult > maxHeight)
                {
                    maxHeight = layer1Height + baseResult;
                    maxHeightZoneId = heights[i].ZoneId;
                }

                if (heights[i].AdditionalLayer)
                {
                    if (layer2Result == null)
                        layer2Result = 0;
                    layer2Result += layer1Height;
                }
                else
                    layer1Result += layer1Height;
            }

            layer1Result += baseResult; //?

            return new MicroHeight(baseResult, layer2Result.HasValue ? Math.Max(layer2Result.Value, layer1Result) : layer1Result, maxHeightZoneId);
        }
        */

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

            Debug.LogFormat("Average diff {0}, max diff {1} on cell {2}", averageDiff, maxDiff, maxDiffCell?.Coords);
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
