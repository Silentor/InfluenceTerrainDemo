using System;
using UnityEngine;

namespace TerrainDemo.Settings
{
    [Serializable]
    public struct BlockColors
    {
        public BlockType Block;
        public Color Color;
        public Texture2D Texture;
    }
}
