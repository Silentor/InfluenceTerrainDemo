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
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenTK.Vector2;

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
        

        public BaseZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings)
        {
            _macroMap = macroMap;
            _settings = settings;
            _random = new Random(unchecked (settings.Seed + id));

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);

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
                    cell.Height = _random.Range(0, 1f);
                else if (Zone.Biome.Type == BiomeType.Hills)
                    cell.Height = _random.Range(1, 3);
                else if (Zone.Biome.Type == BiomeType.Lake)
                    cell.Height = Zone.Border.Contains(cell) ? 0 : -10;
            }

            return Zone;
        }

        
        public void GenerateMicroCell(Micro.Cell cell, MicroMap map)
        {
            /*
            var influences = new double[cell.Blocks.Length][];
            for (int i = 0; i < cell.Blocks.Length; i++)
            {
                influences[i] = _macroMap.GetInfluence(BlockInfo.GetCenter(cell.Blocks[i]));
            }
            */

            /*
            var heights = new float[cell.BlockPositions.Length];
            var blocks = new BlockType[cell.BlockPositions.Length];
            for (int i = 0; i < cell.BlockPositions.Length; i++)
            {
                heights[i] = _macroMap.GetHeight(cell.BlockPositions[i]);
                heights[i] = GetMicroHeight(cell.BlockPositions[i], heights[i]);
                    //blocks[i] = GetBlock(cell.BlockPositions[i]);
            }

            map.SetHeights(cell.BlockPositions, heights);
            */
        }
    

        public virtual MicroHeight GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            if (Zone.Biome.Type == BiomeType.Lake)
                return new MicroHeight(vertex.MacroHeight - 10, vertex.MacroHeight);
            else if (Zone.Biome.Type == BiomeType.Plains)
                return new MicroHeight(vertex.MacroHeight / 3 - 10, vertex.MacroHeight);

            return new MicroHeight(vertex.MacroHeight, vertex.MacroHeight);
        }
        
        public BlockType GetBlock(Vector2i position)
        {
            return Zone.Biome.DefaultBlock.Block;
        }

        public virtual Blocks GetBlocks(Vector2i position, Vector3 normal)
        {
            return new Blocks(){Base = _settings.BaseBlock.Block, Layer1 = Zone.Biome.DefaultBlock.Block};
        }


        public BlockType GetBaseBlock(Vector2i position)
        {
            return _settings.BaseBlock.Block;
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
