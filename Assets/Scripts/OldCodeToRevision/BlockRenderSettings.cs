using System;
using TerrainDemo.Micro;
using UnityEngine;

namespace TerrainDemo.OldCodeToRevision
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

        [Header("Tint")]
        public bool BypassTint;
        public Color TintFrom;
        public Color TintTo;
        public float TintNoiseScale;
    }

    [Serializable]
    public struct TextureSettings
    {
        public Texture2D Texture;
        public Texture2D Normals;

        [Header("Mix")]
        public bool BypassMix;
        //public Texture2D MixTexture;
        public float MixNoiseScale;
        public float MixTextureScale;
        public float MixTextureAngle;
    }
}
