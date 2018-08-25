using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tri
{
    //Зон Генератор не пытается юзать соседние Зон Генераторы и смешивать пограничные блоки. Он просто генерит
    //по запросу блоки (как своей Зоны, так и за пределами). Более общий LandGenerator генерит пограничные 
    //блоки их ЗонГенераторами, а потом смешивает их в зависимости от инфлюенса. Инфлюенс основан за 
    //Зонах, а не Биомах
    /// <summary>
    /// Generate given zone heights, materials etc
    /// </summary>
    public class TriZoneGenerator
    {
        protected readonly MacroMap _macroMap;
        private readonly TriRunner _settings;
        private Cell[] _cells;
        public readonly Macro.Zone Zone;
        private readonly Random _random;
        private FastNoise _microReliefNoise;

        public TriZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, TriBiome biome, TriRunner settings)
        {
            _macroMap = macroMap;
            _settings = settings;
            _cells = zoneCells.ToArray();
            _random = new Random(settings.Seed + id);

            _microReliefNoise = new FastNoise(_random.Seed);
            _microReliefNoise.SetFrequency(1);

            Assert.IsTrue(_cells.All(c => c.Biome == biome && c.ZoneId == id));

            Zone = new Macro.Zone(_macroMap, _cells, id, biome, settings);

        }

        /// <summary>
        /// Generate zone layout on given mesh
        /// </summary>
        public virtual Macro.Zone GenerateMacroZone()
        {
            /*
            switch (Zone.Biome.Relief )
            {
                case TriBiome.ReliefType.Mountains:
                    GenerateMountainsHeights();
                    break;
                case TriBiome.ReliefType.Plains:
                    GeneratePlainsHeights();
                    break;
                case TriBiome.ReliefType.Water:
                    GenerateLakeHeights();
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var cell in _cells)
            {
                cell.Tesselate2();
            }
            */

            
            foreach (var cell in Zone.Cells)
            {
                if (Zone.Biome.Relief == TriBiome.ReliefType.Mountains)
                    //cell.Height = _random.Range(5, 10f);
                {
                    //Make one great mountain with peak at map's center
                    var mountRadius = Vector2.Distance(Vector2.Zero, new Vector2(_macroMap.Bounds.Left, _macroMap.Bounds.Bottom));
                    cell.Height = Mathf.Lerp(20, 1, Vector2.Distance(Vector2.Zero, cell.Center) / mountRadius );
                    //cell.Height = 50;
                }
                else if (Zone.Biome.Relief == TriBiome.ReliefType.Plains)
                {
                    cell.Height = _random.Range(0, 1f);
                }

            }
            

            return Zone;
        }

        public void GenerateMicroCell(Micro.Cell cell, MicroMap map)
        {


            var influences = new double[cell.Blocks.Length][];
            for (int i = 0; i < cell.Blocks.Length; i++)
            {
                //influences[i] = cell.Macro.Zone.Influence;          //DEBUG no actual influence calculation
                influences[i] = _macroMap.GetInfluence(BlockInfo.GetCenter(cell.Blocks[i]));
            }

            map.SetInfluence(cell.Blocks, influences);

            var heights = new float[cell.Blocks.Length];
            for (int i = 0; i < cell.Blocks.Length; i++)
            {
                //influences[i] = cell.Macro.Zone.Influence;          //DEBUG no actual influence calculation
                heights[i] = _macroMap.GetHeight(cell.Blocks[i]);
                heights[i] += (float) _microReliefNoise.GetSimplex(cell.Blocks[i].X, cell.Blocks[i].Z);
            }

            map.SetHeights(cell.Blocks, heights);
        }

        private void GenerateLakeHeights()
        {
            /*
            foreach (var cell in _cells)
            {
                cell.H1 = 0;
                cell.H2 = 0;
                cell.H3 = 0;
            }
            */
        }

        private void GenerateMountainsHeights()
        {
            /*
            foreach (var cell in _cells)
            {
                cell.H1 = UnityEngine.Random.Range(5, 15f);
                cell.H2 = UnityEngine.Random.Range(5, 15f);
                cell.H3 = UnityEngine.Random.Range(5, 15f);
            }
            */
        }

        private void GeneratePlainsHeights()
        {
            /*
            foreach (var cell in _cells)
            {
                cell.H1 = UnityEngine.Random.Range(1, 5f);
                cell.H2 = UnityEngine.Random.Range(1, 5f);
                cell.H3 = UnityEngine.Random.Range(1, 5f);
            }
            */
        }
    }
}
