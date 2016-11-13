using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Generators;
using TerrainDemo.Hero;
using TerrainDemo.Layout;
using TerrainDemo.Map;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using TerrainDemo.Threads;
using TerrainDemo.Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TerrainDemo
{
    public class Runner : MonoBehaviour, ILandSettings
    {
        [Header("Land settings")]
        [Range(1, 100)]
        public int ZonesCount = 16;
        [Range(1, 100), Tooltip("Chunks")]
        public int LandSize = 30;                           //Chunks
        public LandNoiseSettings LandNoiseSettings;
        public ZoneSettings[] Zones;
        public Vector2 ZonesDensity = new Vector2(30, 40);
        public bool GenerateSeed = true;
        public int LandSeed = 0;
        public LayoutGenerator.Type LayoutGenerator = Generators.LayoutGenerator.Type.PoissonClustered;

        [Header("Influence settings")]
        public float IDWCoeff = 2;
        public float IDWOffset = 0;
        public float IDWRadius = 30;
        public int IDWNearestPoints = 7;
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

        public GameObject Tree;

        public GameObject Stone;

        public Bounds2i LayoutBounds { get; private set; }

        public Main Main { get; private set; }

        public IObserver Observer { get; private set; }

        /// <summary>
        /// Generate layout, map and mesh
        /// </summary>
        public void GenerateLayout()
        {
            SetSeed();
            Main.GenerateLayout();
        }

        /// <summary>
        /// Regenerate map and mesh
        /// </summary>
        public void GenerateMap()
        {
            Main.GenerateMap();
        }

        /// <summary>
        /// Regenerate mesh
        /// </summary>
        public void GenerateMesh()
        {
            Main.GenerateMesh();   
        }

        public ZoneSettings this[ZoneType index]
        {
            get { return _zoneSettingsLookup[(int)index]; }
        }

        #region Settings

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
        float ILandSettings.IDWRadius { get { return IDWRadius; } }
        int ILandSettings.IDWNearestPoints { get { return IDWNearestPoints; } }

        bool ILandSettings.InterpolateInfluence { get { return InterpolateInfluence; } }

        float ILandSettings.InfluenceThreshold { get { return InfluenceThreshold; } }
        int ILandSettings.InfluenceLimit { get { return InfluenceLimit; } }

        GameObject ILandSettings.Tree { get { return Tree; } }
        GameObject ILandSettings.Stone { get { return Stone; } }

        public LayoutGenerator CreateLayoutGenerator()
        {
            switch (LayoutGenerator)
            {
                case Generators.LayoutGenerator.Type.Random:
                    return new RandomLayoutGenerator(this);

                case Generators.LayoutGenerator.Type.PoissonTwoSide:
                    return new PoissonTwoSideLayoutGenerator(this);

                case Generators.LayoutGenerator.Type.PoissonClustered:
                    return new PoissonClusteredLayoutGenerator(this);

                default:
                    throw new NotImplementedException(string.Format("Layout generator type {0} is not defined", LayoutGenerator));
            }
        }

        #endregion

        //private Bounds2i _landSizeChunks;
        private ZoneSettings[] _zoneSettingsLookup;
        private GameObject _zonesParent;

        private void SetSeed()
        {
            if (GenerateSeed)
                LandSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(LandSeed);
        }

        #region Unity

        void Awake()
        {
            if(!Zones.Any()) throw new InvalidOperationException("There are no zones configured");
            if (Zones.Distinct(ZoneSettings.TypeComparer).Count() != Zones.Length)
                throw new InvalidOperationException("There is duplicate Zone Settings");
            if(LandNoiseSettings == null) throw new InvalidOperationException("There is no LandNoiseSettings defined");
            if (BlockSize * BlocksCount != Chunk.Size)
                throw new ArgumentException("Block size and blocks count invalid");

            SetSeed();

            _zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            foreach (var zoneSettings in Zones)
                _zoneSettingsLookup[(int)zoneSettings.Type] = zoneSettings;

            _zonesParent = new GameObject("Zones");

            Observer = FindObjectOfType<ObserverSettings>();

            Main = new Main(this, Observer, GetComponent<MesherSettings>());
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
            var oldZones = _zonesParent.transform.GetComponentsInChildren<Transform>().Where(t => t != _zonesParent.transform).ToArray();
            foreach (var zone in oldZones)
                Destroy(zone.gameObject);

            foreach (var zone in result)
            {
                var zoneHandleGO = new GameObject(zone.Type.ToString());
                zoneHandleGO.transform.position = new Vector3(zone.Center.x, 0, zone.Center.y);
                zoneHandleGO.transform.parent = _zonesParent.transform;
            }
        }

        #endregion
    }
}
