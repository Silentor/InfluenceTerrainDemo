using System.Collections.Generic;
using TerrainDemo.Macro;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tri
{
    public class MountainsGenerator : TriZoneGenerator
    {
        public MountainsGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, TriBiome biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Relief == TriBiome.ReliefType.Mountains);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                //Random hills
                //cell.Height = _random.Range(5, 10f);

                //Make one great mountain with peak at map's center (height = 20 m)
                var mountRadius = Vector2.Distance(Vector2.Zero,
                    new Vector2(_macroMap.Bounds.Left, _macroMap.Bounds.Bottom));
                cell.Height = Mathf.Lerp(20, 1, Vector2.Distance(Vector2.Zero, cell.Center) / mountRadius);

                //Make local peak at Zone center
                //Get zone center
                
            }


            return Zone;
        }
    }
}
