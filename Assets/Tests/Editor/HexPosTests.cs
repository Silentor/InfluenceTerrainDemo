using System.Collections;
using NUnit.Framework;
using TerrainDemo.Spatial;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
    [TestFixture]
    public class HexPosTests
    {

        [Test]
        public void TestDistance()
        {
            var from = new HexPos(-1, 1);
            var from2 = from;
            var to = new HexPos(2, 0);

            Assert.That( from.Distance( from2 ), Is.EqualTo( 0 ) );
            Assert.That( from.Distance( to ), Is.EqualTo( 3 ) );
        }

    }
}
