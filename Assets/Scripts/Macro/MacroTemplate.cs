using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Micro;
using TerrainDemo.Tri;
using UnityEngine.Assertions;
using Random = TerrainDemo.Tools.Random;

namespace TerrainDemo.Macro
{
    public class MacroTemplate
    {
        private readonly Random _random;
        private List<TriZoneGenerator> _generators = new List<TriZoneGenerator>();


        public MacroTemplate(Random random)
        {
            _random = random;
        }

        public MacroMap CreateMacroMap(TriRunner settings)
        {
            var result = new MacroMap(settings, _random);
            var zoneGenerators = RandomClusterZonesDivider(result, settings);

            foreach (var generator in zoneGenerators)
            {
                _generators.Add(generator);
                result.Generators.Add(generator);
                var zone = generator.GenerateMacroZone();
                result.Zones.Add(zone);
            }

            return result;
        }

        /// <summary>
        /// Generate micro-presentation for given macro zone and place it to Micromap
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="map"></param>
        public void GenerateMicroZone(Zone zone, MicroMap map)
        {
            //Get generator for this zone
            var generator = _generators.Find(g => g.Zone == zone);

            foreach (var cell in zone.Cells)
            {
                var microcell = map.Cells.First(c => c.Macro == cell);
                generator.GenerateMicroCell(microcell, map);
            }
            
        }

        /*
        public TriMesh GenerateLayout(TriMesh mesh, TriRunner settings)
        {

        }
        */

        /// <summary>
        /// Divide mesh to clustered zones completely random
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private IEnumerable<TriZoneGenerator> RandomClusterZonesDivider(MacroMap mesh, TriRunner settings)
        {
            var zones = new List<TriZoneGenerator>();
            var zoneId = 0;
            foreach (var cell in mesh.Cells)
            {
                if (cell.ZoneId == Cell.InvalidZoneId)
                {
                    var biome = _random.Item(settings.Biomes);
                    var zoneSize = _random.Range(biome.SizeRange);
                    var startCell = cell;
                    var zoneCells = mesh.FloodFill(startCell, c => c.ZoneId == Cell.InvalidZoneId).Take(zoneSize).ToArray();
                    foreach (var triCell in zoneCells)
                    {
                        triCell.ZoneId = zoneId;
                        triCell.Biome = biome;
                    }

                    zones.Add(new TriZoneGenerator(mesh, zoneCells, zoneId, biome, settings));

                    zoneId++;
                }
            }

            //Assert.IsTrue(mesh.Cells.All(c => c.Id != Cell.InvalidZoneId));
            return zones;
        }
    }
}
