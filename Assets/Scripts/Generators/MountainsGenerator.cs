using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Tri;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Generators
{
    public class MountainsGenerator : BaseZoneGenerator
    {
        public MountainsGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Mountain);

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            //Select some center zone as mountain top
            
            Vector2 zoneAverageCenterPoint = Vector2.Zero;
            foreach (var cell in Zone.Cells)
            {
                zoneAverageCenterPoint += cell.Center;
            }

            zoneAverageCenterPoint /= Zone.Cells.Count;

            var peak = Zone.Submesh.GetNearestFace(zoneAverageCenterPoint);
            var cellsInDistanceOrder = Zone.Submesh.FloodFill(peak).ToArray();

            //Make one-peak conus mountain
            var heights = 5 * cellsInDistanceOrder.Length;
            foreach (var cell in cellsInDistanceOrder)
            {
                cell.DesiredHeight = heights;
                heights -= 5;
            }

            /*
            foreach (var cell in Zone.Cells)
            {
                //Make one great mountain with peak at map's center (height = 20 m)
                var mountRadius = Vector2.Distance(Vector2.Zero,
                    new Vector2(_macroMap.Bounds.Left, _macroMap.Bounds.Bottom));
                cell.Height = Mathf.Lerp(20, 1, Vector2.Distance(Vector2.Zero, cell.Center) / mountRadius);
            }
            */

            return Zone;
        }

        public override MicroHeight GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            return new MicroHeight(vertex.MacroHeight / 2 - 10,
                +(float)System.Math.Pow(_microReliefNoise.GetSimplex(position.X / 10, position.Y / 10) + 1, 2) - 2    //Вытянутые пики средней частоты
                + (float)_microReliefNoise.GetSimplex(position.X / 2, position.Y / 2)    //Высокочастотные неровности
                + vertex.MacroHeight);
        }

        private readonly FastNoise _microReliefNoise;
    }
}
