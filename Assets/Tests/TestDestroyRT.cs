using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Tests
{
    public class TestDestroyRT : MonoBehaviour
    {

        // Use this for initialization
        IEnumerator Start ()
        {
            var rtList = new List<RenderTexture>();

            yield return StartCoroutine(Create(rtList));
            yield return StartCoroutine(Dispose(rtList));
            yield return StartCoroutine(ReCreate(rtList));
            yield return StartCoroutine(Dispose(rtList));
        }

        private IEnumerator Create(List<RenderTexture> list)
        {
            for (int i = 0; i < 100; i++)
            {
                //Cant reuse texture if drawing many textures for one frame
                var renderTexture = new RenderTexture(1024, 1024, 0);
                renderTexture.wrapMode = TextureWrapMode.Clamp;
                renderTexture.enableRandomWrite = true;
                renderTexture.useMipMap = false;
                renderTexture.autoGenerateMips = false;
                renderTexture.Create();
                list.Add(renderTexture);

                yield return null;
            }
        }

        private IEnumerator ReCreate(List<RenderTexture> list)
        {
            for (int i = 0; i < 100; i++)
            {
                list[i].Create();

                yield return null;
            }
        }

        private IEnumerator Dispose(List<RenderTexture> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Release();

                yield return null;
            }
        }
    }
}
