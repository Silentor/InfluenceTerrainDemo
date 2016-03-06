using System;
using UnityEngine;

namespace TerrainDemo.Settings
{
    [Serializable]
    public struct BlockRenderSettings
    {
        public BlockType Block;
        public Color Color;

        public TextureSettings FlatTexture;
        public TextureSettings SteepTexture;

        [Header("Triplanar")]
        public bool BypassTriplanar;
        public Vector2 SteepAngles;
    }

    [Serializable]
    public struct TextureSettings
    {
        public Texture2D Texture;

        [Header("Mix")]
        public bool BypassMix;
        public Texture2D MixTexture;
        public float MixNoiseScale;
        public float MixTextureScale;
        public float MixTextureAngle;

        [Header("Tint")]
        public bool BypassTint;
        public Color TintFrom;
        public Color TintTo;
        public float TintNoiseScale;
    }
}
