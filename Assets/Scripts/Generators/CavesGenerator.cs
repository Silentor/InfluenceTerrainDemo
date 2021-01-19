#define CENTER_HOLE

using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Quaternion = OpenToolkit.Mathematics.Quaternion;


namespace TerrainDemo.Generators
{
    public class CavesGenerator : BaseZoneGenerator
    {
        public CavesGenerator( uint index, int seed, BiomeSettings zoneSettings, TriRunner gameResources ) : base( index, seed, zoneSettings, gameResources )
        {
            Assert.IsTrue(zoneSettings.Type == BiomeType.Caves);

            _cavesNoise = new FastNoise( seed );
            _cavesNoise.SetFrequency(0.1);
        }

        public override Macro.Zone GenerateMacroZone( MacroMap map )
        {
            foreach (var cell in Zone.Cells)
            {
#if CENTER_HOLE
                cell.DesiredHeight = new Heights(0, -5, -10);
#else

                var baseHeight = _zoneRandom.Range(-12, -5);
                var mainHeight = _zoneRandom.Range(0, 3);
                cell.DesiredHeight = new Heights(baseHeight, _zoneRandom.Range(baseHeight, mainHeight), mainHeight);
#endif
            }

            return Zone;
        }

        /*
        //todo передавать ячейку, которую заполняем, точно нужно в батч-версии метода
        public override Heights GetMicroHeight(Vector2 position, MacroTemplate.CellMixVertex vertex)
        {
            //Generate big central hole
#if CENTER_HOLE
            var distance = Vector2.Distance(Vector2.Zero, position);
            return new Heights(-10, 
                distance < 10 
                    ? -10 + (10 - distance)
                    : -10, 
                distance < 10 
                    ? -10 + distance 
                    : 0);
#else

            var caveMicroHeight = (float)_cavesNoise.GetSimplex(position.X, position.Y) * 10;
            return new Heights(vertex.MacroHeight.BaseHeight, vertex.MacroHeight.UndergroundHeight + caveMicroHeight, vertex.MacroHeight.Layer1Height);
#endif
        }
        */

        private readonly FastNoise _cavesNoise;
    }
}
