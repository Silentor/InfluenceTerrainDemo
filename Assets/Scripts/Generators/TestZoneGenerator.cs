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
    public class TestZoneGenerator : TriZoneGenerator
    {
        public TestZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type >= BiomeType.TestBegin && biome.Type <= BiomeType.TestEnd);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                cell.Height = 0;
            }

            return Zone;
        }

        public override MicroHeight GetMicroHeight(Vector2 position, float macroHeight)
        {
            if (Zone.Biome.Type == BiomeType.TestBulge)
                return new MicroHeight(macroHeight, macroHeight + 10, Zone.Id);
            else if (Zone.Biome.Type == BiomeType.TestPit)
                return new MicroHeight(macroHeight - 10, macroHeight - 10, Zone.Id);
            else if(Zone.Biome.Type == BiomeType.TestFlat)
                return new MicroHeight(macroHeight, macroHeight, Zone.Id);
            else
                throw new NotImplementedException();
        }
    }
}
