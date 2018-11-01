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
                    cell.Height = UnityEngine.Random.Range(20, 30);
                }
            }
            else
            {
                foreach (var cell in Zone.Cells)
                {
                    cell.Height = 0;
                }
            }

            return Zone;
        }

        public override MicroHeight GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            if (Zone.Biome.Type == BiomeType.TestBulge)
                return new MicroHeight(vertex.MacroHeight, vertex.MacroHeight + 10);
            else if (Zone.Biome.Type == BiomeType.TestPit)
                return new MicroHeight(vertex.MacroHeight - 10, vertex.MacroHeight - 10);
            else if(Zone.Biome.Type == BiomeType.TestFlat)
                return new MicroHeight(vertex.MacroHeight, vertex.MacroHeight);
            else if (Zone.Biome.Type == BiomeType.TestOnlyMacroLow || Zone.Biome.Type == BiomeType.TestOnlyMacroHigh)
                return new MicroHeight(vertex.MacroHeight, vertex.MacroHeight);

            else
                throw new NotImplementedException();
        }
    }
}
