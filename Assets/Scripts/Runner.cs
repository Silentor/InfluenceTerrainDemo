using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo
{
    public class Runner : MonoBehaviour, ILandSettings
    {
        [Header("Land settings")]
        [Range(1, 100)]
        public int ZonesCount = 16;
        [Range(1, 100)]
        public int LandSize = 30;                           //Chunks
        public LandNoiseSettings LandNoiseSettings;
        public ZoneSettings[] Zones;
        public Vector2 ZonesDensity = new Vector2(30, 40);
        public bool GenerateSeed = true;
        public int LandSeed = 0;

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

        public GameObject Stone;

        public Bounds2i LayoutBounds { get; private set; }

        public Main Main { get; private set; }

        public void MeshAndVisualize(LandMap landMap)
        {
            CreateZonesHandle(landMap.Layout.Zones);

            ChunkGO.Clear();

            //var mesher = new InfluenceMesher(this);
            //var mesher = new ColorMesher(this);
            var mesherSettings = GetComponent<MesherSettings>();
            var mesher = new TextureMesher(this, mesherSettings);
            //var mesh = mesher.Generate(landMap.Map[Vector2i.Zero], landMap.Map);
            //var go = ChunkGO.Create(landMap.Map[Vector2i.Zero], mesh);

            foreach (var chunk in landMap.Map)
            {
                var mesh = mesher.Generate(chunk.Value, landMap.Map);
                var go = ChunkGO.Create(chunk.Value, mesh);
                go.CreateFlora(this, chunk.Value.Flora);
                go.CreateStones(this, chunk.Value.Stones);
            }

            Debug.LogFormat("Mesh generation timings: mesh {0} ms, texture {1} ms, {2} ops", mesher.MeshTimer.AvgTimeMs, mesher.TextureTimer.AvgTimeMs, mesher.TextureTimer.SamplesCount);
        }

        public void BuildLayout()
        {
            SetSeed();
            Main.GenerateLayout(new PoissonClusteredLayout(this));
        }


        public void BuildAll()
        {
            BuildLayout();
            var map = Main.GenerateMap(this);
            MeshAndVisualize(map);
        }

        public ZoneSettings this[ZoneType index]
        {
            get { return _zoneSettingsLookup[(int)index]; }
        }

        int ILandSettings.ChunkSize { get { return BlocksCount * BlockSize; } }
        int ILandSettings.BlockSize { get { return BlockSize; } }
        int ILandSettings.BlocksCount { get { return BlocksCount; } }
        int ILandSettings.ZonesCount { get { return ZonesCount; } }
        IEnumerable<ZoneSettings> ILandSettings.ZoneTypes { get { return Zones; } }
        Vector2 ILandSettings.ZonesDensity { get { return ZonesDensity; } }

        LandNoiseSettings ILandSettings.LandNoiseSettings { get { return LandNoiseSettings; } }
        Bounds2i ILandSettings.LandBounds { get { return LayoutBounds; } }
        float ILandSettings.IDWCoeff { get { return IDWCoeff; } }
        float ILandSettings.IDWOffset { get { return IDWOffset; } }

        bool ILandSettings.InterpolateInfluence { get { return InterpolateInfluence; } }

        float ILandSettings.InfluenceThreshold { get { return InfluenceThreshold; } }
        int ILandSettings.InfluenceLimit { get { return InfluenceLimit; } }

        IEnumerable<BlockColors> ILandSettings.Blocks { get { return Blocks ?? new BlockColors[0]; } }

        GameObject ILandSettings.Tree { get { return Tree; } }
        GameObject ILandSettings.Stone { get { return Stone; } }

        //private Bounds2i _landSizeChunks;
        private ZoneSettings[] _zoneSettingsLookup;
        private GameObject _parentObject;

        private void SetSeed()
        {
            if (GenerateSeed)
                LandSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.seed = LandSeed;
        }

        #region Unity

        void Awake()
        {
            SetSeed();

            if (BlockSize*BlocksCount != Chunk.Size)
                throw new ArgumentException("Block size and blocks count invalid");

            _zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            foreach (var zoneSettings in Zones)
                _zoneSettingsLookup[(int)zoneSettings.Type] = zoneSettings;

            _parentObject = new GameObject("Zones");

            Main = new Main(new PoissonClusteredLayout(this));

            BuildLayout();
        }

        void OnValidate()
        {
            if (LandSize < 1) LandSize = 1;

            //Update Land bounds
            var landMin = -LandSize / 2;
            var landMax = landMin + LandSize - 1;
            var minChunkBounds = Chunk.GetBounds(new Vector2i(landMin, landMin));
            var maxChunkBounds = Chunk.GetBounds(new Vector2i(landMax, landMax));

            LayoutBounds = new Bounds2i(minChunkBounds.Min, maxChunkBounds.Max);
        }

        #endregion

        #region Develop

        private void CreateZonesHandle(IEnumerable<ZoneLayout> result)
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

        #endregion
    }
}
