using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Code.Generators;
using Assets.Code.Layout;
using Assets.Code.Voronoi;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Assets.Code
{
    public class Runner : MonoBehaviour, ILandSettings
    {
        [Header("Land settings")] 
        [Range(1, 100)]
        public int ZonesCount = 16;
        public int LandSizeChunks = 30;
        public LandNoiseSettings LandNoiseSettings;
        public ZoneSettings[] Zones;

        [Header("Influence settings")]
        public float IDWCoeff = 2;

        public float IDWOffset = 0;

        public bool InterpolateInfluence = true;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;

        public IEnumerable<Zone> MakeLayout(IEnumerable<Cell> cells)
        {
            var layouter = new LandLayouter();
            return layouter.CreateLayout(cells, this);
        }

        public Dictionary<Vector2i, Chunk> GenerateMap(Land land)
        {
            var time = Stopwatch.StartNew();

            _land = land;
            var landMap = new Dictionary<Vector2i, Chunk>();

            //Generate land's chunks
            var landGenerator = new LandGenerator(land, this);
            landGenerator.Generate(landMap);

            time.Stop();
            Debug.Log(_land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));

            return landMap;
        }

        public void MeshAndVisualize(Dictionary<Vector2i, Chunk> landMap)
        {
            ChunkGO.Clear();

            var mesher = new InfluenceMesher(this);
            foreach (var chunk in landMap)
            {
                var mesh = mesher.Generate(chunk.Value);
                ChunkGO.Create(chunk.Value, mesh);
            }
        }

        public void BuildAll()
        {
            _voronoi = CellMeshGenerator.Generate(ZonesCount, new Bounds(Vector3.zero, 2 * Vector3.one * LandSizeChunks * BlocksCount * BlockSize), Random.Range(int.MinValue, int.MaxValue),
                64);

            _layout = MakeLayout(_voronoi);
            _land = new Land(_layout, this);
            var map = GenerateMap(_land);
            MeshAndVisualize(map);
        }

        public void RecreateMap()
        {
            if (_layout != null)
            {
                _land = new Land(_layout, this);
                var map = GenerateMap(_land);
                MeshAndVisualize(map);
            }
        }

        int ILandSettings.ChunkSize { get { return BlocksCount * BlockSize;} }
        int ILandSettings.BlockSize { get { return BlockSize; } }
        int ILandSettings.BlocksCount { get { return BlocksCount; } }
        int ILandSettings.ZonesCount { get { return ZonesCount; } }
        IEnumerable<ZoneSettings> ILandSettings.ZoneTypes { get { return Zones; } }

        public ZoneSettings this[ZoneType index]
        {
            get { return _zoneSettingsLookup[(int) index]; }
        }

        LandNoiseSettings ILandSettings.LandNoiseSettings { get { return LandNoiseSettings; } }
        Bounds2i ILandSettings.LandSizeChunks { get { return _landSizeChunks; } }
        float ILandSettings.IDWCoeff { get { return IDWCoeff; } }
        float ILandSettings.IDWOffset { get { return IDWOffset; } }

        bool ILandSettings.InterpolateInfluence { get { return InterpolateInfluence; } }

        private Land _land;
        private Bounds2i _landSizeChunks;
        private ZoneSettings[] _zoneSettingsLookup;
        private Cell[] _voronoi;
        private IEnumerable<Zone> _layout;

        void Awake()
        {
            _landSizeChunks = new Bounds2i(Vector2i.Zero, LandSizeChunks);
            _zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            foreach (var zoneSettings in Zones)
                _zoneSettingsLookup[(int) zoneSettings.Type] = zoneSettings;
        }

        void Start()
        {
            BuildAll();
        }

        void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                if (_voronoi != null)
                {
                    Gizmos.color = Color.white;

                    foreach (var cell in _voronoi)
                        foreach (var edge in cell.Edges)
                            Gizmos.DrawLine(new Vector3(edge.Vertex1.x, 50, edge.Vertex1.y), new Vector3(edge.Vertex2.x, 50, edge.Vertex2.y));
                }

                if (_land != null && _land.Zones != null)
                    foreach (var zone in _land.Zones)
                    {
                        if (zone.Type != ZoneType.Empty)
                        {
                            Gizmos.color = this[zone.Type].LandColor;
                            Gizmos.DrawSphere(new Vector3(zone.Center.x, 50, zone.Center.y), 10);
                        }
                    }

            }
        }

    }
}
