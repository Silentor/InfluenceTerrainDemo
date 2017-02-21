using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public void SaveMesh(string path)
        {
            var data = Main.SaveCellMesh();

            using (var file = File.CreateText(path))
            {
                file.Write(data);
            }

            Debug.Log("Write cell mesh to " + path);
        }

        public void LoadMesh(string path)
        {
            string data;
            using (var file = File.OpenText(path))
            {
                data = file.ReadToEnd();
            }

            Main.LoadCellMesh(data);

            Debug.Log("Read cell mesh from " + path);
        }

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
        
    }
}
