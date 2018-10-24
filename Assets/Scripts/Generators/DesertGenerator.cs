using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Tri;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;
using Quaternion = OpenTK.Quaternion;

namespace TerrainDemo.Generators
{
    public class DesertGenerator : TriZoneGenerator
    {
        public DesertGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Desert);

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);

            _hillsOrientation = _random.Range(0, 180f);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
                cell.Height = _random.Range(0, 3);

            return Zone;
        }

        public override MicroHeight GetMicroHeight(Vector2 position, float macroHeight)
        {
            position = Vector2.Transform(position, Quaternion.FromEulerAngles(0, 0, _hillsOrientation));
            return new MicroHeight(macroHeight - 5,
                (float) (_microReliefNoise.GetSimplex(position.X / 10, position.Y / 30)) * 2 + macroHeight,         //Вытянутые дюны
                Zone.Id,  
                true);
        }

        private readonly FastNoise _microReliefNoise;
        private readonly float _hillsOrientation;
    }
}
