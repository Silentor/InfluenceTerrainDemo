using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Common texture pool for chunk mesh generator and chunk GOs
    /// </summary>
    public class TexturePool
    {
        public RenderTexture Get(int size)
        {
            RenderTexture renderTex2;
            if (_pool.Count > 0)
            {
                renderTex2 = _pool.Last();
                _pool.RemoveAt(_pool.Count - 1);
            }
            else
            {
                renderTex2 = new RenderTexture(size, size, 0);
                renderTex2.useMipMap = true;
                renderTex2.generateMips = true;
                renderTex2.wrapMode = TextureWrapMode.Clamp;
            }

            renderTex2.Create();
            return renderTex2;
        }

        public void Put(RenderTexture texture)
        {
            texture.Release();
            _pool.Add(texture);
        }

        private readonly List<RenderTexture> _pool = new List<RenderTexture>();
    }
}
