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
using Vector2 = OpenTK.Vector2;

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
            var additionalGeneratorsCache = new List<BaseZoneGenerator> ();

            foreach (var cell in zone.Cells)
            {
                var microcell = outputMap.Cells.First(c => c.Macro == cell);

                //Prepare buffers for microcell
                var bufferSize = microcell.Bounds;
                var cellMixBuffer = new CellMixVertex[bufferSize.Size.X + 1, bufferSize.Size.Z + 1];
                var heightBuffer = new List<MicroHeight>(microcell.VertexPositions.Length);

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

                //Calculate normals based on macro height
                foreach (var vertexPosition in microcell.VertexPositions)
                {
                    var localPos = vertexPosition - bufferSize.Min;
                    var cellMixVertex = cellMixBuffer[localPos.X, localPos.Z];

                    cellMixVertex.Normal = CalculateNormal(cellMixBuffer, localPos);
                }

                //Calculate micro heightmap
                foreach (var vertexPosition in microcell.VertexPositions)
                {
                    var localPos = vertexPosition - bufferSize.Min;
                    var cellMixVertex = cellMixBuffer[localPos.X, localPos.Z];

                    cellMixVertex.MicroHeights = new MicroHeight[cellMixVertex.Influence.Count];
                    for (var i = 0; i < cellMixVertex.Influence.Count; i++)
                    {
                        var infl = cellMixVertex.Influence.GetInfluence(i);

                        BaseZoneGenerator generator;
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

                        var microHeight = generator.GetMicroHeight(vertexPosition, cellMixVertex);
                        cellMixVertex.MicroHeights[i] = microHeight;
                    }

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

                //Generate blocks
                var blocksBuffer = new List<Blocks>(microcell.BlockPositions.Length);
                foreach (var blockPosition in microcell.BlockPositions)
                {
                    var localPos = blockPosition - bufferSize.Min;

                    var zoneGeneratorId = cellMixBuffer[localPos.X, localPos.Z].Influence.GetMostInfluenceZone();
                    var isMainLayerPresent = cellMixBuffer[localPos.X, localPos.Z].ResultHeight.IsLayer1Present
                                             || cellMixBuffer[localPos.X + 1, localPos.Z].ResultHeight.IsLayer1Present
                                             || cellMixBuffer[localPos.X, localPos.Z + 1].ResultHeight.IsLayer1Present
                                             || cellMixBuffer[localPos.X + 1, localPos.Z + 1].ResultHeight.IsLayer1Present;

                    BaseZoneGenerator generator;
                    if (zoneGeneratorId == zone.Id)
                        generator = mainGenerator;
                    else
                        generator = additionalGeneratorsCache.Find(g => g.Zone.Id == zoneGeneratorId);

                    Assert.IsNotNull(generator);

                    var blockNormal = cellMixBuffer[localPos.X, localPos.Z].Normal
                                      + cellMixBuffer[localPos.X + 1, localPos.Z].Normal
                                      + cellMixBuffer[localPos.X, localPos.Z + 1].Normal
                                      + cellMixBuffer[localPos.X + 1, localPos.Z + 1].Normal;
                    blockNormal = (blockNormal / 4).normalized;
                    var block = generator.GetBlocks(blockPosition, blockNormal);
                    block.Normal = blockNormal;
                    if (!isMainLayerPresent)
                        block.Layer1 = BlockType.Empty;
                    blocksBuffer.Add(block);
                }

                outputMap.SetBlocks(microcell.BlockPositions, blocksBuffer);
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
            if(biome.Type == BiomeType.Mountain)
                return new MountainsGenerator(map, cells, zoneId, biome, settings);
            else if(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd)
                return new TestZoneGenerator(map, cells, zoneId, biome, settings);
            else if(biome.Type == BiomeType.Desert)
                return new DesertGenerator(map, cells, zoneId, biome, settings);
            else
                return new BaseZoneGenerator(map, cells, zoneId, biome, settings);
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
        private static MicroHeight CombineSimple(Influence influnce, params MicroHeight[] heights)
        {
            Assert.IsTrue(influnce.Count == heights.Length);

            //Fast pass
            if (heights.Length == 1)
                return new MicroHeight(heights[0].BaseHeight, heights[0].Layer1Height);      //Be sure to disable Additional layer flag

            float baseResult = 0;
            float layer1Result = 0;
            for (int i = 0; i < heights.Length; i++)
            {
                //Assert.IsTrue(heights[i].ZoneId == influnce.GetZone(i));

                var weight = influnce.GetWeight(i);
                var baseHeight = heights[i].BaseHeight * weight;
                var layer1Height = heights[i].Layer1Height * weight;

                baseResult += baseHeight;
                layer1Result += layer1Height;
            }

            return new MicroHeight(baseResult, layer1Result);
        }

        /// <summary>
        /// based on http://www.flipcode.com/archives/Calculating_Vertex_Normals_for_Height_Maps.shtml
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        private static Vector3 CalculateNormal(CellMixVertex[,] buffer, Vector2i localPosition)
        {
            var currentVertex = buffer[localPosition.X, localPosition.Z];

            var x0 = buffer[localPosition.X < buffer.GetLength(0) - 1 ? localPosition.X + 1 : localPosition.X, localPosition.Z];
            if (x0 == null)
                x0 = currentVertex;
            var x1 = buffer[localPosition.X > 0 ? localPosition.X - 1 : localPosition.X, localPosition.Z];
            if (x1 == null)
                x1 = currentVertex;
            var sx = x0.MacroHeight - x1.MacroHeight;
            if (x0 == currentVertex || x1 == currentVertex)
                sx *= 2;

            var z0 = buffer[localPosition.X, localPosition.Z < buffer.GetLength(1) - 1 ? localPosition.Z + 1 : localPosition.Z];
            if (z0 == null)
                z0 = currentVertex;
            var z1 = buffer[localPosition.X, localPosition.Z > 0 ? localPosition.Z - 1 : localPosition.Z];
            if (z1 == null)
                z1 = currentVertex;
            var sz = z0.MacroHeight - z1.MacroHeight;
            if (z0 == currentVertex || z1 == currentVertex)
                sz *= 2;

            var result = new Vector3(-sx, 2, sz);
            return result.normalized;
        }

        /// <summary>
        /// Helper data type to mix different zones in given vertex
        /// </summary>
        public class CellMixVertex
        {
            //1) Prepare buffer
            public Influence Influence;
            public float MacroHeight;

            //2) Generate macro height normals
            public Vector3 Normal;

            //3) Generate microheights for every influenced zone
            public MicroHeight[] MicroHeights;   //As in Influence

            //4) Generate result microheight in given vertex mixing all influenced zone microheights
            public MicroHeight ResultHeight;
        }
    }
}
