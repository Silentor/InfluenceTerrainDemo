using System;
using System.Collections.Generic;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;

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

        public override Macro.Zone GenerateMacroZone( MacroMap map )
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

        
        //public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        //{
        //    var block = base.GenerateBlock2(position, macroHeight);

        //    if (Zone.Biome.Type == BiomeType.TestWaves)
        //    {
        //        var height = Mathf.Sin(position.X / 5f) * 5;
        //        var heights = new Heights(macroHeight.Main + height, macroHeight.Underground + height, macroHeight.Base + height);
        //        block = block.MutateHeight(heights);
        //    }
        //    else if (Zone.Biome.Type == BiomeType.TestBaseOreAndGround)
        //    {
        //        var baseHeight = _generator.GetSimplex(position.X / 20f, position.Z / 20f) * 3 + macroHeight.Base;
        //        var resourcesHeight = Interpolation.SmoothestStep(Mathf.Clamp01((float)_generator.GetSimplex(position.X / 10f, position.Z / 10f))) * 10 + macroHeight.Underground - 1;
        //        var groundHeight = Interpolation.SmoothestStep((_generator.GetSimplex(position.Z / 40f, position.X / 40f) + 1) / 2) * 10 - 12 + macroHeight.Main;
        //        //var groundHeight = -1;

        //        //var underground = resourcesHeight > baseHeight ? BlockType.GoldOre : BlockType.Empty;
        //        var underground = BlockType.GoldOre;
        //        BlockType ground;
        //        /*
        //        if (groundHeight > resourcesHeight && groundHeight > baseHeight)
        //            ground = BlockType.Grass;
        //        else
        //            ground = BlockType.Empty;
        //            */
        //        ground = BlockType.Grass;

        //        block = new Blocks(ground, underground, new Heights((float)groundHeight, resourcesHeight, (float)baseHeight));
        //    }
        //    else if(Zone.Biome.Type == BiomeType.TestBaseCavesAndGround)
        //    {
        //        //"Hourglass" shaped hole to the base layer
        //        //
        //        //-\        /---
        //        //  \      /
        //        //   \    /
        //        //   /    \
        //        //  /      \
        //        //-/--------\--
        //        var distance = Vector2.Distance(Vector2.Zero, position);

        //        if (distance < 15)
        //        {
        //            var baseHeight = -10;
        //            var underHeight = distance < 10 ? -10 + (10 - distance) : -10;
        //            var groundHeight = distance < 10 ? -10 + distance : 0;

        //            /*
        //            var baseHeight = _generator.GetSimplex(position.X / 30f, position.Z / 30f) * 3 + macroHeight.BaseHeight;
        //            var undergroundHeight = (_generator.GetSimplex(position.X / 20f, position.Z / 20f) + 1);
        //            undergroundHeight = Math.Pow(undergroundHeight, 4) + macroHeight.UndergroundHeight;
        //            var groundHeight = _generator.GetSimplex(position.X / 5f, position.Z / 5f) + macroHeight.Layer1Height;
        //            */

        //            BlockType underground;
        //            if (underHeight > groundHeight || underHeight <= baseHeight)
        //                underground = BlockType.Empty;
        //            else
        //                underground = BlockType.GoldOre;

        //            BlockType ground = groundHeight >= underHeight ? BlockType.Grass : BlockType.Empty;

        //            block = new Blocks(ground, underground, new Heights(groundHeight, underHeight, baseHeight));
        //        }
        //        else
        //            block = Blocks.Empty;
        //    }
        //    else if (Zone.Biome.Type == BiomeType.TestBaseOreGroundColumn)
        //    {
        //        //Column made from ground and ore, to test very steep slopes (vertical)

        //        //Column has half-circle form
        //        if (position.X < 0 && Vector2i.Distance(position, Vector2i.Zero) < 5)
        //        {
        //            //All blocks the same
        //            //blocks = new Blocks(BlockType.Grass, BlockType.GoldOre, new Heights(10, 5, -5));

        //            //All block has different heights
        //            var baseHeight = _zoneRandom.Range(0, 5);
        //            var underHeight = baseHeight + _zoneRandom.Range(0, 5);
        //            var groundHeight = underHeight + _zoneRandom.Range(0, 5);

        //            block = new Blocks(BlockType.Grass, BlockType.GoldOre, 
        //                new Heights(groundHeight, underHeight, baseHeight));
        //        }
        //        else
        //        {
        //            block = new Blocks(BlockType.Empty, BlockType.Empty, new Heights(-10, -10, -10));
        //        }
        //    }
        //    else if (Zone.Biome.Type == BiomeType.TestBaseOrePyramidGround)
        //    {
        //        //Special test case
        //        if (position == (3, -21))
        //        {
        //            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block,
        //                new Heights(0, 1, -5));
        //        }

        //        if (position == (-15, -13) || position == (-15, -14))
        //        {
        //            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block,
        //                new Heights(0, 1, -5));
        //        }

        //        if (position == (-18, 12) || position == (-18, 13) || position == (-17, 13))
        //        {
        //            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block,
        //                new Heights(0, 1, -5));
        //        }

        //        if (position == (13, 14) || position == (13, 15) || position == (12, 14) || position == (12, 15))
        //        {
        //            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block,
        //                new Heights(0, 1, -5));
        //        }

        //        var distance = Vector2.Distance(Vector2.Zero, position);

        //        var baseHeight = -5f;
        //        var oreHeight = 7 - distance;
        //        var grassHeight = 0;

        //        block = new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block, 
        //            new Heights(grassHeight, oreHeight, baseHeight));
        //    }
        //    else
        //    {
        //        if (Zone.Biome.Type == BiomeType.TestTunnel)
        //        {
        //            //Tunnel is line defined by ax + by + c = 0
        //            const float a = 2f, b = -1f, c = 5f;
        //            const float tunnelWidth = 15;
        //            const float tunnelCenterHeight = 0;

        //            //Mountain defined as cone
        //            Vector2 mountCenter = Vector2.Zero;
        //            const float mountSize = 20;


        //            var distanceToTunnel = Mathf.Abs(a * position.X + b * position.Z + c) / Mathf.Sqrt(a * a + b * b);
        //            var distanceToMount = Vector2.Distance(position, mountCenter);
        //            var tunnelTurbulence = (float)(_generator.GetSimplex(position.X / 10d, position.Z / 10d) * 2 
        //                                   + _generator.GetSimplex(position.Z / 70d, position.X / 70d) * 5) ;

        //            //Make tunnel floor
        //            var baseHeight = 0f;
        //            if (distanceToTunnel < tunnelWidth)
        //                baseHeight += tunnelCenterHeight - Mathf.Sqrt(tunnelWidth * tunnelWidth - distanceToTunnel * distanceToTunnel) + tunnelTurbulence;

        //            var underHeight = 0f;
        //            if(distanceToTunnel < tunnelWidth)
        //                underHeight += tunnelCenterHeight + Mathf.Sqrt(tunnelWidth * tunnelWidth - distanceToTunnel * distanceToTunnel) + tunnelTurbulence;

        //            var groundHeight = 5f;
        //            if (distanceToMount < mountSize)
        //                groundHeight += mountSize - distanceToMount;
        //            block = new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block, 
        //                new Heights(groundHeight, underHeight, baseHeight));

        //        }
        //    }

        //    return block;
        //}

        private readonly FastNoise _generator;
    }
}
