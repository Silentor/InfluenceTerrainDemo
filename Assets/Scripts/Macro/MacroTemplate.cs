using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Generators;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = TerrainDemo.Tools.Random;

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
            var additionalGeneratorsCache = new List<TriZoneGenerator> ();

            foreach (var cell in zone.Cells)
            {
                var microcell = outputMap.Cells.First(c => c.Macro == cell);

                //Prepare buffers for microcell
                var bufferSize = microcell.Bounds;
                var cellMixBuffer = new CellMixVertex[bufferSize.Size.X + 1, bufferSize.Size.Z + 1];
                var heightBuffer = new List<MicroHeight>(microcell.VertexPositions.Length);

                foreach (var vertexPosition in microcell.VertexPositions)
                {
                    //Prepare cell mix buffer
                    var localPos = vertexPosition - bufferSize.Min;
                    var cellMixVertex = new CellMixVertex()
                    {
                        Influence = inputMap.GetInfluence(vertexPosition),
                        MacroHeight = inputMap.GetHeight(vertexPosition),
                    };
                    cellMixBuffer[localPos.X, localPos.Z] = cellMixVertex;

                    //Calculate heights for all influencing zones
                    cellMixVertex.MicroHeights = new MicroHeight[cellMixVertex.Influence.Count];
                    for (int i = 0; i < cellMixVertex.Influence.Count; i++)
                    {
                        var infl = cellMixVertex.Influence.GetInfluence(i);

                        TriZoneGenerator generator;
                        if (infl.Item1 == zone.Id)
                            generator = mainGenerator;
                        else
                        {
                            generator = additionalGeneratorsCache.Find(g => g.Zone.Id == infl.Item1);
                            if (generator == null)
                            {
                                generator = inputMap.Generators.Find(g => g.Zone.Id == infl.Item1);
                                additionalGeneratorsCache.Add(generator);
                            }
                        }

                        Assert.IsNotNull(generator);

                        //var weight = Math.Tan(1.40948538956 * infl.Item2 - 1.56091059484) + 1.15259329272;
                        //var weight = -100.010204082 * Math.Pow(0.000102030405061, infl.Item2) + 0.0102040816327;
                        var weight = infl.Item2;

                        //Debug.Log($"{infl.Item2} = {weight}");

                        var microHeight = generator.GetMicroHeight(vertexPosition, cellMixVertex.MacroHeight);
                        //if (microHeight > maxHeight)
                        //{
                        //    maxHeight = microHeight;
                        //    maxHeightZone = infl.Item1;
                        //}

                        cellMixVertex.MicroHeights[i] = microHeight;

                    }

                    //cellMixVertex.HighestZone = maxHeightZone;

                    //Calculate result heights. Простейший способ - лерп высот на основании инфлюенса
                    var resultMicroHeight = CombineSimple(cellMixVertex.Influence, cellMixVertex.MicroHeights);
                    //var resultMicroHeight = Combine(cellMixVertex.Influence, cellMixVertex.MicroHeights);
                    cellMixVertex.ResultHeight = resultMicroHeight;
                    heightBuffer.Add(resultMicroHeight);

                    //Способ интереснее - выигрывает самая высокая вершина после применения веса инфлюенса,
                    //тогда можно будет управлять, как поверхности накрывают друг друга
                    //heightBuffer.Add(cellMixVertex.MicroHeights.Max() + cellMixVertex.MacroHeight);

                    _mixBufferDebug[vertexPosition] = cellMixVertex;
                }

                outputMap.SetHeights(microcell.VertexPositions, heightBuffer);

                //Вычислить высоты в 4х углах для каждой из влияющих зон
                //Вычислить самые высокие высоты, от них зависит, какая из влияющих зон создаст данный блок земли
                //Создать вычисленный блок земли с усреднёнными высотами

                var blocksBuffer = new List<Blocks>(microcell.BlockPositions.Length);
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var localPos = blockPosition - bufferSize.Min;

                    //Calculate what zone influenced most to heightmap for given block
                    //var zoneIdOfBlock = Zone.InvalidId;

                    var zoneIdOfBlock = MicroHeight.GetMostInfluencedZone(
                        cellMixBuffer[localPos.X, localPos.Z].ResultHeight,
                        cellMixBuffer[localPos.X + 1, localPos.Z].ResultHeight,
                        cellMixBuffer[localPos.X, localPos.Z + 1].ResultHeight,
                        cellMixBuffer[localPos.X + 1, localPos.Z + 1].ResultHeight);

                    //var randomZone = _random.Range(0, 3);
                    //if (randomZone == 0)
                    //    zoneIdOfBlock = cellMixBuffer[localPos.X, localPos.Z].HighestZone;
                    //else if (randomZone == 1)
                    //    zoneIdOfBlock = cellMixBuffer[localPos.X + 1, localPos.Z].HighestZone;
                    //else if (randomZone == 2)
                    //    zoneIdOfBlock = cellMixBuffer[localPos.X, localPos.Z + 1].HighestZone;
                    //else if (randomZone == 3)
                    //    zoneIdOfBlock = cellMixBuffer[localPos.X + 1, localPos.Z + 1].HighestZone;

                    TriZoneGenerator generator;
                    if (zoneIdOfBlock.Item1 == zone.Id)
                        generator = mainGenerator;
                    else
                        generator = additionalGeneratorsCache.Find(g => g.Zone.Id == zoneIdOfBlock.Item1);

                    Assert.IsNotNull(generator);

                    var block = generator.GetBlocks(blockPosition);
                    if (zoneIdOfBlock.Item2 == BlockLayer.Base)
                        block.Layer1 = BlockType.Empty;
                    blocksBuffer.Add(block);
                }

                outputMap.SetBlocks(microcell.BlockPositions, blocksBuffer);
            }
            
        }

        /*
        public TriMesh GenerateLayout(TriMesh mesh, TriRunner settings)
        {

        }
        */

        /// <summary>
        /// Divide mesh to clustered zones completely random
        /// </summary>
        /// <param name="map"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IEnumerable<TriZoneGenerator> RandomClusterZonesDivider(MacroMap map, TriRunner settings)
        {
            var zones = new List<TriZoneGenerator>();
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

        private TriZoneGenerator GetZoneGenerator(BiomeSettings biome, MacroMap map, IEnumerable<Cell> cells, int zoneId, TriRunner settings)
        {
            if(biome.Type == BiomeType.Mountain)
                return new MountainsGenerator(map, cells, zoneId, biome, settings);
            else if(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd)
                return new TestZoneGenerator(map, cells, zoneId, biome, settings);
            else if(biome.Type == BiomeType.Desert)
                return new DesertGenerator(map, cells, zoneId, biome, settings);
            else
                return new TriZoneGenerator(map, cells, zoneId, biome, settings);
        }

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

        /// <summary>
        /// Combine influenced heights for given vertex
        /// </summary>
        /// <param name="influnce"></param>
        /// <param name="heights"></param>
        /// <returns></returns>
        private static MicroHeight CombineSimple(Influence influnce, params MicroHeight[] heights)
        {
            Assert.IsTrue(influnce.Count == heights.Length);

            //Fast pass
            //if (heights.Length == 1)
            //return new MicroHeight(heights[0].BaseHeight, heights[0].Layer1Height, heights[0].ZoneId);      //Be sure to disable Additional layer flag

            float baseResult = 0;
            float layer1Result = 0;
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.IsTrue(heights[i].ZoneId == influnce.GetZone(i));

                var weight = influnce.GetWeight(i);
                var baseHeight = heights[i].BaseHeight * weight;
                var layer1Height = heights[i].Layer1Height * weight;

                baseResult += baseHeight;
                layer1Result += layer1Height;
            }

            return new MicroHeight(baseResult, layer1Result, influnce.GetMostInfluenceZone());
        }

        /// <summary>
        /// Helper data type to mix different zones in given vertex
        /// </summary>
        public class CellMixVertex
        {
            public Influence Influence;
            public float MacroHeight;
            public MicroHeight[] MicroHeights;   //As in Influence
            public int HighestZone;
            public MicroHeight ResultHeight;
        }
    }
}
