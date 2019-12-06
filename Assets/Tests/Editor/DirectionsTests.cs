using NUnit.Framework;
using TerrainDemo.Spatial;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Tests.Editor
{
    [TestFixture]
    public class DirectionsTests
    {
        [Test]
        public void TestFromVector2Constructor()
        {
            Assert.That( Directions.From( new Vector2(2, 1) ), Is.EqualTo( Direction.Right ) );
            Assert.That( Directions.From( new Vector2(2, -1) ), Is.EqualTo( Direction.Right ) );

            Assert.That( Directions.From( new Vector2(-2, 1) ),  Is.EqualTo( Direction.Left ) );
            Assert.That( Directions.From( new Vector2(-2, -1) ), Is.EqualTo( Direction.Left ) );

            Assert.That( Directions.From( new Vector2(1, 2) ),  Is.EqualTo( Direction.Forward ) );
            Assert.That( Directions.From( new Vector2(-1, 2) ), Is.EqualTo( Direction.Forward ) );

            Assert.That( Directions.From( new Vector2(1, -2) ),  Is.EqualTo( Direction.Back ) );
            Assert.That( Directions.From( new Vector2(-1, -2) ), Is.EqualTo( Direction.Back ) );

            Assert.That( Directions.From( new Vector2(0, 0) ), Is.EqualTo( Direction.Forward ) );

            Assert.That( Directions.From( new Vector2(1, 1) ),  Is.EqualTo( Direction.Forward ) );
            Assert.That( Directions.From( new Vector2(-1, 1) ), Is.EqualTo( Direction.Left ) );
            Assert.That( Directions.From( new Vector2(1, -1) ),  Is.EqualTo( Direction.Right ) );
            Assert.That( Directions.From( new Vector2(-1, -1) ), Is.EqualTo( Direction.Back ) );
        }

       
    }
}
