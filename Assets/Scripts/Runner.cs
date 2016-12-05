using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Hero;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TerrainDemo
{
    [RequireComponent(typeof(LandSettings))]
    public class Runner : MonoBehaviour
    {
        public LandSettings LandSettings { get; private set; }

        public Main Main { get; private set; }

        public IObserver Observer { get; private set; }

        /// <summary>
        /// Generate layout, map and mesh
        /// </summary>
        public void GenerateLayout()
        {
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

        //private Bounds2i _landSizeChunks;
        private GameObject _zonesParent;


        #region Unity

        void Awake()
        {
            //SeedTest();
            //NoiseBenchmark();

            //_zoneSettingsLookup = new ZoneSettings[(int)Zones.Max(z => z.Type) + 1];
            //foreach (var zoneSettings in Zones)
            //    _zoneSettingsLookup[(int)zoneSettings.Type] = zoneSettings;

            _zonesParent = new GameObject("Zones");

            Observer = FindObjectOfType<ObserverSettings>();

            LandSettings = GetComponent<LandSettings>();
            var mesherSettings = GetComponent<MesherSettings>();

            Main = new Main(LandSettings, Observer, mesherSettings);
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

        private void SeedTest()
        {
            var fast = new FastNoise(0);
            for (int i = 0; i < 10; i++)
            {
                fast.SetSeed(i);
                Debug.LogFormat("Seed {0}, value {1}", i, fast.GetSimplex(1000.3, 1000.3));
            }
        }

        private void NoiseBenchmark()
        {
            //var open = new OpenSimplexNoise();
            var fast = new FastNoise();
            const int iter = 1000000;

            //Warm up
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                //open.Evaluate(i, i);
                //open.Evaluate(i, i, i);
                Mathf.PerlinNoise(i, i);
                fast.GetSimplex(i, i);
                fast.GetSimplex(i, i, i);
            }

            float resultFloat = 0f;
            timer.Stop();
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultFloat += Mathf.PerlinNoise(i, i);

            timer.Stop();

            Debug.Log("Unity 2D Perlin " + timer.ElapsedMilliseconds); //Unity 2D Perlin 41

            double resultDouble = 0d;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetPerlin(i, i);

            timer.Stop();

            Debug.Log("Fast 2D Perlin " + timer.ElapsedMilliseconds);  //Fast 2D Perlin 204

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetSimplex(i, i);

            timer.Stop();

            Debug.Log("Fast 2D simplex " + timer.ElapsedMilliseconds);  //Fast 2D simplex 139

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetSimplex(i, i, i);

            timer.Stop();

            Debug.Log("Fast 3D simplex " + timer.ElapsedMilliseconds); //Fast 3D simplex 195

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            //for (int i = 0; i < iter; i++)
                //resultDouble += open.Evaluate(i, i);

            timer.Stop();

            Debug.Log("Open 2D simplex " + timer.ElapsedMilliseconds);  //Open 2D simplex 157

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            //for (int i = 0; i < iter; i++)
                //resultDouble += open.Evaluate(i, i, i);

            timer.Stop();

            Debug.Log("Open 3D simplex " + timer.ElapsedMilliseconds);  //Open 3D simplex 263
        }

    }
}
