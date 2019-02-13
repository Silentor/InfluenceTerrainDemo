using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
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

            _dunesNoise = new FastNoise(_random.Seed);
            _dunesNoise.SetFrequency(1);

            _hillsOrientation = _random.Range(0, 180f);
            _stoneBlock = settings.AllBlocks.First(b => b.Block == BlockType.Stone);
            _globalZoneHeight = _random.Range(0, 10);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                var baseHeight = _random.Range(-12, -5) + _globalZoneHeight;
                cell.DesiredHeight = new Heights(_random.Range(0, 3) + _globalZoneHeight, _random.Range(baseHeight - 5, baseHeight + 1), baseHeight);
            }

            return Zone;
        }

        public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            var rotatedPos = Vector2.Transform((Vector2)position, Quaternion.FromEulerAngles(0, 0, _hillsOrientation));

            return new Blocks(BlockType.Sand, BlockType.GoldOre,
                new Heights((float)(_dunesNoise.GetSimplex(rotatedPos.X / 10f, rotatedPos.Y / 30f)) * 2 + macroHeight.Layer1Height, macroHeight.UndergroundHeight, macroHeight.BaseHeight)); //Вытянутые дюны
        }

        private float NormalToSlope(Vector3 normal)
        {
            return Vector3.Angle(normal, Vector3.up);
        }

        private readonly FastNoise _dunesNoise;
        private readonly float _hillsOrientation;
        private readonly BlockSettings _stoneBlock;
        private int _globalZoneHeight;
    }
}
