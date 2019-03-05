using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
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
            var height = 5 * cellsInDistanceOrder.Length;
            foreach (var cell in cellsInDistanceOrder)
            {
                var baseHeight = (height - 12f) / 3;
                cell.DesiredHeight = new Heights(height, UnityEngine.Random.Range(baseHeight - 5, baseHeight + 1), baseHeight);
                height -= 5;
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

        public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            var mainLayer =
                +(float)System.Math.Pow(_microReliefNoise.GetSimplex(position.X / 10f, position.Z / 10f) + 1, 2) - 2 //Вытянутые пики средней частоты
                + (float)_microReliefNoise.GetSimplex(position.X / 2f, position.Z / 2f) //Высокочастотные неровности
                + macroHeight.Main;

            return new Blocks(BlockType.Stone, BlockType.GoldOre,
                new Heights(mainLayer, UnityEngine.Random.Range(macroHeight.Base, macroHeight.Underground), macroHeight.Base));
        }

        private readonly FastNoise _microReliefNoise;
    }
}
