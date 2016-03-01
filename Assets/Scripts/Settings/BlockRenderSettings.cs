using System;
using UnityEngine;

namespace TerrainDemo.Settings
{
    [Serializable]
    public struct BlockRenderSettings
    {
        public BlockType Block;
        public Color Color;
        public Texture2D FlatTexture;
        public Texture2D TextureNrm;
        public Texture2D SteepTexture;
        public Vector2 SteepAngles;
        public Texture2D Texture2Nrm;
    }
}
