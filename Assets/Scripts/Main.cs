using System;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using TerrainDemo.Hero;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;

namespace TerrainDemo
{
    public class Main
    {
        public LandLayout LandLayout { get; private set; }

        public LandMap Map { get; private set; }

        //public Observer Observer { get; private set; }

        public Main(ILandSettings settings, [NotNull] LandLayout landLayout, IObserver observer, MesherSettings mesherSettings)
        {
            if (landLayout == null) throw new ArgumentNullException("landLayout");
            LandLayout = landLayout;

            //var mesher = new InfluenceMesher(this, mesherSettings);
            //var mesher = new ColorMesher(this, mesherSettings);
            _mesher = new TextureMesher(settings, mesherSettings);

            _observer = observer;
            _observer.Changed += ObserverOnChanged;

            Map = new LandMap(settings, LandLayout);
            Map.Modified += MapOnModified;

            _landGenerator = new LandGenerator(LandLayout, settings, Map);
        }

        private void ObserverOnChanged()
        {
            //Get zones in Observer range
            var zones = LandLayout.Zones.Where(zl => zl.Cell.IsClosed && _observer.IsZoneVisible(zl));
            _landGenerator.GenerateAsync(zones);
        }

        public void GenerateLayout([NotNull] LandLayout landLayout)
        {
            if (landLayout == null) throw new ArgumentNullException("landLayout");

            //Empty for now
            LandLayout = landLayout;
        }

        public LandMap GenerateMap(ILandSettings settings)
        {
            //Land = new Land(Layout, settings);

            //Generate land's chunks
            //Get zones in Observer range
            var zones = LandLayout.Zones.Where(zl => zl.Cell.IsClosed && _observer.IsZoneVisible(zl));
            _landGenerator.GenerateAsync(zones);

            //Debug.Log(Land.GetStaticstics());

            return Map;
        }

        private readonly BaseMesher _mesher;
        private readonly IObserver _observer;
        private readonly LandGenerator _landGenerator;

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
