using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Tests.Editor
{
    public class IntersectionsTests
    {
        [Test]
        public void TestBarycentric2DCoords()
        {
            var a = new OpenToolkit.Mathematics.Vector2(0, 0);
            var b = new OpenToolkit.Mathematics.Vector2(10, 0);
            var c = new OpenToolkit.Mathematics.Vector2(0, 10);

            //First coord should be large
            var coords = Intersections.Barycentric2DCoords(new OpenToolkit.Mathematics.Vector2(0.5f, 0.5f), a, b, c);
            Assert.That(coords.u, Is.GreaterThan(0.8));
            Assert.That(coords.u + coords.v + coords.w, Is.EqualTo(1));

            //Second coord should be large
            coords = Intersections.Barycentric2DCoords(new OpenToolkit.Mathematics.Vector2(9f, 0.5f), a, b, c);
            Assert.That(coords.v, Is.GreaterThan(0.8));
            Assert.That(coords.u + coords.v + coords.w, Is.EqualTo(1));

            //Third coord should be large
            coords = Intersections.Barycentric2DCoords(new OpenToolkit.Mathematics.Vector2(0.5f, 9f), a, b, c);
            Assert.That(coords.w, Is.GreaterThan(0.8));
            Assert.That(coords.u + coords.v + coords.w, Is.EqualTo(1));

            //Some manual case
            coords = Intersections.Barycentric2DCoords(new  OpenToolkit.Mathematics.Vector2(94, 27), new OpenToolkit.Mathematics.Vector2(86, 34), new OpenToolkit.Mathematics.Vector2(119, 23), new OpenToolkit.Mathematics.Vector2(93, -2));
            //Assert.tha
        }

        [Test]
        public void TestGridIntersection()
        {
            var inters = Intersections.GridIntersections(new Vector2(0, 1), new Vector2(3, 0), 10);
            Assert.That(inters.Count(), Is.EqualTo(4));

            inters = Intersections.GridIntersections(new Vector2(0, 0), new Vector2(1, 1)).Take(100);
            Assert.That(inters.Count(), Is.EqualTo(2));
            //ShowPath(inters);

            inters = Intersections.GridIntersections(new Vector2(0, 1), new Vector2(1, 0)).Take(3).ToArray();
            //ShowPath(inters);
            Assert.That(inters.Count(), Is.EqualTo(2));

            inters = Intersections.GridIntersections(new Vector2(1, 1), new Vector2(0, 0)).Take(100);
            Assert.That(inters.Count(), Is.EqualTo(2));
            //ShowPath(inters);

            inters = Intersections.GridIntersections(new Vector2(1, 0), new Vector2(0, 1)).Take(100);
            Assert.That(inters.Count(), Is.EqualTo(2));
            //ShowPath(inters);

            inters = Intersections.GridIntersections(
                BlockInfo.GetWorldCenter(-11, 26),
                BlockInfo.GetWorldCenter(-10, 25)).Take(100);
            Assert.That(inters.Count(), Is.EqualTo(2));

            inters = Intersections.GridIntersections(new Vector2(-11, 26), new Vector2(-10, 25)).Take(100);
            Assert.That(inters.Count(), Is.EqualTo(2));

            inters = Intersections.GridIntersections(new Vector2(0, 0), new Vector2(0, 0)).Take(3).ToArray();
            Assert.That(inters.Count(), Is.EqualTo(0));
        }

        private void ShowPath(IEnumerable<(Vector2i blockPosition, float distance, Vector2i normal)> path)
        {
            var path2 = path.ToArray();
            foreach (var inter in path)
            {
                Debug.Log(inter);
            }

        }
    }
}
