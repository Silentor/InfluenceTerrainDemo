using System;
using System.Collections.Generic;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Tri;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;

namespace TerrainDemo.Generators
{
    public class TestZoneGenerator : BaseZoneGenerator
    {
        public TestZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            if (Zone.Biome.Type == BiomeType.TestOnlyMacroHigh)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(0, 20);
                }
            }
            if (Zone.Biome.Type == BiomeType.TestPit)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(-20, -10);
                }
            }
            if (Zone.Biome.Type == BiomeType.TestBulge)
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(10, 20);
                }
            }
            else
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.DesiredHeight = new Heights(0, 0);
                }
            }

            return Zone;
        }

        public override Heights GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            if (Zone.Biome.Type == BiomeType.TestBulge)
                return new Heights(vertex.MacroHeight.BaseHeight, vertex.MacroHeight.Layer1Height);
            else if (Zone.Biome.Type == BiomeType.TestPit)
                return new Heights(vertex.MacroHeight.BaseHeight, vertex.MacroHeight.Layer1Height);
            else if(Zone.Biome.Type == BiomeType.TestFlat)
                return new Heights(vertex.MacroHeight.BaseHeight, vertex.MacroHeight.Layer1Height);
            else if (Zone.Biome.Type == BiomeType.TestOnlyMacroLow || Zone.Biome.Type == BiomeType.TestOnlyMacroHigh)
                return new Heights(vertex.MacroHeight.BaseHeight, vertex.MacroHeight.Layer1Height);

            else
                throw new NotImplementedException();
        }
    }
}
