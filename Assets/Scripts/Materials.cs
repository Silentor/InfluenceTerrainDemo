using UnityEngine;

namespace TerrainDemo
{
    public class Materials : MonoBehaviour
    {
        public Material Grass;
        public ComputeShader TextureBlendShader;

        public static Materials Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<Materials>();

                return _instance;
            }
        }

        private static Materials _instance;
    }
}
