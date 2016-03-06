using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Settings
{
    public class MesherSettings : MonoBehaviour
    {
        public int TextureSize = 1024;
        public int MaskBorder = 2;
        public float Turbulence = 0.1f;
        public Texture2D NoiseTexture;
        public Material Material;

        public BlockRenderSettings[] Blocks;

        public ComputeShader TextureBlendShader;
        public ComputeShader TriplanarTextureShader;
        public ComputeShader MixTintShader;
    }
}
