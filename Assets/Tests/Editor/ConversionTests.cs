using NUnit.Framework;
using TerrainDemo.Spatial;
using UnityEngine;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tests.Editor
{
    public class ConversionTests
    {
        [Test]
        public void TestVector2()
        {
            Assert.That((Vector2i)Vector2.Zero, Is.EqualTo(Vector2i.Zero));
            Assert.That((Vector2i)Vector2.One, Is.EqualTo(Vector2i.One));
            Assert.That((Vector2i)new Vector2(0.01f, 0.01f), Is.EqualTo(Vector2i.Zero));
            Assert.That((Vector2i)new Vector2(0.0001f, 0.0001f), Is.EqualTo(Vector2i.Zero));
            Assert.That((Vector2i)new Vector2(0.9999f, 0.9999f), Is.EqualTo(Vector2i.Zero));
            Assert.That((Vector2i)new Vector2(1.0001f, 1.0001f), Is.EqualTo(Vector2i.One));
            Assert.That((Vector2i)new Vector2(-0, -0), Is.EqualTo(Vector2i.Zero));
            Assert.That((Vector2i)new Vector2(-0.0001f, -0.0001f), Is.EqualTo(new Vector2i(-1, -1)));
            Assert.That((Vector2i)new Vector2(-0.5f, -0.5f), Is.EqualTo(new Vector2i(-1, -1)));
            Assert.That((Vector2i)new Vector2(-0.9999f, -0.9999f), Is.EqualTo(new Vector2i(-1, -1)));
            Assert.That((Vector2i)new Vector2(-1f, -1f), Is.EqualTo(new Vector2i(-1, -1)));
            Assert.That((Vector2i)new Vector2(-1.0001f, -1.0001f), Is.EqualTo(new Vector2i(-2, -2)));
            Assert.That((Vector2i)new Vector2(-1.9999f, -1.9999f), Is.EqualTo(new Vector2i(-2, -2)));
            Assert.That((Vector2i)new Vector2(-2f, -2f), Is.EqualTo(new Vector2i(-2, -2)));
        }

        [Test]
        public void TestVector3()
        {
            Assert.That((Vector3i)Vector3.zero, Is.EqualTo(Vector3i.Zero));
            Assert.That((Vector3i)Vector3.one, Is.EqualTo(Vector3i.One));
            Assert.That((Vector3i)new Vector3(0.01f, 0.01f, 0.01f), Is.EqualTo(Vector3i.Zero));
            Assert.That((Vector3i)new Vector3(0.0001f, 0.0001f, 0.0001f), Is.EqualTo(Vector3i.Zero));
            Assert.That((Vector3i)new Vector3(0.9999f, 0.9999f, 0.9999f), Is.EqualTo(Vector3i.Zero));
            Assert.That((Vector3i)new Vector3(1.0001f, 1.0001f, 1.0001f), Is.EqualTo(Vector3i.One));
            Assert.That((Vector3i)new Vector3(-0, -0, -0), Is.EqualTo(Vector3i.Zero));
            Assert.That((Vector3i)new Vector3(-0.0001f, -0.0001f, -0.0001f), Is.EqualTo(new Vector3i(-1, -1, -1)));
            Assert.That((Vector3i)new Vector3(-0.5f, -0.5f, -0.5f), Is.EqualTo(new Vector3i(-1, -1, -1)));
            Assert.That((Vector3i)new Vector3(-0.9999f, -0.9999f, -0.9999f), Is.EqualTo(new Vector3i(-1, -1, -1)));
            Assert.That((Vector3i)new Vector3(-1f, -1f, -1f), Is.EqualTo(new Vector3i(-1, -1, -1)));
            Assert.That((Vector3i)new Vector3(-1.0001f, -1.0001f, -1.0001f), Is.EqualTo(new Vector3i(-2, -2, -2)));
            Assert.That((Vector3i)new Vector3(-1.9999f, -1.9999f, -1.9999f), Is.EqualTo(new Vector3i(-2, -2, -2)));
            Assert.That((Vector3i)new Vector3(-2f, -2f, -2f), Is.EqualTo(new Vector3i(-2, -2, -2)));
        }

    }
}
