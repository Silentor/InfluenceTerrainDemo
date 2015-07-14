using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Runner : MonoBehaviour
    {
        [Header("World settings")] 
        [Range(1, 100)]
        public int ZonesCount = 16;
        public int WorldSize = 16;

        [Header("Chunk settings")]
        [Range(1, 128)]
        public int BlocksCount = 16;
        [Range(1, 128)]
        public int BlockSize = 1;

        [Header("Zones settings")] 
        public GeneratorSettings[] Zones;
        public float IDWCoeff = 2;

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

            Mesher.Zones = Zones;
        }

        public void Generate()
        {
            Init();

            Main.FillMap(WorldSize);
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
