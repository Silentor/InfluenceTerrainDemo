using System.Collections;
using NUnit.Framework;
using TerrainDemo.Spatial;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
    [TestFixture]
    public class HexGridTests
    {
        [Test]
        public void TestEdge()
        {
            var grid = new HexGrid<HexPos, int, int>( 10, 3 );

            //Create first node
            grid[HexPos.Zero] = HexPos.Zero;
            var edges = grid.GetEdges( HexPos.Zero );
            edges[HexDir.QPlus] = 1;

            //Create second node
            var neighbor = HexPos.Zero + HexPos.QPlus;
            grid[neighbor] = neighbor;

            //Assert that edge is shared
            Assert.IsTrue( grid.GetEdges( neighbor )[HexDir.QMinus] == 1 );
        }

        [Test]
        public void TestVertex()
        {
	        var grid = new HexGrid<HexPos, int, int>( 10, 2 );

	        //Create first node
	        grid[HexPos.Zero] = HexPos.Zero;
	        var vertices = grid.GetVertices( HexPos.Zero );
	        vertices[1] = 1;

	        //Create second node
	        var neighbor = HexPos.Zero + HexPos.SPlus;
	        grid[neighbor] = neighbor;

	        //Assert that vertex is shared
	        Assert.IsTrue( grid.GetVertices( HexPos.Zero + HexPos.SPlus )[5] == 1 );
	        Assert.IsTrue( grid.GetVertices( HexPos.Zero + HexPos.QPlus )[3] == 1 );
        }

        
    }
}
