using System.Collections;
using NUnit.Framework;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using UnityEditor;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
	[TestFixture]
	public class NavGridTests
	{

		[Test]
		public void TestBlockNormal( )
		{
			var normal = new Normal(Incline.Small, Direction.Forward);

			Assert.That( normal.Project( Direction.Forward ), Is.EqualTo( LocalIncline.SmallUphill ));
			Assert.That( normal.Project( Direction.Left ), Is.EqualTo( LocalIncline.Flat ));
			Assert.That( normal.Project( Direction.Back ), Is.EqualTo( LocalIncline.SmallDownhill ));
		}

		
	}
}
