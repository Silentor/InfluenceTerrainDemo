using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;

namespace TerrainDemo.Generators
{
    public class ForestGenerator : BaseZoneGenerator
    {
        public ForestGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Forest);

	        _globalZoneHeight = _zoneRandom.Range(1, 3);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                var baseHeight = _zoneRandom.Range(-12, -5) + _globalZoneHeight;
                cell.DesiredHeight = new Heights(_zoneRandom.Range(0, 5) + _globalZoneHeight, _zoneRandom.Range(baseHeight - 5, baseHeight + 1), baseHeight);
            }

            return Zone;
        }


        public override Heights GenerateHeight(GridPos position, in Heights macroHeight)
        {
	        return macroHeight;
        }

        public override BlockLayers GenerateBlock3(GridPos position, in Heights v00, in Heights v01,
            in Heights v10, in Heights v11)
        {
			//Trees
            var isObstacle = _zoneRandom.Value( ) < 0.03;
            return new BlockLayers(BlockType.Grass, BlockType.Empty, isObstacle);
        }

        private readonly int _globalZoneHeight;
    }
}
