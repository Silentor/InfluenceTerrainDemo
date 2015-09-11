using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Code.Generators;
using Assets.Code.Layout;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        public bool InterpolateInfluence = true;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;

        public void CreateLand()
        {
            _land = MakeLayout();
            Generate(_land);
        }

        public Land MakeLayout()
        {
            var layouter = new LandLayouter();
            return layouter.CreateLand(this);
        }

        public void Generate(Land land)
        {
            var time = Stopwatch.StartNew();

            var landMap = new Dictionary<Vector2i, Chunk>();

            //Generate land's chunks
            var landGenerator = new LandGenerator(land, this);
            landGenerator.Generate(landMap);

            //Generate land's meshes
            var mesher = new InfluenceMesher(this);
            foreach (var chunk in landMap)
            {
                var mesh = mesher.Generate(chunk.Value);
                ChunkGO.Create(chunk.Value, mesh);
            }

            time.Stop();
            Debug.Log(_land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));
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
        bool ILandSettings.InterpolateInfluence { get { return InterpolateInfluence; } }

        private Land _land;
        private Bounds2i _landSizeChunks;
        private ZoneSettings[] _zoneSettingsLookup;

        void Awake()
        {
            _landSizeChunks = new Bounds2i(Vector2i.Zero, LandSizeChunks);
            _zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            foreach (var zoneSettings in Zones)
                _zoneSettingsLookup[(int) zoneSettings.Type] = zoneSettings;
        }

        void Start()
        {
            CreateLand();
        }

        void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && _land != null && _land.Zones != null)
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
