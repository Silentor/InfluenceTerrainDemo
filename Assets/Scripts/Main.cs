using System.Collections.Generic;
using System.Diagnostics;
using TerrainDemo.Generators;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using Debug = UnityEngine.Debug;

namespace TerrainDemo
{
    public static class Main
    {
        public static LandLayout Layout { get; set; }

        public static Land Land { get; private set; }

        public static LandMap GenerateMap(ILandSettings settings)
        {
            var time = Stopwatch.StartNew();

            //Land = new Land(Layout, settings);

            //Generate land's chunks
            var map = new LandMap(settings);
            var landGenerator = new LandGenerator(Layout, settings);
            var result = landGenerator.Generate(map);

            time.Stop();
            //Debug.Log(Land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));

            return result;
        }


    }
}
