using System;
using System.Diagnostics;
using System.Linq;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Visualization;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Debug = UnityEngine.Debug;
using Renderer = TerrainDemo.Visualization.Renderer;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tri
{
    public class TriRunner : MonoBehaviour
    {
        [Header("Generator settings")]
        public int Seed;
        public bool RandomizeSeed;
        public float LandSize = 100;
        public float Side = 10;
        public float InfluencePerturbFreq = 0.25f;
        public float InfluencePerturbPower = 4;
        public BiomeSettings[] Biomes = new BiomeSettings[0];

        [Header("Visualizer settings")]
        public LandRenderMode LandRender_Mode;
        [Header("Macro")]
        public Renderer.MacroCellInfluenceMode MacroCellInfluenceVisualization;
        public Renderer.MacroCellReliefMode MacroCellReliefVisualization;
        public Material VertexColoredMat;
        [Header("Micro")]
        public Material TexturedMat;
        public BlockSettings BaseBlock;

        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        public MacroTemplate Land { get; private set; }

        private Tools.Random _random;
        private Renderer _renderer;

        public void Render(Renderer.MicroRenderMode mode)
        {
            var timer = Stopwatch.StartNew();

            _renderer.Clear();

            //Visualization
            if (LandRender_Mode == LandRenderMode.Macro)
                _renderer.Render(Macro, MacroCellInfluenceVisualization, MacroCellReliefVisualization);
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
            Debug.LogFormat("Created micromap in {0} msec", microtimer.ElapsedMilliseconds);

            //Visualization
            _renderer?.Clear();
            _renderer = new Renderer(this, new Mesher(Macro, this));
            Render(Renderer.MicroRenderMode.Default);

            //Estimate macro height function quality
            //Узнаем, на сколько отличается функция макровысоты от заданных высот ячеек
            float maxDiff = 0, averageDiff = 0;
            Cell maxDiffCell = null;
            foreach (var macroCell in Macro.Cells)
            {
                var heightDiff = macroCell.Height - Macro.GetHeight(macroCell.Center);
                if (Mathf.Abs(maxDiff) < Mathf.Abs(heightDiff))
                {
                    maxDiffCell = macroCell;
                    maxDiff = heightDiff;
                }

                averageDiff += heightDiff;
            }

            averageDiff /= Macro.Cells.Count;

            Debug.LogFormat("Average diff {0}, max diff {1} on cell {2}", averageDiff, maxDiff, maxDiffCell?.Coords);
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


        void Start()
        {
            Generate();
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
