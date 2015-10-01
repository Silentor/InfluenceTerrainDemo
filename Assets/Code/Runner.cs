using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Code.Generators;
using Assets.Code.Layout;
using Assets.Code.Meshing;
using Assets.Code.Settings;
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
        [Range(0, 1)]
        public float InfluenceThreshold = 0.01f;
        [Range(0, 20)]
        public int InfluenceLimit = 5;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;

        public BlockColors[] Blocks;

        public GameObject Tree;

        public Land Land { get; private set; }

        public IEnumerable<Zone> MakeLayout(IEnumerable<Cell> cells)
        {
            var layouter = new LandLayouter();
            var result = layouter.CreateLayout(cells, this);
            CreateZonesHandle(result);
            return result;
        }

        public Dictionary<Vector2i, Chunk> GenerateMap(Land land)
        {
            var time = Stopwatch.StartNew();

            Land = land;
            var landMap = new Dictionary<Vector2i, Chunk>();

            //Generate land's chunks
            var landGenerator = new LandGenerator(land, this);
            landGenerator.Generate(landMap);

            time.Stop();
            Debug.Log(Land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));

            return landMap;
        }

        public void MeshAndVisualize(Dictionary<Vector2i, Chunk> landMap)
        {
            ChunkGO.Clear();

            //var mesher = new InfluenceMesher(this);
            var mesher = new ColorMesher(this);
            foreach (var chunk in landMap)
            {
                var mesh = mesher.Generate(chunk.Value);
                var go = ChunkGO.Create(chunk.Value, mesh);
                go.CreateFlora(this, chunk.Value.Flora);
            }
        }

        public void BuildAll()
        {
            _voronoi = CellMeshGenerator.Generate(ZonesCount, new Bounds(Vector3.zero, 2 * Vector3.one * LandSizeChunks * BlocksCount * BlockSize), Random.Range(int.MinValue, int.MaxValue),
                64);

            _layout = MakeLayout(_voronoi);
            Land = new Land(_layout, this);
            var map = GenerateMap(Land);
            MeshAndVisualize(map);
        }

        public void RecreateMap()
        {
            if (_layout != null)
            {
                Land = new Land(_layout, this);
                var map = GenerateMap(Land);
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

        float ILandSettings.InfluenceThreshold { get { return InfluenceThreshold; } }
        int ILandSettings.InfluenceLimit { get { return InfluenceLimit; } }

        IEnumerable<BlockColors> ILandSettings.Blocks { get { return Blocks ?? new BlockColors[0]; } }

        GameObject ILandSettings.Tree { get { return Tree; } }

        private Bounds2i _landSizeChunks;
        private ZoneSettings[] _zoneSettingsLookup;
        private Cell[] _voronoi;
        private IEnumerable<Zone> _layout;
        private GameObject _parentObject;

        private void CreateZonesHandle(IEnumerable<Zone> result)
        {
            var oldZones = _parentObject.transform.GetComponentsInChildren<Transform>().Where(t => t != _parentObject.transform).ToArray();
            foreach (var zone in oldZones)
                Destroy(zone.gameObject);

            foreach (var zone in result)
            {
                var zoneHandleGO = new GameObject(zone.Type.ToString());
                zoneHandleGO.transform.position = new Vector3(zone.Center.x, 0, zone.Center.y);
                zoneHandleGO.transform.parent = _parentObject.transform;
            }
        }

        void Awake()
        {
            _landSizeChunks = new Bounds2i(Vector2i.Zero, LandSizeChunks);
            _zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            foreach (var zoneSettings in Zones)
                _zoneSettingsLookup[(int) zoneSettings.Type] = zoneSettings;

            _parentObject = new GameObject("Zones");
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

                if (Land != null && Land.Zones != null)
                    foreach (var zone in Land.Zones)
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
