using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using TerrainDemo.Hero;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo
{
    public class Main
    {
        public LandLayout LandLayout { get; private set; }

        public LandMap Map { get; private set; }

        //public Observer Observer { get; private set; }

        public Main(ILandSettings settings, IObserver observer, MesherSettings mesherSettings)
        {
            _settings = settings;

            _generator = settings.CreateLayoutGenerator();
            _mesher = mesherSettings.CreateMesher(settings);

            //First time land layout generation
            LandLayout = _generator.Generate(null);

            //Generate and visualize map on Observer move
            _observer = observer;
            _observer.Changed += ObserverOnChangedGenerator;
            _observer.Changed += ObserverOnChangedVisualizer;

            Map = new LandMap(_settings, LandLayout);
            //Map.Modified += MapOnModified;

            _landGenerator = new LandGenerator(LandLayout, _settings, Map);
        }

        /// <summary>
        /// Regenerate layout, invalidate map and view
        /// </summary>
        public void GenerateLayout()
        {
            //Map.Clear();
            //_mesher.Clear();
            _generator.Generate(LandLayout);
            //ObserverOnChanged();
        }

        /// <summary>
        /// Regenerate map over existing layout
        /// </summary>
        public void GenerateMap()
        {
            Map.Clear();
            //_mesher.Clear();
            GenerateAllMap();

            //ObserverOnChanged();
        }

        /// <summary>
        /// Regenerate land mesh over existing map
        /// </summary>
        public void GenerateMesh()
        {
            foreach (var farAwayChunk in _visualizedChunks.ToArray())
                farAwayChunk.Value.Dispose();
            _visualizedChunks.Clear();

            _mesher.Clear();

            foreach (var chunk in Map.Map.Values)
            {
                var mesh = _mesher.Generate(chunk, Map.Map);
                var go = ChunkGO.Create(chunk, mesh);
                Debug.LogFormat(go, "Generated mesh for chunk {0}", chunk.Position);

                if (_visualizedChunks.ContainsKey(chunk.Position))
                    Debug.LogFormat("Chunk {0} already visualized", chunk.Position);

                _visualizedChunks[chunk.Position] = go;
            }
            //ObserverOnChanged();
        }

        private readonly BaseMesher _mesher;
        private readonly ILandSettings _settings;
        private readonly IObserver _observer;
        private readonly LandGenerator _landGenerator;
        private readonly List<ZoneLayout> _alreadyGeneratedZones = new List<ZoneLayout>();
        private readonly Dictionary<Vector2i, ChunkGO> _visualizedChunks = new Dictionary<Vector2i, ChunkGO>();         //Move to appropriate Visualizer class
        private LayoutGenerator _generator;

        private void GenerateAllMap()
        {
            if (LandLayout != null)
            {
                //Get zones in Observer range
                var zones =
                    LandLayout.Zones.Where(zl => zl.Cell.IsClosed)
                        .ToArray();
                _landGenerator.Generate(zones, false);
                _alreadyGeneratedZones.AddRange(zones);
            }
        }

        /// <summary>
        /// Mesh and visualize modified chunk
        /// </summary>
        /// <param name="chunk"></param>
        private void MapOnModified(Chunk chunk)
        {
            if (_observer.IsBoundVisible(Chunk.GetBounds(chunk.Position)))
            {
                var mesh = _mesher.Generate(chunk, Map.Map);
                Debug.LogFormat("Generated mesh for chunk {0}", chunk.Position);

                var go = ChunkGO.Create(chunk, mesh);

                if (_visualizedChunks.ContainsKey(chunk.Position))
                    Debug.LogFormat("Chunk {0} already visualized", chunk.Position);
                
                _visualizedChunks[chunk.Position] = go;
            }
        }

        private void ObserverOnChangedGenerator()
        {
            if (LandLayout != null)
            {
                //Get zones in Observer range
                var zones =
                    LandLayout.Zones.Where(
                            zl => zl.Cell.IsClosed && !_alreadyGeneratedZones.Contains(zl) && _observer.IsZoneVisible(zl))
                        .ToArray();
                _landGenerator.Generate(zones, true);
                _alreadyGeneratedZones.AddRange(zones);
            }
        }

        private void ObserverOnChangedVisualizer()
        {
            //Destroy chunk meshes far away from Observer
            var farAwayChunks = _visualizedChunks.Where(vc => !_observer.IsBoundVisible(Chunk.GetBounds(vc.Key))).ToArray();
            foreach (var farAwayChunk in farAwayChunks)
            {
                farAwayChunk.Value.Dispose();
                _visualizedChunks.Remove(farAwayChunk.Key);
            }

            //Draw chunks in Observer range
            foreach (var chunkToDraw in _observer.ValuableChunkPos(_observer.Range))
            {
                if (!_visualizedChunks.ContainsKey(chunkToDraw.Position))
                {
                    Chunk chunk;
                    if (Map.Map.TryGetValue(chunkToDraw.Position, out chunk))
                    {
                        var mesh = _mesher.Generate(chunk, Map.Map);
                        var go = ChunkGO.Create(chunk, mesh);

                        if (_visualizedChunks.ContainsKey(chunk.Position))
                            Debug.LogFormat("Chunk {0} already visualized", chunk.Position);

                        _visualizedChunks[chunk.Position] = go;
                    }
                }
            }
        }
    }
}
