using System;
using System.Collections.Generic;
using TerrainDemo.Meshing;
using UnityEngine;

namespace TerrainDemo.Settings
{
    public class MesherSettings : MonoBehaviour
    {
        public MesherType Type = MesherType.Textured;

        public bool BypassHeightMap;
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

        public BaseMesher CreateMesher(ILandSettings settings)
        {
            switch (Type)
            {
                case MesherType.BlockColor:
                    return new ColorMesher(settings, this);

                case MesherType.Influence:
                    return new InfluenceMesher(settings, this);

                case MesherType.Textured:
                    return new TextureMesher(settings, this);

                default:
                    throw new NotImplementedException(string.Format("Mesher of type {0} is not implemented", Type));
            }
        }

        public enum MesherType
        {
            BlockColor,
            Influence,
            Textured
        }
    }
}
