using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenTK;
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
using Quaternion = UnityEngine.Quaternion;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;

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

        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        public MacroTemplate Land { get; private set; }

        public IReadOnlyCollection<BlockSettings> AllBlocks => _allBlocks;

        private Tools.Random _random;
        private Renderer _renderer;
        private BlockSettings[] _allBlocks;
        private ObjectMap _bridge;

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
                template.GenerateMicroZone(Macro, zone, Micro);
            }
            Micro.GenerateHeightmap();

            microtimer.Stop();

            _bridge = CreateBridgeObj();
            CreateLaputeObj();
            CreateTestBlockObj();

            Micro.Changed += MicroOnChanged;

            Debug.LogFormat("Created micromap in {0} msec", microtimer.ElapsedMilliseconds);
        }

        private ObjectMap CreateLaputeObj()
        {
            var positions = new List<Vector2i>();
            var blocks = new List<Blocks>();

            Vector2i laputaCenter = (-14, -14);
            float laputaHeight = 10f;
            int laputaRadius = 4;
            for (int x = laputaCenter.X - laputaRadius - 1; x < laputaCenter.X + laputaRadius + 1; x++)
                for (int z = laputaCenter.Z - laputaRadius - 1; z < laputaCenter.Z + laputaRadius + 1; z++)
                {
                    var pos = new Vector2i(x, z);
                    if (Vector2.Distance(pos, laputaCenter) < laputaRadius)
                    {
                        positions.Add(pos);

                        var mainHeight = Mathf.Sqrt(laputaRadius * laputaRadius - Vector2.DistanceSquared(laputaCenter, pos));
                        mainHeight = Mathf.Max(mainHeight - 2, 0.25f);

                        blocks.Add(new Blocks(BlockType.Grass, BlockType.Empty,
                            new Heights(laputaHeight + mainHeight / 2, laputaHeight - mainHeight)));
                    }
                }

            var laputaObj = new ObjectMap("Laputa",
                new Bounds2i((positions.Select(p => p.X).Min(), positions.Select(p => p.Z).Min()),
                    (positions.Select(p => p.X).Max(), positions.Select(p => p.Z).Max())), Micro);
            laputaObj.SetBlocks(positions, blocks, false);
            Micro.AddChild(laputaObj);
            laputaObj.GenerateHeightmap();
            laputaObj.Changed += MicroOnChanged;
            return laputaObj;
        }

        private ObjectMap CreateBridgeObj()
        {
            var positions = new List<Vector2i>();
            var blocks = new List<Blocks>();

            const int xStart = -10, xFinish = 10, width = 5;
            const int xCenter = (xStart + xFinish) / 2;
            const int length = xFinish - xStart;

            var transMatrix = OpenTK.Matrix4.CreateRotationY(MathHelper.DegreesToRadians(0));

            for (int x = xStart; x <= xFinish; x++) //length
            {
                for (int z = 0; z <= width; z++) //width
                {
                    //Create pulse from two smoothsteps
                    var stairwayBlockHeight =
                        (Interpolation.SmoothStep(Mathf.InverseLerp(xStart, xCenter, x))
                         - Interpolation.SmoothStep(Mathf.InverseLerp(xCenter, xFinish, x))) * 5 + 2;

                    if (z == 0 || z == width)
                        stairwayBlockHeight -= 0.5f;

                    var baseBlockHeight = stairwayBlockHeight - 2;
                    if (z == 0 || z == width)
                        baseBlockHeight += 0.5f;

                    blocks.Add(new Blocks(BlockType.Sand, BlockType.Empty,
                        new Heights(stairwayBlockHeight, baseBlockHeight, baseBlockHeight)));

                    var transPos = new OpenTK.Vector4(x, 1, z, 1);
                    transPos = transPos * transMatrix;
                    positions.Add(new Vector2i(Mathf.RoundToInt(transPos.X), Mathf.RoundToInt(transPos.Z)));
                }
            }

            var bridgeObj = new ObjectMap("Bridge",
                new Bounds2i((positions.Select(p => p.X).Min(), positions.Select(p => p.Z).Min()),
                    (positions.Select(p => p.X).Max(), positions.Select(p => p.Z).Max())), Micro);
            bridgeObj.SetBlocks(positions, blocks, false);
            Micro.AddChild(bridgeObj);
            bridgeObj.GenerateHeightmap();
            bridgeObj.Changed += MicroOnChanged;
            return bridgeObj;
        }

        private ObjectMap CreateTestBlockObj()
        {
            Vector2i position = (10, 10);
            var mainBlock = Micro.GetBlockRef(position);
            var testBlock = new Blocks(BlockType.Stone, BlockType.Empty, 
                new Heights(mainBlock.Height.Main - 3, mainBlock.Height.Main - 5));

            var testObj = new ObjectMap("TestBlock", new Bounds2i(position, position), Micro);
            testObj.SetBlocks(new []{position}, new []{ testBlock }, false);
            Micro.AddChild(testObj);
            testObj.GenerateHeightmap();
            testObj.Changed += MicroOnChanged;
            return testObj;
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

        private IEnumerator AnimateTest()
        {
            yield return new WaitForSeconds(1);
            var startTimeOffset = Time.time;

            while (true)
            {
                const float deltaTime = 0.1f;
                const float amplitude = 5;
                _bridge.Translate((Mathf.Sin(Time.time - startTimeOffset)) * deltaTime * 4);

                yield return new WaitForSeconds(deltaTime);
                //yield return null;

                //Debug.Break();  to take manual screenshots :)
            }
        }

        private void BenchRaycast()
        {
            //Old time - 133-137 msec for 1000 raycasts
            //New time - 81-83 msec for 1000 raycasts

            var timer = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                //Test ray simulates user input from mouse
                var testRay = new Ray(
                    new UnityEngine.Vector3(0, 10, 0),
                    Quaternion.AngleAxis(UnityEngine.Random.Range(0, 359f), UnityEngine.Vector3.up) 
                    * Quaternion.AngleAxis(UnityEngine.Random.Range(-45, 45f), UnityEngine.Vector3.right)
                    * UnityEngine.Vector3.forward);

                var hit = Micro.RaycastHeightmap(testRay);
                if (hit.HasValue)
                {
                    Debug.DrawLine(testRay.origin, hit.Value.hitPoint, Color.green, 10, true);
                }
                else
                    Debug.DrawLine(testRay.origin, testRay.GetPoint(100), Color.red, 10, true);
            }

            timer.Stop();

            Debug.Log($"Benchmark time {timer.ElapsedMilliseconds}");
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
            Render(this);

            //StartCoroutine(AnimateTest());
            //BenchRaycast();
        }

        private void OnValidate()
        {
            LandBounds = new Box2(-LandSize / 2, LandSize / 2, LandSize / 2, -LandSize / 2);
        }
#endregion
    }
}
