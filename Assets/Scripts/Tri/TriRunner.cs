using System;
using System.Linq;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using UnityEngine;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tri
{
    public class TriRunner : MonoBehaviour
    {
        [Header("Macro settings")]
        public int Seed;
        public bool RandomizeSeed;
        public float LandSize = 100;
        public float Side = 10;
        public float CellPerturbance = 2;
        public TriBiome[] Biomes = new TriBiome[0];

        [Header("Visualizer settings")]
        public LandRenderMode LandRender_Mode;
        public TriRenderer.MacroCellInfluenceMode MacroCellInfluenceVisualization;
        public TriRenderer.MacroCellReliefMode MacroCellReliefVisualization;
        public TriRenderer.MicroInfluenceRenderMode MicroInfluenceRenderMode;
        public Material VertexColoredMat;


        public Box2 LandBounds { get; private set; }

        public MacroMap Macro { get; private set; }
        public MicroMap Micro { get; private set; }

        private Tools.Random _random;

        void Awake()
        {
            if (RandomizeSeed)
                Seed = UnityEngine.Random.Range(0, int.MaxValue);

            _random = new Tools.Random(Seed);

            for (int i = 0; i < Biomes.Length; i++)
                Biomes[i].Index = i;
        }

        void Start()
        {
            var template = new MacroTemplate(_random);

            //Fully generate Macro Map
            Macro = template.CreateMacroMap(this);

            Micro = new MicroMap(Macro, this);

            foreach (var zone in Macro.Zones)
            {
                template.GenerateMicroZone(zone, Micro);
            }

            //Visualization
            var renderer = new TriRenderer(this, new TriMesher(Macro, this));
            if(LandRender_Mode == LandRenderMode.Macro)
                renderer.Render(Macro, MacroCellInfluenceVisualization, MacroCellReliefVisualization);
            else
                renderer.Render(Micro, MicroInfluenceRenderMode);
            
            /*
            foreach (var cell in Macro.Cells)
            {
                renderer.Render(cell);
            }
            */
            
            //renderer.Render(Mesh);


            //Estimate macro height function quality
            //Узнаем, на сколько отличается функция высоты от заданных высот ячеек
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

            Debug.LogFormat("Average diff {0}, max diff {1} on cell {2}", averageDiff, maxDiff, maxDiffCell?.Position);
        }

        private void OnValidate()
        {
            LandBounds = new Box2(-LandSize / 2, LandSize / 2, LandSize / 2, -LandSize / 2);
        }

        public enum LandRenderMode
        {
            Macro,
            Micro
        }
    }
}
