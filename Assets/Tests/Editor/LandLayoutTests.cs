using NUnit.Framework;
using TerrainDemo.Layout;
using TerrainDemo.Libs.SimpleJSON;
using TerrainDemo.Settings;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tests.Editor
{
    public class LandLayoutTests
    {
        [Test]
        public void TestGetChunks()
        {
            var data = (TextAsset)EditorGUIUtility.Load("Tests/Cellmesh0.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));
            var settings = Object.FindObjectOfType<LandSettings>();
            //var clusters = new[]
            //{
            //    new ClusterInfo() {
            //        Id = 0, ZoneHeights = new[] { Vector3.zero, Vector3.one*10 },
            //        ClusterHeights = new[] { Vector3.zero, Vector3.one*10 },
            //        Type = settings.Zones[0].Type,
            //        NeighborsSafe = new ClusterInfo[0],
            //        Cells = new CellMesh.Submesh(mesh, )





            //    }, 
            //}

            //var landLayout = new LandLayout(settings, mesh, );


            var finder = mesh.FloodFill(mesh[0]);
            var neighbors2 = finder.GetNeighbors(2);

            Assert.That(mesh[0].Neighbors2, Is.EquivalentTo(neighbors2));
        }
      
    }
}
