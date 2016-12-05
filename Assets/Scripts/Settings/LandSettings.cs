using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using UnityEngine;

namespace TerrainDemo.Settings
{
    public class LandSettings : MonoBehaviour
    {
        [Header("Layout settings")]
        [Tooltip("In chunks")]
        public int LandSize = 20;
        public bool RegenerateSeed;
        public int Seed;
        public double GlobalHeightFreq = 1/200.0;
        public double GlobalHeightAmp = 200.0/4;
        public Vector2 ZonesDensity = new Vector2(30, 40);
        public ZoneSettings[] Zones = new ZoneSettings[0];
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

        [Header("Map settings")]
        public bool BypassHeight;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;

        public GameObject Tree;

        public GameObject Stone;

        /// <summary>
        /// Land bounds in chunks
        /// </summary>
        public Bounds2i LandBounds { get; private set; }

        /// <summary>
        /// Get zone settings for zone type
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ZoneSettings this[ZoneType index]
        {
            get { return Zones.First(zt => zt.Type == index); }
        }

        public LayoutGenerator CreateLayoutGenerator()
        {
            switch (LayoutGenerator)
            {
                case Generators.LayoutGenerator.Type.PoissonTwoSide:
                    return new PoissonTwoSideLayoutGenerator(this);

                case Generators.LayoutGenerator.Type.PoissonClustered:
                    return new PoissonClusteredLayoutGenerator(this);

                default:
                    throw new NotImplementedException(string.Format("Layout generator type {0} is not defined", LayoutGenerator));
            }
        }

        public void SetSeed()
        {
            if (RegenerateSeed)
                Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(Seed);
        }

        void Awake()
        {
            if (!Zones.Any()) throw new InvalidOperationException("There are no zones configured");
            if (Zones.Distinct(ZoneSettings.TypeComparer).Count() != Zones.Length)
                throw new InvalidOperationException("There is duplicate Zone Settings");
            if (BlockSize * BlocksCount != Chunk.Size)
                throw new ArgumentException("Block size and blocks count invalid");

            SetSeed();
        }

        void OnValidate()
        {
            if (LandSize < 1) LandSize = 1;

            //Update Land bounds
            var landMin = -LandSize / 2;
            var landMax = landMin + LandSize - 1;
            var minChunkBounds = Chunk.GetBounds(new Vector2i(landMin, landMin));
            var maxChunkBounds = Chunk.GetBounds(new Vector2i(landMax, landMax));

            LandBounds = new Bounds2i(minChunkBounds.Min, maxChunkBounds.Max);
        }

    }
}