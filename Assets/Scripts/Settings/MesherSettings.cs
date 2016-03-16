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

        /// <summary>
        /// Material for textured mesher
        /// </summary>
        public Material TexturedMaterial;

        /// <summary>
        /// Material for vertex colored simple meshers
        /// </summary>
        public Material VertexColoredMaterial;


        public BlockRenderSettings[] Blocks;

        public ComputeShader TextureBlendShader;
        public ComputeShader BlockShader;
    }
}
