using NUnit.Framework;
using TerrainDemo.Tools.SimpleJSON;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tests.Editor
{
    public class CellTests
    {
        [Test]
        public void TestNeighbors2()
        {
            var data = (TextAsset)EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var finder = mesh.FloodFill(mesh[0]);
            var neighbors2 = finder.GetNeighbors(2);

            Assert.That(mesh[0].Neighbors2, Is.EquivalentTo(neighbors2));
        }
    }
}
