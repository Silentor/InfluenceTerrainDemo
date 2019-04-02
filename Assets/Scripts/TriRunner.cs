using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenToolkit.Mathematics;
using TerrainDemo.Generators;
using TerrainDemo.Generators.MapObjects;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Visualization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Input = TerrainDemo.Hero.Input;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Quaternion = OpenToolkit.Mathematics.Quaternion;

namespace TerrainDemo
{
    public class TriRunner : MonoBehaviour
    {
#region Generator

        [Header("Generator settings")]
        public int Seed;
        public bool RandomizeSeed;
        public float LandSize = 100;
        public float CellSide = 10;
        public float GridPerturbFreq = 0.01f;
        public float GridPerturbPower = 10;
        public float InfluencePerturbFreq = 0.25f;
        public float InfluencePerturbPower = 4;
        public BiomeSettings[] Biomes = new BiomeSettings[0];

        #endregion

#region Visualization

        [Header("Visualizer settings")]
        public Renderer.TerrainRenderMode RenderMode = Renderer.TerrainRenderMode.Terrain;
        public Renderer.TerrainLayerToRender RenderLayer = Renderer.TerrainLayerToRender.Main;

        #endregion

#region Macro

        [Header("Macro")]
        public Renderer.MacroCellInfluenceMode MacroCellInfluenceVisualization;
        public Material VertexColoredMat;

        #endregion

#region Micro

        [Header("Micro")]
        public Renderer.BlockTextureMode TextureMode;
        public Material TexturedMat;

        #endregion

        #region New region

        [Header("Actors")]
        public GameObject ActorPrefab;

        #endregion

        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        public MacroTemplate Land { get; private set; }

        public IReadOnlyCollection<BlockSettings> AllBlocks => _allBlocks;

        private Tools.Random _random;
        private Renderer _renderer;
        private BlockSettings[] _allBlocks;
        private ObjectMap _bridge;
        private Actor _hero;

        public void Render(TriRunner renderSettings)
        {
            var timer = Stopwatch.StartNew();

            _renderer.Clear();

            //Visualization
            if (renderSettings.RenderMode == Renderer.TerrainRenderMode.Macro)
                _renderer.Render(Macro, renderSettings);
            else
                //_renderer.Render(Micro, mode);
                _renderer.Render(Micro, renderSettings);               //Experimental renderer

            timer.Stop();

            Debug.LogFormat("Rendered all map in {0} msec", timer.ElapsedMilliseconds);
        }

        public void Generate()
        {
            Prepare();

            var template = new MacroTemplate(_random);
            Land = template;

            //Fully generate Macro Map
            Macro = template.CreateMacroMap(this);

            var microtimer = Stopwatch.StartNew();
            Micro = new MicroMap(Macro, this);

            foreach (var zone in Macro.Zones)
            {
                template.GenerateMicroZone2(Macro, zone, Micro);
            }
            Micro.GenerateHeightmap();

            microtimer.Stop();

            var laputaGenerator = new LaputaGenerator(4);
            var laputaData = laputaGenerator.Generate((-14, -14), 10);
            var laputa = new ObjectMap("Laputa", laputaData.bounds, Micro);
            laputa.SetHeights(laputaData.vertexPositions, laputaData.heightmap);
            laputa.SetBlocks(laputaData.blockPositions, laputaData.blockmap);
            Micro.AddChild(laputa);
            laputa.Snap();
            laputa.Changed += MicroOnChanged;

            var bridgeGenerator = new BridgeGenerator(20, 6, 6);
            var bridgeData = bridgeGenerator.Generate((0, 5), 2);
            var bridge = new ObjectMap("Bridge", bridgeData.bounds, Micro);
            bridge.SetHeights(bridgeData.vertexPositions, bridgeData.heightmap);
            bridge.SetBlocks(bridgeData.blockPositions, bridgeData.blockmap);
            Micro.AddChild(bridge);
            bridge.Snap();
            bridge.Changed += MicroOnChanged;

            var mountGenerator = new MountGenerator(10, 20, Micro);
            var mountData = mountGenerator.Generate((14, -16), 0);
            var mount = new ObjectMap("Mount", mountData.bounds, Micro);
            mount.SetHeights(mountData.vertexPositions, mountData.heightmap);
            mount.SetBlocks(mountData.blockPositions, mountData.blockmap);
            Micro.AddChild(mount);
            mount.Snap();
            mount.Changed += MicroOnChanged;

            _hero = new Actor(Micro, new Vector2(-14, 2), Quaternion.FromEulerAngles(0, MathHelper.DegreesToRadians(90), 0));
            Micro.AddActor(_hero);

            Micro.Changed += MicroOnChanged;

            Debug.LogFormat("Created micromap in {0} msec", microtimer.ElapsedMilliseconds);

            StartCoroutine(AnimateTest(bridge));
        }

        private void MicroOnChanged()
        {
            //Visualization
            Render(this);
        }

        private void Prepare()
        {
            if (RandomizeSeed)
                Seed = UnityEngine.Random.Range(0, int.MaxValue);

            _random = new Tools.Random(Seed);

            for (int i = 0; i < Biomes.Length; i++)
                Biomes[i].Index = i;
        }

        private IEnumerator AnimateTest(ObjectMap obj)
        {
            yield return new WaitForSeconds(1);
            var startTimeOffset = Time.time;

            while (true)
            {
                const float deltaTime = 0.1f;
                const float amplitude = 5;
                obj.Translate((Mathf.Sin(Time.time - startTimeOffset)) * deltaTime * 4);

                yield return new WaitForSeconds(deltaTime);
                //yield return null;

                //Debug.Break();  to take manual screenshots :)
            }
        }

        private void InputSourceOnStop()
        {
            _hero.Stop();
        }

        private void InputSourceOnMove(OpenToolkit.Mathematics.Vector2 direction)
        {
            _hero.Move(direction);
        }

        private void InputSourceOnStopRotating()
        {
            _hero.Rotate(0);
        }

        private void InputSourceOnRotate(float normalized)
        {
            _hero.Rotate(normalized);
        }

#region Unity

        void Awake()
        {
            Assert.raiseExceptions = true;
            _allBlocks = Resources.LoadAll<BlockSettings>("");
        }

        void Start()
        {
            Generate();

            _renderer = new Renderer(new Mesher(Macro, this), this);
            _renderer.AssingCamera(FindObjectOfType<ObserverController>().GetComponent<Camera>(), _hero);
            Render(this);

            var inputSource = FindObjectOfType<Input>();
            inputSource.Move += InputSourceOnMove;
            inputSource.StopMoving += InputSourceOnStop;
            inputSource.Rotate += InputSourceOnRotate;
            inputSource.StopRotating += InputSourceOnStopRotating;
        }

        private void OnValidate()
        {
            LandBounds = new Box2(-LandSize / 2, LandSize / 2, LandSize / 2, -LandSize / 2);
        }

        private void Update()
        {
            _hero?.Update(Time.deltaTime);
        }
#endregion
    }
}
