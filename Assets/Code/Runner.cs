using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets.Code
{
    public class Runner : MonoBehaviour
    {
        [Header("World settings")] 
        [Range(1, 100)]
        public int ZonesCount = 16;
        public int WorldSize = 16;
        public WorldSettings WorldSettings;

        [Header("Zones settings")]
        public ZoneSettings[] Zones;
        public float IDWCoeff = 2;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;
        public bool Interpolate = true;


        void Start()
        {
            Init();
            Generator.Init(ZonesCount);

            Generate();
        }

        void Init()
        {
            Generator.Zones = Zones;
            Generator.WorldSize = WorldSize;
            Generator.BlocksCount = BlocksCount;
            Generator.BlockSize = BlockSize;
            Generator.IDWCoeff = IDWCoeff;
            Generator.World = WorldSettings;

            Mesher.Zones = Zones;
        }

        public void Generate()
        {
            StartCoroutine(GenerateCoroutine());
        }

        private IEnumerator GenerateCoroutine()
        {
            var time = Stopwatch.StartNew();
            Init();

            Main.FillMap(WorldSize, Interpolate);
            //yield return StartCoroutine(Main.FillMapAsync(WorldSize, Interpolate));
            
            time.Stop();
            Debug.Log(Generator.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));
            yield break;
        }

        private static IEnumerator Empty()
        {
            yield break;
        }

        void OnDrawGizmosSelected()
        {
            if(Application.isPlaying && Generator.Zones2 != null)
                foreach (var zone in Generator.Zones2)
                {
                    Gizmos.color = Zones[zone.Id].LandColor;
                    Gizmos.DrawSphere(new Vector3(zone.Position.x, 50, zone.Position.y), 10);
                }
        }

    }
}
