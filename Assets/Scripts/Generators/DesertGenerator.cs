using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;
using Quaternion = OpenTK.Quaternion;

namespace TerrainDemo.Generators
{
    public class DesertGenerator : BaseZoneGenerator
    {
        public DesertGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Desert);

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);

            _hillsOrientation = _random.Range(0, 180f);
            _stoneBlock = settings.AllBlocks.First(b => b.Block == BlockType.Stone);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
                cell.DesiredHeight = _random.Range(0, 3);

            return Zone;
        }

        public override MicroHeight GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            position = Vector2.Transform(position, Quaternion.FromEulerAngles(0, 0, _hillsOrientation));
            return new MicroHeight(vertex.MacroHeight - 5,
                (float) (_microReliefNoise.GetSimplex(position.X / 10, position.Y / 30)) * 2 + vertex.MacroHeight,         //Вытянутые дюны
                true);
        }

        public override Blocks GetBlocks(Vector2i position, Vector3 normal)
        {
            if(NormalToSlope(normal) > 5)
                return new Blocks() { Base = _settings.BaseBlock.Block, Layer1 = _stoneBlock.Block };
            else
            {
                return base.GetBlocks(position, normal);
            }
        }

        private float NormalToSlope(Vector3 normal)
        {
            return Vector3.Angle(normal, Vector3.up);
        }

        private readonly FastNoise _microReliefNoise;
        private readonly float _hillsOrientation;
        private readonly BlockSettings _stoneBlock;
    }
}
