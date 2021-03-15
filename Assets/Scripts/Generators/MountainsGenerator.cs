using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Generators
{
    public class MountainsGenerator : BaseZoneGenerator
    {
	    public MountainsGenerator( uint index, int seed, BiomeSettings zoneSettings, TriRunner gameResources ) : base( index, seed, zoneSettings, gameResources )
	    {
		    _microReliefNoise = new FastNoise(_zoneRandom.Seed);
		    _microReliefNoise.SetFrequency(1);
	    }
	    public MountainsGenerator(MacroMap macroMap, IEnumerable<Cell> zoneCells, int id, BiomeSettings biome, TriRunner settings) : base(macroMap, zoneCells, id, biome, settings)
        {
            Assert.IsTrue(biome.Type == BiomeType.Mountain);

            _microReliefNoise = new FastNoise(_zoneRandom.Seed);
            _microReliefNoise.SetFrequency(1);
        }

        public override Macro.Zone GenerateMacroZone( MacroMap map )
        {
            var submesh     = map.GetSubmesh(_zonePositions);
            var borderCells = submesh.GetBorderCells( ).ToArray(  );
            Zone      = new Macro.Zone( _index, submesh, _zoneSettings );

            //Fill cells
            foreach ( var position in _zonePositions )
            {
                map.AddCell( position, Zone );
            }

            //Select some center zone as mountain top
            Vector2 zoneAverageCenterPoint = Vector2.Zero;
            foreach (var cell in Zone )
            {
                zoneAverageCenterPoint += cell.Center;
            }
            zoneAverageCenterPoint /= Zone.Cells.Count;

            var peak = Zone.Cluster.GetNearestCell((GridPos)zoneAverageCenterPoint);
            var cellsInDistanceOrder = Zone.Cluster.FloodFill(peak).Select( pos => Zone.Cluster.Grid[pos] ).ToArray();

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

        public override Heights GenerateHeight(GridPos position, in Heights macroHeight)
        {
            var mainLayer =
                +(float)System.Math.Pow(_microReliefNoise.GetSimplex(position.X / 10f, position.Z / 10f) + 1, 2) - 2 //Вытянутые пики средней частоты
                + (float)_microReliefNoise.GetSimplex(position.X / 2f, position.Z / 2f) //Высокочастотные неровности
                + macroHeight.Main;
            return new Heights(mainLayer, UnityEngine.Random.Range(macroHeight.Base, macroHeight.Underground),
                macroHeight.Base);
        }

        public override BlockLayers GenerateBlock3(GridPos position, in Heights v00, in Heights v01,
            in Heights v10, in Heights v11)
        {
            return new BlockLayers(BlockType.Stone, BlockType.GoldOre);
        }

        /*
        public override Blocks GenerateBlock2(Vector2i position, Heights macroHeight)
        {
            var mainLayer =
                +(float)System.Math.Pow(_microReliefNoise.GetSimplex(position.X / 10f, position.Z / 10f) + 1, 2) - 2 //Вытянутые пики средней частоты
                + (float)_microReliefNoise.GetSimplex(position.X / 2f, position.Z / 2f) //Высокочастотные неровности
                + macroHeight.Main;

            return new Blocks(BlockType.Stone, BlockType.GoldOre,
                new Heights(mainLayer, UnityEngine.Random.Range(macroHeight.Base, macroHeight.Underground), macroHeight.Base));
        }
        */

        private readonly FastNoise _microReliefNoise;
    }
}
