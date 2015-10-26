using System.Collections.Generic;
using System.Diagnostics;
using Assets.Code.Generators;
using Assets.Code.Layout;
using Assets.Code.Settings;
using Debug = UnityEngine.Debug;

namespace Assets.Code
{
    public static class Main
    {
        public static LandLayout Layout { get; set; }

        public static Land Land { get; private set; }

        public static Dictionary<Vector2i, Chunk> GenerateMap(ILandSettings settings)
        {
            var time = Stopwatch.StartNew();

            //Land = new Land(Layout, settings);

            //Generate land's chunks
            var landGenerator = new LandGenerator(Layout, settings);
            var result = landGenerator.Generate();

            time.Stop();
            //Debug.Log(Land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));

            return result;
        }


    }
}
