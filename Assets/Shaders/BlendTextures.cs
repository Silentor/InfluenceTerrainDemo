using UnityEngine;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class BlendTextures : MonoBehaviour
{
    public ComputeShader shader;
    public Texture2D Mask;
    public Texture2D InputTexture1;
    public Texture2D InputTexture2;

    private RenderTexture tex;

    void Start()
    {
        var timer = Stopwatch.StartNew();

        tex = new RenderTexture(1024, 1024, 0);
        tex.enableRandomWrite = true;
        tex.Create();

        shader.SetTexture(0, "mask", Mask);
        shader.SetTexture(0, "input1", InputTexture1);
        shader.SetTexture(0, "input2", InputTexture2);
        shader.SetTexture(0, "tex", tex);
        shader.Dispatch(0, tex.width / 8, tex.height / 8, 1);

        timer.Stop();
        Debug.Log(timer.ElapsedTicks);
    }

    void OnGUI()
    {
        int w = Screen.width / 2;
        int h = Screen.height / 2;
        int s = 512;

        GUI.DrawTexture(new Rect(w - s / 2, h - s / 2, s, s), tex);
    }

    void OnDestroy()
    {
        tex.Release();
    }
}