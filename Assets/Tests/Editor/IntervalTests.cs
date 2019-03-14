using System.Collections;
using NUnit.Framework;
using TerrainDemo.Tools;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
    public class IntervalTests
    {

        [Test]
        public void TestContains()
        {
            var interval = new Interval(0, 5);

            Assert.That(interval.Contains(3), Is.True);
            Assert.That(interval.Contains(0), Is.True);
            Assert.That(interval.Contains(5), Is.True);
            Assert.That(interval.Contains(-1), Is.False);
        }

        [Test]
        public void TestConstructor()
        {
            var interval = new Interval(-1, 3);

            Assert.That(interval.IsEmpty, Is.False);
            Assert.That(interval, Is.Not.EqualTo(Interval.Empty));
            Assert.That(interval != Interval.Empty, Is.True);

            interval = new Interval(3, -1);

            Assert.That(interval.IsEmpty, Is.True);
            Assert.That(interval, Is.EqualTo(Interval.Empty));
            Assert.That(interval == Interval.Empty, Is.True);
        }

        [Test]
        public void TestSubstract()
        {
            var interval1 = new Interval(0, 5);
            var interval2 = new Interval(-5, -1);
            var interval3 = new Interval(5, 10);
            var interval4 = new Interval(-1, 15);
            var interval5 = new Interval(0, 2);
            var interval6 = new Interval(3, 5);
            var interval7 = new Interval(2, 3);

            Assert.That(interval1.Subtract2(interval2), Is.EquivalentTo(new []{interval1}));
            Assert.That(interval1.Subtract2(interval3), Is.EquivalentTo(new[] { interval1 }));
            Assert.That(interval1.Subtract2(interval4), Is.Empty);
            Assert.That(interval1.Subtract2(interval5), Is.EquivalentTo(new []{new Interval(2, 5)}));
            Assert.That(interval1.Subtract2(interval6), Is.EquivalentTo(new[] { new Interval(0, 3) }));
            Assert.That(interval1.Subtract2(interval7), Is.EquivalentTo(new[]{new Interval(0, 2), new Interval(3, 5)}));
        }

        [Test]
        public void TestIsIntersects()
        {
            var interval1 = new Interval(0, 5);
            var interval2 = new Interval(-5, -1);
            var interval3 = new Interval(5, 10);
            var interval4 = new Interval(-1, 15);
            var interval5 = new Interval(0, 2);
            var interval6 = new Interval(3, 5);
            var interval7 = new Interval(2, 3);

            Assert.That(interval5.IsIntersects(interval6), Is.False);
            Assert.That(interval6.IsIntersects(interval5), Is.False);
            Assert.That(interval5.IsIntersects(interval5), Is.True);
            Assert.That(interval5.IsIntersects(interval7), Is.True);
            Assert.That(interval7.IsIntersects(interval5), Is.True);
        }

    }
}
