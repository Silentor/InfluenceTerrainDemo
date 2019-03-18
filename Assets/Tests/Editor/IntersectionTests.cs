using NUnit.Framework;
using TerrainDemo.Tools;
using UnityEngine;

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
    }
}
