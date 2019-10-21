using TerrainDemo.Micro;
using UnityEngine;

namespace TerrainDemo.Settings
{
    [CreateAssetMenu(fileName = "Biome.asset", menuName = "Tri/Biome")]
    public class BiomeSettings : ScriptableObject
    {
        public BiomeType Type;

        /// <summary>
        /// Optimize influence calculations
        /// </summary>
        public int Index { get; set; }

        public Color LayoutColor = Color.magenta;

        /// <summary>
        /// Biome size at cells
        /// </summary>
        public Vector2Int SizeRange = new Vector2Int(5, 20);

        //Add some settings about general biome look and feel
        public BlockSettings DefaultMainBlock;
        public BlockSettings DefaultUndergroundBlock;

        public override string ToString()
        {
            return $"Biome {name}";
        }
    }

    public enum BiomeType
    {
        Plains,
        Mountain,
        Hills,
        Desert,
        Lake,
        Snow,
        Caves,
        Forest,
        

        TestBegin = TestFlat,
        TestFlat = 100,
        TestBulge,
        TestPit,
        TestOnlyMacroLow,                   //Cell Macro height = 0, do not modify macro height
        TestOnlyMacroHigh,                  //Cell Macro height ~ 10, do not modify macro height
        TestWaves,
        TestBaseOreAndGround,
        TestBaseCavesAndGround,
        TestBaseOreGroundColumn,
        TestBaseOrePyramidGround,
        TestTunnel,
        TestNavigation,
        TestEnd = TestNavigation
    }
}
