using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Random = TerrainDemo.Tools.Random;
using Vector2 = OpenToolkit.Mathematics.Vector2;

#nullable enable

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
	    private readonly   BiomeSettings _zoneSettings;
        protected readonly TriRunner     _settings;
        private            HexPos[]      _cells;
        public             Macro.Zone    Zone { get; private set; }
        private            HexPos[]      _zonePositions;
        protected readonly Random        _zoneRandom;
        private readonly   uint           _index;


        public BaseZoneGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings)
        {
            //_macroMap = macroMap;
            //_settings = settings;
            //_zoneRandom = new Random(unchecked (settings.Seed + id));

            //Assert.IsTrue(zoneCells.All(c => c.ZoneId == id));

            //Zone = new Macro.Zone( id, _macroMap.GetSubmesh(zoneCells), biome );
        }

        public BaseZoneGenerator( uint index, int seed, BiomeSettings zoneSettings, TriRunner gameResources )
        {
	        _index        = index;
            _zoneRandom   = new Random( seed );
	        _zoneSettings = zoneSettings;
	        _settings     = gameResources;

        }

        public bool GenerateLayout( HexPos startCell, LayoutGrid layout )
        {
            Assert.IsTrue( layout[startCell].IsEmpty );

            var zoneSize      = _zoneRandom.Range(_zoneSettings.SizeRange);
            var zonePositions = layout.FloodFill(startCell, (_, cell) => cell.IsEmpty ).Take(zoneSize).ToArray();
			//todo check for minimum zone size, discard zone, return false
			foreach ( var zonePosition in zonePositions )
			{
				layout[zonePosition] = new CapturedCell( _zoneSettings.DefaultCell, this );
			}

			_zonePositions = zonePositions;

			return true;
        }

        #region Macrolevel generation

        /// <summary>
        /// Generate zone layout on given mesh
        /// </summary>
        public virtual Macro.Zone GenerateMacroZone( MacroMap map )
        {
	        var submesh     = map.GetSubmesh(_zonePositions);
	        var borderCells = submesh.GetBorderCells( ).ToArray(  );
	        var result      = new Macro.Zone( _index, submesh, _zoneSettings );

            foreach (var position in _zonePositions)
            {
                Assert.IsTrue( map.GetCell(position) == null );
                var cell = map.AddCell( position, result );
                Assert.IsFalse( map.GetCell(position) == null );

                if (_zoneSettings.Type == BiomeType.Plains)
                {
                    var baseHeight = _zoneRandom.Range(-5f, -1);
                    cell.DesiredHeight = new Heights(_zoneRandom.Range(0f, 1f), _zoneRandom.Value() > 0.8 ? baseHeight + 2 : baseHeight - 2, baseHeight);
                }
                else if (_zoneSettings.Type == BiomeType.Hills)
                {
                    var baseHeight = _zoneRandom.Range(-12f, -5);
                    cell.DesiredHeight = new Heights(_zoneRandom.Range(1f, 3), _zoneRandom.Range(baseHeight - 2, baseHeight + 2), baseHeight);
                }
                else if (_zoneSettings.Type == BiomeType.Lake)
                    cell.DesiredHeight = new Heights(borderCells.Contains(cell.Position) ? 0f : -10, -100, Zone.Border.Contains(cell) ? -12f : -15);
            }

            Zone = result;
            return result;
        }

        #endregion

        /*
        public virtual Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            return new Blocks(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block, macroHeight);
        }
        */

        #region Microlevel generation

        public virtual void BeginCellGeneration(Micro.Cell microcell)
        {
            //todo Add cell generation timer
        }

        public virtual BlockLayers GenerateBlock3(GridPos position, in Heights v00, in Heights v01,
            in Heights v10,
            in Heights v11)
        {
            return new BlockLayers(Zone.Biome.DefaultMainBlock.Block, Zone.Biome.DefaultUndergroundBlock.Block);
        }

        public virtual Heights GenerateHeight(GridPos position, in Heights macroHeight)
        {
            return macroHeight;
        }

        public virtual void EndCellGeneration(Micro.Cell microcell)
        {
            //todo Add cell generation timer
        }

        #endregion

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
