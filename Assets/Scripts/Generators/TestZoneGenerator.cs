using System;
using System.Collections.Generic;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Generators
{
    public class TestZoneGenerator : BaseZoneGenerator
    {
        public TestZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd);
            _generator = new FastNoise(unchecked(settings.Seed + id));
            _generator.SetFrequency(1);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            if (Zone.Biome.Type == BiomeType.TestOnlyMacroHigh)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(20, 0, 0);
                }
            }
            else if (Zone.Biome.Type == BiomeType.TestPit)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(-10, -20, -20);
                }
            }
            else if (Zone.Biome.Type == BiomeType.TestBulge)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(20, 10, 10);
                }
            }
            else if(Zone.Biome.Type == BiomeType.TestBaseOreAndGround)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(0, -9, -10);
                }
            }
            else if (Zone.Biome.Type == BiomeType.TestBaseCavesAndGround)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(0, -5, -10);
                }
            }
            else
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(0, 0, 0);
                }
            }

            return Zone;
        }

        public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            var blocks = base.GenerateBlock2(position, macroHeight);

            if (Zone.Biome.Type == BiomeType.TestWaves)
            {
                var height = Mathf.Sin(position.X / 5f) * 5;
                var heights = new Heights(macroHeight.Layer1Height + height, macroHeight.UndergroundHeight + height, macroHeight.BaseHeight + height);
                blocks = blocks.MutateHeight(heights);
            }
            else if (Zone.Biome.Type == BiomeType.TestBaseOreAndGround)
            {
                var baseHeight = _generator.GetSimplex(position.X / 20f, position.Z / 20f) * 3 + macroHeight.BaseHeight;
                var resourcesHeight = Interpolation.SmoothestStep(Mathf.Clamp01((float)_generator.GetSimplex(position.X / 10f, position.Z / 10f))) * 10 + macroHeight.UndergroundHeight - 1;
                var groundHeight = Interpolation.SmoothestStep((_generator.GetSimplex(position.Z / 40f, position.X / 40f) + 1) / 2)
                                   * 10 - 12 + macroHeight.Layer1Height;

                var underground = resourcesHeight > baseHeight ? BlockType.GoldOre : BlockType.Empty;
                BlockType ground;
                if (groundHeight > resourcesHeight && groundHeight > baseHeight)
                    ground = BlockType.Grass;
                else
                    ground = BlockType.Empty;

                blocks = new Blocks(ground, underground, new Heights((float)groundHeight, resourcesHeight, (float)baseHeight));
            }
            else if(Zone.Biome.Type == BiomeType.TestBaseCavesAndGround)
            {
                //"Hourglass" shaped hole to the base layer
                //
                //-\        /---
                //  \      /
                //   \    /
                //   /    \
                //  /      \
                //-/--------\--
                var distance = Vector2.Distance(Vector2.Zero, position);
                var baseHeight = -10;
                var underHeight = distance < 10 ? -10 + (10 - distance) : -10;
                var groundHeight = distance < 10 ? -10 + distance : 0;


                /*
                var baseHeight = _generator.GetSimplex(position.X / 30f, position.Z / 30f) * 3 + macroHeight.BaseHeight;
                var undergroundHeight = (_generator.GetSimplex(position.X / 20f, position.Z / 20f) + 1);
                undergroundHeight = Math.Pow(undergroundHeight, 4) + macroHeight.UndergroundHeight;
                var groundHeight = _generator.GetSimplex(position.X / 5f, position.Z / 5f) + macroHeight.Layer1Height;
                */

                BlockType underground;
                if (underHeight > groundHeight || underHeight <= baseHeight)
                    underground = BlockType.Empty;
                else
                    underground = BlockType.Cave;

                BlockType ground = groundHeight >= underHeight ? BlockType.Grass : BlockType.Empty;

                blocks = new Blocks(ground, underground, new Heights(groundHeight, underHeight, baseHeight));
            }
            else if (Zone.Biome.Type == BiomeType.TestBaseOreGroundColumn)
            {
                //Column made from ground and ore, to test very steep slopes (vertical)

                //Column has half-circle form
                if (position.X < 0 && Vector2i.Distance(position, Vector2i.Zero) < 5)
                {
                    //All blocks the same
                    //blocks = new Blocks(BlockType.Grass, BlockType.GoldOre, new Heights(10, 5, -5));

                    //All block has different heights
                    var baseHeight = _random.Range(0, 5);
                    var underHeight = baseHeight + _random.Range(0, 5);
                    var groundHeight = underHeight + _random.Range(0, 5);

                    blocks = new Blocks(BlockType.Grass, BlockType.Cave, 
                        new Heights(groundHeight, underHeight, baseHeight));

                }
                else
                {
                    blocks = new Blocks(BlockType.Empty, BlockType.Empty, new Heights(-10, -10, -10));
                }
            }

            return blocks;
        }

        private readonly FastNoise _generator;
    }
}
