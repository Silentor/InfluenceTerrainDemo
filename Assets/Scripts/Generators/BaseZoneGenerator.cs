using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Generators
{
    //Зон Генератор не пытается юзать соседние Зон Генераторы и смешивать пограничные блоки. Он просто генерит
    //по запросу блоки (как своей Зоны, так и за пределами). Более общий LandGenerator генерит пограничные 
    //блоки их ЗонГенераторами, а потом смешивает их в зависимости от инфлюенса. Инфлюенс основан за 
    //Зонах, а не Биомах
    /// <summary>
    /// Generate given zone heights, materials etc
    /// </summary>
    public class BaseZoneGenerator
    {
        protected readonly MacroMap _macroMap;
        protected readonly TriRunner _settings;
        private Cell[] _cells;
        public readonly Macro.Zone Zone;
        protected readonly Random _random;
        private readonly FastNoise _microReliefNoise;
        private readonly FastNoise _resourcesNoise;


        public BaseZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings)
        {
            _macroMap = macroMap;
            _settings = settings;
            _random = new Random(unchecked (settings.Seed + id));

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);

            _resourcesNoise = new FastNoise(_random.Seed);
            _resourcesNoise.SetFrequency(0.05);

            Assert.IsTrue(zoneCells.All(c => c.ZoneId == id));

            Zone = new Macro.Zone(_macroMap, _macroMap.GetSubmesh(zoneCells), id, biome, settings);
        }

        /// <summary>
        /// Generate zone layout on given mesh
        /// </summary>
        public virtual Macro.Zone GenerateMacroZone()
        {
            foreach (var cell in Zone.Cells)
            {
                if (Zone.Biome.Type == BiomeType.Plains)
                {
                    var baseHeight = _random.Range(-5f, -1);
                    cell.DesiredHeight = new Heights(_random.Range(0f, 1f), _random.Value() > 0.8 ? baseHeight + 2 : baseHeight - 2, baseHeight);
                }
                else if (Zone.Biome.Type == BiomeType.Hills)
                {
                    var baseHeight = _random.Range(-12f, -5);
                    cell.DesiredHeight = new Heights(_random.Range(1f, 3), _random.Range(baseHeight - 2, baseHeight + 2), baseHeight);
                }
                else if (Zone.Biome.Type == BiomeType.Lake)
                    cell.DesiredHeight = new Heights(Zone.Border.Contains(cell) ? 0f : -10, -100, Zone.Border.Contains(cell) ? -12f : -15);
            }

            return Zone;
        }

        /*
        public virtual Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block, macroHeight);
        }
        */

        public virtual BlockLayers GenerateBlock3(Vector2i position, in Heights v00, in Heights v01, in Heights v10,
            in Heights v11)
        {
            return new BlockLayers(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block);
        }

        public virtual Heights GenerateHeight(Vector2i position, in Heights macroHeight)
        {
            return macroHeight;
        }

        /// <summary>
        /// Zone map
        /// </summary>
        public class CellContent
        {
            public readonly Micro.Cell Cell;
            public readonly float[,] HeightMap;
            public readonly BlockType[,] Blocks;

            public CellContent(Micro.Cell cell, float[,] heightMap, BlockType[,] blocks)
            {
                Cell = cell;
                HeightMap = heightMap;
                Blocks = blocks;
            }

            /*
            public Vector2i GetWorldPosition(Vector2i localPosition)
            {
                return Zone.Bounds.Min + localPosition;
            }

            public Vector2i GetLocalPosition(Vector2i worldPosition)
            {
                return worldPosition - Zone.Bounds.Min;
            }
            */
        }
    }
}
