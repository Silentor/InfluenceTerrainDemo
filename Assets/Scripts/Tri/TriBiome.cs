using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tri
{
    [CreateAssetMenu(fileName = "Biome.asset", menuName = "Tri/Biome")]
    public class TriBiome : ScriptableObject
    {
        /// <summary>
        /// Optimize influence calculations
        /// </summary>
        public int Index { get; set; }

        public Color LayoutColor = Color.magenta;
        /// <summary>
        /// Biome size at cells
        /// </summary>
        public Vector2Int SizeRange = new Vector2Int(5, 20);

        public ReliefType Relief;

        //Add some settings about general biome look and feel

        public enum ReliefType
        {
            Unknown,
            Water,
            Mountains,
            Plains
        }
    }
}
