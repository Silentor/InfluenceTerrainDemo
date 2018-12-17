using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Visualization;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Renderer = TerrainDemo.Visualization.Renderer;

namespace TerrainDemo
{
    public class TriRunner : MonoBehaviour
    {
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

        [Header("Visualizer settings")]
        public LandRenderMode LandRender_Mode;
        [Header("Macro")]
        public Renderer.MacroCellInfluenceMode MacroCellInfluenceVisualization;
        //[Obsolete]
        //public Renderer.MacroCellReliefMode MacroCellReliefVisualization;
        public Material VertexColoredMat;
        [Header("Micro")]
        public Material TexturedMat;
        public BlockSettings BaseBlock;

        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        public MacroTemplate Land { get; private set; }

        public IReadOnlyCollection<BlockSettings> AllBlocks => _allBlocks;

        private Tools.Random _random;
        private Renderer _renderer;
        private BlockSettings[] _allBlocks;

        public void Render(Renderer.MicroRenderMode mode)
        {
            var timer = Stopwatch.StartNew();

            _renderer.Clear();

            //Visualization
            if (LandRender_Mode == LandRenderMode.Macro)
                _renderer.Render(Macro, MacroCellInfluenceVisualization);
            else
                _renderer.Render(Micro, mode);

            timer.Stop();

            Debug.LogFormat("Rendered in {0} msec", timer.ElapsedMilliseconds);
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

            microtimer.Stop();

            Micro.Changed += MicroOnChanged;

            Debug.LogFormat("Created micromap in {0} msec", microtimer.ElapsedMilliseconds);
        }

        private void MicroOnChanged()
        {
            //Visualization
            Render(Renderer.MicroRenderMode.Default);
        }

        private void Prepare()
        {
            if (RandomizeSeed)
                Seed = UnityEngine.Random.Range(0, int.MaxValue);

            _random = new Tools.Random(Seed);

            for (int i = 0; i < Biomes.Length; i++)
                Biomes[i].Index = i;
        }

        #region Unity

        void Awake()
        {
            _allBlocks = Resources.LoadAll<BlockSettings>("");
        }

        void Start()
        {
            Generate();

            _renderer = new Renderer(this, new Mesher(Macro, this));
            Render(Renderer.MicroRenderMode.Default);
        }

        private void OnValidate()
        {
            LandBounds = new Box2(-LandSize / 2, LandSize / 2, LandSize / 2, -LandSize / 2);
        }

        #endregion

        public enum LandRenderMode
        {
            Macro,
            Micro
        }
    }
}
