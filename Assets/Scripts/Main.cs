using System;
using System.Diagnostics;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using Debug = UnityEngine.Debug;

namespace TerrainDemo
{
    public class Main
    {
        public LandLayout LandLayout { get; private set; }

        public LandMap Map { get; private set; }

        //public Observer Observer { get; private set; }

        public Main(ILandSettings settings, [NotNull] LandLayout landLayout, ObserverSettings observer, MesherSettings mesherSettings)
        {
            if (landLayout == null) throw new ArgumentNullException("landLayout");
            LandLayout = landLayout;

            //var mesher = new InfluenceMesher(this, mesherSettings);
            //var mesher = new ColorMesher(this, mesherSettings);
            _mesher = new TextureMesher(settings, mesherSettings);
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
            Map = new LandMap(settings, LandLayout);
            Map.Modified += MapOnModified;
            var landGenerator = new LandGenerator(LandLayout, settings, Map);
            landGenerator.GenerateAsync();

            time.Stop();
            //Debug.Log(Land.GetStaticstics());
            Debug.Log(string.Format("Generate map {0} ms", time.ElapsedMilliseconds));

            return Map;
        }

        private readonly BaseMesher _mesher;

        /// <summary>
        /// Mesh and visualize modified chunk
        /// </summary>
        /// <param name="chunk"></param>
        private void MapOnModified(Chunk chunk)
        {
            var mesh = _mesher.Generate(chunk, Map.Map);
            var go = ChunkGO.Create(chunk, mesh);
        }
    }
}
