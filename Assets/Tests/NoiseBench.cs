using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Tests
{
    public class NoiseBench : MonoBehaviour
    {
        IEnumerator Start()
        {
            //var open = new OpenSimplexNoise();            
            var fast = new FastNoise();
            const int iter = 1000000;

            //Warm up
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                //open.Evaluate(i, i);
                //open.Evaluate(i, i, i);
                Mathf.PerlinNoise(i, i);
                fast.GetSimplex(i, i);
                fast.GetSimplex(i, i, i);
            }

            yield return new WaitForSeconds(2);

            float resultFloat = 0f;
            timer.Stop();
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultFloat += Mathf.PerlinNoise(i, i);

            timer.Stop();

            Debug.Log("Unity 2D Perlin " + timer.ElapsedMilliseconds); //Unity 2D Perlin 41

            double resultDouble = 0d;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetPerlin(i, i);

            timer.Stop();

            Debug.Log("Fast 2D Perlin " + timer.ElapsedMilliseconds);  //Fast 2D Perlin 204

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetSimplex(i, i);

            timer.Stop();

            Debug.Log("Fast 2D simplex " + timer.ElapsedMilliseconds);  //Fast 2D simplex 139

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            for (int i = 0; i < iter; i++)
                resultDouble += fast.GetSimplex(i, i, i);

            timer.Stop();

            Debug.Log("Fast 3D simplex " + timer.ElapsedMilliseconds); //Fast 3D simplex 195

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            //for (int i = 0; i < iter; i++)
            //resultDouble += open.Evaluate(i, i);

            timer.Stop();

            Debug.Log("Open 2D simplex " + timer.ElapsedMilliseconds);  //Open 2D simplex 157

            resultDouble = 0f;
            timer.Reset();
            timer.Start();

            //for (int i = 0; i < iter; i++)
            //resultDouble += open.Evaluate(i, i, i);

            timer.Stop();

            Debug.Log("Open 3D simplex " + timer.ElapsedMilliseconds);  //Open 3D simplex 263
        }
    }
}
