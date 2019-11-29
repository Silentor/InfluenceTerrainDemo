using System;
using System.Linq;
using NUnit.Framework;
using OpenToolkit.Mathematics;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;

namespace TerrainDemo.Tests.Editor
{
	[TestFixture]
	public class GridAreaTests
	{
		[Test]
		public void TestBorder( )
		{
			var v1 = new Vector2(1.2f,  1.5f);
			var v2 = new Vector2(0.55f, -1.55f);
			var v3 = new Vector2(-2.4f, -1.1f);
			var v4 = new Vector2(-0.5f, 2.3f);

			var h1 = new HalfPlane(v1, v2, Vector2.Zero);
			var h2 = new HalfPlane(v2, v3, Vector2.Zero);
			var h3 = new HalfPlane(v3, v4, Vector2.Zero);
			var h4 = new HalfPlane(v4, v1, Vector2.Zero);

			var isContains = new Predicate<Vector2>( p => HalfPlane.ContainsInConvex(p, new []{h1, h2, h3, h4}));

			var boundingBox = new Box2(-2.4f, -1.55f, 1.2f, 2.3f);
			var result      = Rasterization.ConvexToBlocks(isContains, boundingBox).GetBorder(  ).ToArray(  );
			var correctAnswer = new GridPos[]
			                    {
				                    ( -2, -1 ), ( -2, 0 ), ( -1, 1 ), ( 0, 1 ), ( 0, 0 ), ( 0, -1 ), ( 0, -2 ),
				                    ( -1, -1 )
			                    };

			Assert.That( result.Length, Is.EqualTo( 8 ) );
			Assert.That( result, Is.EquivalentTo( correctAnswer ) );
		}
	}
}
