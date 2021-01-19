using System;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Quaternion = OpenToolkit.Mathematics.Quaternion;

#nullable enable

namespace TerrainDemo.Generators
{
    public class NavigationTestGenerator : BaseZoneGenerator
    {
        public NavigationTestGenerator( uint index, int seed, BiomeSettings zoneSettings, TriRunner gameResources ) : base( index, seed, zoneSettings, gameResources )
        {
            Assert.IsTrue(zoneSettings.Type == BiomeType.TestNavigation);

            _mountNoise = new FastNoise(_zoneRandom.Seed);
            _mountNoise.SetFrequency(0.2);

            _dunesNoise = new FastNoise(_zoneRandom.Seed);
            _dunesNoise.SetFrequency(1);
            _sandDunesOrientation = Quaternion.FromEulerAngles(0, 0, _zoneRandom.Range(-MathHelper.PiOver2, MathHelper.PiOver2));

            _globalZoneHeight = _zoneRandom.Range(1, 3);
        }

        public override Macro.Zone GenerateMacroZone( MacroMap map )
        {
            //Random mounts, trenches and flats
            foreach (var cell in Zone.Cells)
            {
                var relief = GetCellRelief();
                var material = GetCellMaterial(relief);
                //var cellRelief = GetCellHeight(relief);
                //cell.DesiredHeight = new Heights(cellRelief, cellRelief - 1);

                _cells[cell] = (relief, material);
            }

            foreach (var cell in Zone.Cells)
            {
                var cellData = _cells[cell];
                var height = GetCellHeight(cellData.Item1);
                var neigborsCount = 0;
                if (cellData.Item1 == CellRelief.Mountain)
                {
                    foreach (var neighbor in cell.NeighborsSafe)
                    {
                        if (_cells.TryGetValue(neighbor, out var neighborData) &&
                            neighborData.Item1 == CellRelief.Mountain)
                            neigborsCount++;
                        //height += GetCellHeight(CellRelief.Mountain);
                    }

                    if (neigborsCount >= 3)
                        height += 20;
                }
                else if (cellData.Item1 == CellRelief.Trench)
                {
                    foreach (var neighbor in cell.NeighborsSafe)
                    {
                        if (_cells.TryGetValue(neighbor, out var neighborData) &&
                            neighborData.Item1 == CellRelief.Trench)
                            neigborsCount++;
                        //height += GetCellHeight(CellRelief.Trench);
                    }

                    if (neigborsCount >= 3)
                        height -= 20;
                }

                cell.DesiredHeight = cell.DesiredHeight = new Heights(height, height - 1);
            }

            return Zone;
        }

        public override void BeginCellGeneration(Micro.Cell microcell)
        {
            base.BeginCellGeneration(microcell);

            _currentCell = microcell;
            _currentCellData = _cells[microcell.Macro];
        }

        public override Heights GenerateHeight(GridPos position, in Heights macroHeight)
        {
            if (_currentCell != null)
            {
                float height = macroHeight.Nominal;
                if (_currentCellData.Item1 == CellRelief.Mountain)
                    height = macroHeight.Nominal + (float)_mountNoise.GetSimplex(position.X, position.Z) * 4;
                
                else if (_currentCellData.Item2 == BlockType.Sand)
                {
                    var rotatedPos = Vector2.Transform(position, _sandDunesOrientation);
                    height = macroHeight.Nominal + (float)_dunesNoise.GetSimplex(rotatedPos.X / 5, rotatedPos.Y / 30) * 2;
                }
                    
                return new Heights(height, height - 1);
                
            }
            

            return base.GenerateHeight(position, in macroHeight);

            //return base.GenerateHeight(position, in macroHeight);
            //
            //return new Heights((float)(_dunesNoise.GetSimplex(rotatedPos.X / 10f, rotatedPos.Y / 30f)) * 2 + macroHeight.Main, macroHeight.Underground, macroHeight.Base); //Вытянутые дюны
        }

        public override BlockLayers GenerateBlock3(GridPos position, in Heights v00, in Heights v01,
            in Heights v10, in Heights v11)
        {
	        if ( _currentCell != null )
	        {
		        var isObstacle = _zoneRandom.Value( ) < 0.01;
		        return new BlockLayers(_currentCellData.Item2, BlockType.Empty, isObstacle);
	        }
            else //Just defence
                return new BlockLayers(BlockType.Empty, BlockType.Empty);
        }

        public override void EndCellGeneration(Micro.Cell microcell)
        {
            base.EndCellGeneration(microcell);

            Assert.IsTrue(_currentCell == microcell);
            _currentCell = null;
        }

        private readonly FastNoise _dunesNoise;
        private readonly int _globalZoneHeight;
        private Micro.Cell _currentCell;
        private (CellRelief, BlockType) _currentCellData;
        private readonly Dictionary<Macro.Cell, (CellRelief, BlockType)> _cells = new Dictionary<Cell, (CellRelief, BlockType)>();
        private readonly FastNoise _mountNoise;
        private readonly Quaternion _sandDunesOrientation;

        private CellRelief GetCellRelief()
        {
            return (CellRelief) _zoneRandom.Range(Enum.GetValues(typeof(CellRelief)).Length);
        }

        private float GetCellHeight(CellRelief relief)
        {
            switch (relief)
            {
                case CellRelief.Flat: return 0;
                case CellRelief.Mountain: return 10;
                case CellRelief.Trench: return -10;
                default: throw new NotImplementedException();
            }
        }

        private BlockType GetCellMaterial(CellRelief relief)
        {
            return relief == CellRelief.Mountain ? BlockType.Stone :
                relief == CellRelief.Flat ? BlockType.Grass : BlockType.Sand;

            var materialIndex = _zoneRandom.Range(3);
            BlockType material = materialIndex == 0 ? BlockType.Grass :
                materialIndex == 1 ? BlockType.Sand : BlockType.Stone;
            return material;
        }

        private enum CellRelief
        {
            Trench,
            Flat,
            Mountain
        }
    }
}
