using System.Collections;
using NUnit.Framework;
using OpenToolkit.Mathematics;
using TerrainDemo.Spatial;
using UnityEngine;
using UnityEngine.TestTools;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Tests.Editor
{
    public class Bounds2iTests {

        [Test]
        public void TestBoundsConversion()
        {
            var source1 = new Bounds() { min = new Vector3(0.5f, 0, 0.5f), max = new Vector3(2.5f, 0, 2.5f)};
            var result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(2))));

            source1 = new Bounds() {min = Vector3.zero, max = Vector3.one * 3};
            result = (Bounds2i) source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(2))));

            source1 = new Bounds() { min = Vector3.zero, max = Vector3.one * 3.001f };
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(3))));

            source1 = new Bounds() { max = new Vector3(-0.5f, 0, -0.5f), min = new Vector3(-2.5f, 0, -2.5f) };
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-3), new GridPos(-1))));

            source1 = new Bounds() { max = Vector3.zero, min = Vector3.one * -3 };
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-3), new GridPos(-1))));

            source1 = new Bounds() { max = Vector3.zero, min = Vector3.one * -3.001f };
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-4), new GridPos(-1))));

        }

        [Test]
        public void TestBox2Conversion()
        {
            var source1 = new Box2(0.5f, 2.5f, 2.5f, 0.5f);
            var result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(2))));

            source1 = new Box2(0, 3, 3, 0);
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(3))));

            source1 = new Box2(0, 3.001f, 3.001f, 0);
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(GridPos.Zero, new GridPos(3))));

            source1 = new Box2(-2.5f, -0.5f, -0.5f, -2.5f);
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-3), new GridPos(-1))));

            source1 = new Box2(-3, 0, 0, -3);
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-3), new GridPos(-1))));

            source1 = new Box2(-3.001f, 0, 0, -3.001f);
            result = (Bounds2i)source1;
            Assert.That(result, Is.EqualTo(new Bounds2i(new GridPos(-4), new GridPos(-1))));
        }
    }
}
