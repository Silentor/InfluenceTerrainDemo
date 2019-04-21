using NUnit.Framework;
using TerrainDemo.Tools;

namespace TerrainDemo.Tests.Editor
{
    public class InterpolationTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestRemap()
        {
            Assert.That(Interpolation.Remap(2, 1, 3, 10, 20), Is.EqualTo(15));
            Assert.That(Interpolation.Remap(0, 1, 3, 10, 20), Is.EqualTo(10));
            Assert.That(Interpolation.Remap(100, 1, 3, 10, 20), Is.EqualTo(20));

            Assert.That(Interpolation.RemapUnclamped(2, 1, 3, 10, 20), Is.EqualTo(15));
            Assert.That(Interpolation.RemapUnclamped(0, 1, 3, 10, 20), Is.EqualTo(5));
            Assert.That(Interpolation.RemapUnclamped(4, 1, 3, 10, 20), Is.EqualTo(25));

            Assert.That(Interpolation.Remap(2, 1, 3, 20, 10), Is.EqualTo(15));
            Assert.That(Interpolation.Remap(0, 1, 3, 20, 10), Is.EqualTo(20));
            Assert.That(Interpolation.Remap(100, 1, 3, 20, 10), Is.EqualTo(10));

            Assert.That(Interpolation.RemapUnclamped(2, 1, 3, 20, 10), Is.EqualTo(15));
            Assert.That(Interpolation.RemapUnclamped(0, 1, 3, 20, 10), Is.EqualTo(25));
            Assert.That(Interpolation.RemapUnclamped(4, 1, 3, 20, 10), Is.EqualTo(5));


        }
    }
}
