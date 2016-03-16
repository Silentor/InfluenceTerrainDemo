using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using TerrainDemo.Hero;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Settings;
using Debug = UnityEngine.Debug;

namespace TerrainDemo
{
    public class Main
    {
        public LandLayout LandLayout { get; private set; }

        public LandMap Map { get; private set; }

        //public Observer Observer { get; private set; }

        public Main([NotNull] LandLayout landLayout, ObserverSettings observer)
        {
            if (landLayout == null) throw new ArgumentNullException("landLayout");
            LandLayout = landLayout;
        }

        public void GenerateLayout([NotNull] LandLayout landLayout)
        {
            if (landLayout == null) throw new ArgumentNullException("landLayout");

            //Empty for now
            LandLayout = landLayout;
        }

        public LandMap GenerateMap(ILandSettings settings)
        {
            var time = Stopwatch.StartNew();

            //Land = new Land(Layout, settings);

            //Generate land's chunks
            var map = new LandMap(settings, LandLayout);
            var landGenerator = new LandGenerator(LandLayout, settings);
            Map = landGenerator.Generate(map);

            time.Stop();
            //Debug.Log(Land.GetStaticstics());
            Debug.Log(string.Format("Generate map {0} ms", time.ElapsedMilliseconds));

            return Map;
        }


    }
}
