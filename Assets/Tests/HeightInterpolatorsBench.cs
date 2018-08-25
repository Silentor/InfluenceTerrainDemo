using System.Collections;
using System.Diagnostics;
using TerrainDemo.Libs.SimpleJSON;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace TerrainDemo.Tests
{
    public class HeightInterpolatorsBench : MonoBehaviour
    {
        // Use this for initialization
        IEnumerator Start ()
        {
            var data = (TextAsset)EditorGUIUtility.Load("Tests/Cellmesh0.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var values = new double[mesh.Cells.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = Random.value;

            var shepardInterpolator = new ShepardInterpolator(new CellMesh.Submesh(mesh, mesh.Cells), values);
            var rbfInterpolator = new RbfInterpolator(new CellMesh.Submesh(mesh, mesh.Cells), values);
            var barycentricInterpolator = new BarycentricInterpolator(new CellMesh.Submesh(mesh, mesh.Cells), values);
            var localIDWInterpolator = new LocalIDWInterpolator(new CellMesh.Submesh(mesh, mesh.Cells), values);

            //Warm up
            shepardInterpolator.GetValue(Vector2.zero);
            rbfInterpolator.GetValue(Vector2.zero);
            barycentricInterpolator.GetValue(Vector2.zero);
            localIDWInterpolator.GetValue(Vector2.zero);
            yield return new WaitForSeconds(2);

            var timer = Stopwatch.StartNew();
            for (int x = -100; x < 100; x++)
            {
                for (int z = -100; z < 100; z++)
                {
                    var test = shepardInterpolator.GetValue(new Vector2(x, z));
                }
            }
            var result = timer.ElapsedMilliseconds;
            Debug.LogFormat("Shepard interpolator {0} msec", result);       //192 msec

            yield return null;

            timer.Reset();
            timer.Start();
            for (int x = -100; x < 100; x++)
            {
                for (int z = -100; z < 100; z++)
                {
                    var test = rbfInterpolator.GetValue(new Vector2(x, z));
                }
            }
            result = timer.ElapsedMilliseconds;
            Debug.LogFormat("RBF interpolator {0} msec", result);           //760 msec

            yield return null;

            timer.Reset();
            timer.Start();
            for (int x = -100; x < 100; x++)
            {
                for (int z = -100; z < 100; z++)
                {
                    var test = barycentricInterpolator.GetValue(new Vector2(x, z));
                }
            }
            result = timer.ElapsedMilliseconds;
            Debug.LogFormat("Barycentric interpolator {0} msec", result);       //183 msec

            yield return null;

            timer.Reset();
            timer.Start();
            for (int x = -100; x < 100; x++)
            {
                for (int z = -100; z < 100; z++)
                {
                    var test = localIDWInterpolator.GetValue(new Vector2(x, z));
                }
            }
            result = timer.ElapsedMilliseconds;
            Debug.LogFormat("Local IDW interpolator {0} msec", result);         //572 msec
        }
       
    }
}
