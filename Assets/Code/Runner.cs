using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Code.Generators;
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
        private Land _land;


        void Start()
        {
            Generate();
        }

        public void Generate()
        {
            var time = Stopwatch.StartNew();

            _land = new Land(ZonesCount, Zones, WorldSize, BlocksCount * BlockSize, IDWCoeff, WorldSettings);
            _land.Generate();

            var land = new Dictionary<Vector2i, Chunk>();

            //Generate land's chunks
            var landGenerator = new LandGenerator(_land);
            landGenerator.Generate(land);

            //Generate land's meshes
            var mesher = new InfluenceMesher(Zones);
            foreach (var chunk in land)
            {
                var mesh = mesher.Generate(chunk.Value);
                ChunkGO.Create(chunk.Value, mesh);
            }

            time.Stop();
            Debug.Log(_land.GetStaticstics());
            Debug.Log(string.Format("Total {0} ms", time.ElapsedMilliseconds));
        }

        void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && _land != null && _land.Zones != null)
                foreach (var zone in _land.Zones)
                {
                    if (zone.Type != ZoneType.Empty)
                    {
                        Gizmos.color = Zones[(int) zone.Type].LandColor;
                        Gizmos.DrawSphere(new Vector3(zone.Center.x, 50, zone.Center.y), 10);
                    }
                }
        }

    }
}
