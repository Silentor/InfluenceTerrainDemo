using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Quaternion = OpenToolkit.Mathematics.Quaternion;

namespace TerrainDemo.Generators
{
    public class DesertGenerator : BaseZoneGenerator
    {
        public DesertGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Desert);

            _dunesNoise = new FastNoise(_zoneRandom.Seed);
            _dunesNoise.SetFrequency(1);

            _hillsOrientation = _zoneRandom.Range(0, 180f);
            _stoneBlock = settings.AllBlocks.First(b => b.Block == BlockType.Stone);
            _globalZoneHeight = _zoneRandom.Range(1, 3);
        }

        public override Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                var baseHeight = _zoneRandom.Range(-12, -5) + _globalZoneHeight;
                cell.DesiredHeight = new Heights(_zoneRandom.Range(0, 3) + _globalZoneHeight, _zoneRandom.Range(baseHeight - 5, baseHeight + 1), baseHeight);
            }

            return Zone;
        }

        /*
        public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            var rotatedPos = Vector2.Transform((Vector2)position, Quaternion.FromEulerAngles(0, 0, _hillsOrientation));

            return new Blocks(BlockType.Sand, BlockType.GoldOre,
                new Heights((float)(_dunesNoise.GetSimplex(rotatedPos.X / 10f, rotatedPos.Y / 30f)) * 2 + macroHeight.Main, macroHeight.Underground, macroHeight.Base)); //Вытянутые дюны
        }
        */

        public override Heights GenerateHeight(GridPos position, in Heights macroHeight)
        {
            var rotatedPos = Vector2.Transform(position, Quaternion.FromEulerAngles(0, 0, _hillsOrientation));
            return new Heights((float)(_dunesNoise.GetSimplex(rotatedPos.X / 10f, rotatedPos.Y / 30f)) * 2 + macroHeight.Main, macroHeight.Underground, macroHeight.Base); //Вытянутые дюны
        }

        public override BlockLayers GenerateBlock3(GridPos position, in Heights v00, in Heights v01,
            in Heights v10, in Heights v11)
        {
            var normal = Vector3.Cross(                             //todo simplify
                new Vector3(-1, v01.Nominal - v10.Nominal, 1),
                new Vector3(1, v11.Nominal - v00.Nominal, 1)
            ).Normalized();

            var isObstacle = _zoneRandom.Value( ) < 0.5;

            if(Vector3.CalculateAngle(normal, Vector3.UnitY) <= MathHelper.DegreesToRadians(45))
                return new BlockLayers(BlockType.Sand, BlockType.GoldOre, isObstacle);
            else
                return new BlockLayers(BlockType.Stone, BlockType.GoldOre, isObstacle);

        }

        private readonly FastNoise _dunesNoise;
        private readonly float _hillsOrientation;
        private readonly BlockSettings _stoneBlock;
        private readonly int _globalZoneHeight;
    }
}
