using System.Linq;
using NUnit.Framework;
using TerrainDemo.Generators;
using TerrainDemo.Settings;
using TerrainDemo.Tools.SimpleJSON;
using TerrainDemo.Voronoi;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Tests.Editor
{
    public class CellMeshTests
    {
        [Test]
        public void TestSerialization()
        {
            var cells = new Cell[]
            {
                new Cell(0, new Vector2(1, 1), false, new[] {Vector2.zero, Vector2.up, Vector2.one},
                    new[]
                    {
                        new Cell.Edge(Vector2.zero, Vector2.up), new Cell.Edge(Vector2.up, Vector2.one),
                        new Cell.Edge(Vector2.one, Vector2.zero)
                    },
                    new Bounds(Vector3.zero, Vector3.one)),

                new Cell(1, new Vector2(3, 3), true, new[] {Vector2.zero, Vector2.down, Vector2.left},
                    new[]
                    {
                        new Cell.Edge(Vector2.zero, Vector2.down), new Cell.Edge(Vector2.down, Vector2.left),
                        new Cell.Edge(Vector2.left, Vector2.zero)
                    },
                    new Bounds(Vector3.zero, Vector3.one)),
            };
            var bounds = new Bounds(Vector3.up, Vector3.one);

            cells[0].Init(new[] {cells[1]});
            cells[1].Init(new[] {cells[0]});
            var mesh = new CellMesh(cells, bounds);

            //Act
            var data = mesh.ToJSON().ToString();
            var mesh2 = CellMesh.FromJSON(JSONNode.Parse(data));

            //Assert
            Assert.AreEqual(mesh.Cells.Length, mesh2.Cells.Length);
            Assert.AreEqual(mesh.Cells[0].Id, mesh2.Cells[0].Id);
            Assert.AreEqual(mesh.Bounds, mesh2.Bounds);
            Assert.AreEqual(mesh.Cells[0].Neighbors[0].Id, mesh2.Cells[1].Id);
        }


        [Test]
        public void TestGetNeighbors()
        {
            var data = (TextAsset) EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var center = mesh[0];
            var outers = mesh.GetNeighbors(center);

            //Test center cell
            var neighbors0 = outers.GetNeighbors(0).ToArray();
            Assert.That(neighbors0, Has.Length.EqualTo(1));
            Assert.That(center, Is.EqualTo(neighbors0.First()));
            Assert.That(neighbors0, Is.Unique);

            //Test outer cells rank1
            var neighbors1 = outers.GetNeighbors(1).ToArray();
            Assert.That(neighbors1, Has.Length.EqualTo(6));
            Assert.That(neighbors1, Is.EquivalentTo(new Cell[] {mesh[4], mesh[2], mesh[5], mesh[1], mesh[7], mesh[3]}));
            Assert.That(neighbors1, Is.Unique);

            //Test outer cells rank2
            var neighbors2 = outers.GetNeighbors(2).ToArray();

            //Calulated by hand :)
            Assert.That(neighbors2, Has.Length.EqualTo(12));
            Assert.That(neighbors2, Is.Unique);

            //Test that all ring 2 cells has some neighbors in ring 1
            Assert.That(neighbors2, Has.All.Matches<Cell>(c => c.Neighbors.Any(cn => neighbors1.Contains(cn))));

            //Test that no ring 2 cells has center cell neighbor
            Assert.That(neighbors2, Has.All.Matches<Cell>(c => c.Neighbors.All(cn => cn != center)));

            //Test that ring 0, ring 1 and ring 2 cells completely different
            Assert.That(neighbors0, Is.Not.EquivalentTo(neighbors1));
            Assert.That(neighbors1, Is.Not.EquivalentTo(neighbors2));
            Assert.That(neighbors2, Is.Not.EquivalentTo(neighbors0));
        }

        [Test]
        public void TestFloodFill()
        {
            var data = (TextAsset) EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var center = mesh[0];
            var neighbors = mesh.FloodFill(center).ToArray(); //Get all neighbors

            Assert.That(neighbors, Has.Length.EqualTo(49)); //Because mesh has 50 cells
            Assert.That(neighbors, Is.Unique);
            Assert.That(neighbors, Is.Not.Contains(center));
        }

        [Test]
        public void TestConditionalFloodFill()
        {
            var data = (TextAsset) EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var center = mesh[0];
            var noNeighbors = mesh.FloodFill(center, c => c.Id > 10 && c.Id < 20).ToArray();

            //id 0 cell doesnt has neighbors with 10 < id < 20
            Assert.That(noNeighbors, Has.Length.EqualTo(0));

            var hasNeighbors = mesh.FloodFill(center, c => c.Id == 1 || c.Id == 6 || c.Id == 13).ToArray();
            Assert.That(hasNeighbors, Has.Length.EqualTo(3));
            Assert.That(hasNeighbors, Is.Unique);
        }

        [Test]
        public void TestGetNeighborsOverStep()
        {
            var data = (TextAsset) EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var center = mesh[0];
            var floodfillResult = mesh.GetNeighbors(center);

            //Get step 2 result over step 1
            var result = floodfillResult.GetNeighbors(2).ToArray();

            //Calulated by hand :)
            Assert.That(result, Has.Length.EqualTo(12));
        }

        [Test]
        public void TestSubMeshGetBorder()
        {
            var data = (TextAsset)EditorGUIUtility.Load("Tests/Cellmesh1.json");
            var mesh = CellMesh.FromJSON(JSONNode.Parse(data.text));

            var submeshCells = new int[] {9, 8, 12, 23, 25, 5, 2, 4, 11, 3, 0, 1, 6}.Select(i => mesh[i]).ToArray();
            var submesh = new CellMesh.Submesh(mesh, submeshCells);

            var border = submesh.GetBorderCells().ToArray();
            var expectedBorder = new[] {9, 8, 12, 23, 25, 6, 1, 0, 3, 11, 4}.Select(i => mesh[i]).ToArray();

            //Expected border cells calculated by hand
            Assert.That(border, Is.EquivalentTo(expectedBorder));
            Assert.That(border, Is.Unique);

            var innerNeighborsStep1 = submesh.GetFloodFill(border).GetNeighbors(1);
            var expectedInnerStep1 = submeshCells.Except(expectedBorder).ToArray();

            Assert.That(innerNeighborsStep1, Is.EquivalentTo(expectedInnerStep1));

            var inner = submesh.GetInnerCells().ToArray();

            Assert.That(inner, Is.EquivalentTo(innerNeighborsStep1));
        }
        
    }
}
