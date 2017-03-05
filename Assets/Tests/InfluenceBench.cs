using System.Collections;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Libs.SimpleJSON;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Tests
{
    public class InfluenceBench : MonoBehaviour
    {

        // Use this for initialization
        IEnumerator Start ()
        {
            var data = (TextAsset)EditorGUIUtility.Load("Tests/Cellmesh0.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var settings = FindObjectOfType<LandSettings>();
            var zones = new ZoneInfo[mesh.Cells.Length];
            var zoneTypes = settings.Zones.Select(z => z.Type).ToArray();
            for (int i = 0; i < zones.Length; i++)
                zones[i] = new ZoneInfo {ClusterId = 0, Type = zoneTypes[Random.Range(0, zoneTypes.Length)]};
            var layout = new LandLayout(settings, mesh, new[]
            {
                new ClusterInfo {Id = 0, ClusterHeight = Vector3.zero, Zones = zones}
            });

            //Warm up
            layout.GetInfluenceLocalIDW(Vector2.zero);
            layout.GetInfluenceLocalIDW2(Vector2.zero);

            yield return new WaitForSeconds(2);

            var timer = new Stopwatch();

            timer.Start();

            for (var x = mesh.Bounds.min.x; x < mesh.Bounds.max.x; x += 1)
                for (var z = mesh.Bounds.min.z; z < mesh.Bounds.max.z; z += 1)
                {
                    var value = layout.GetInfluenceLocalIDW(new Vector2(x, 0));
                }

            timer.Stop();

            Debug.LogFormat("GetInfluenceLocalIDW (cellmesh) takes {0} msec", timer.ElapsedMilliseconds);    //1140 msec

            yield return null;

            timer.Reset();
            timer.Start();

            for (var x = mesh.Bounds.min.x; x < mesh.Bounds.max.x; x++)
                for (var z = mesh.Bounds.min.z; z < mesh.Bounds.max.z; z++)
                {
                    var value = layout.GetInfluenceLocalIDW2(new Vector2(x, z));
                }

            timer.Stop();

            Debug.LogFormat("GetInfluenceLocalIDW2 (alglib) takes {0} msec", timer.ElapsedMilliseconds);    //524 msec
        }
    }
}
