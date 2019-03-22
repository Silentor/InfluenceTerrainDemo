using TerrainDemo.Micro;
using UnityEngine;

namespace TerrainDemo.Settings
{
    [CreateAssetMenu(fileName = "Block.asset", menuName = "Tri/Block")]
    public class BlockSettings : ScriptableObject
    {
        public BlockType Block;
        public Color DefaultColor = Color.white;
        [Range(0, 1)]
        public float SpeedModifier = 1;
    }
}
