using System.Collections;
using System.Linq;
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
            //Assigned node
	        Assert.IsTrue( grid.GetVertices( HexPos.Zero + HexPos.SPlus )[5] == 1 );
            //Unassigned node
	        Assert.IsTrue( grid.GetVertices( HexPos.Zero + HexPos.QPlus )[3] == 1 );
        }


        [Test]
        public void TestNeighbors( )
        {
	        var grid = new HexGrid<HexPos?, int, int>( 10, 2 );
            grid[HexPos.Zero] = HexPos.Zero;
            grid[HexPos.Zero + HexPos.QPlus] = HexPos.Zero + HexPos.QPlus;
            grid[HexPos.Zero + HexPos.QMinus] = HexPos.Zero + HexPos.QMinus;

            Assert.That( 
	            grid.GetNeighbors(HexPos.Zero).ToArray(  ), 
	            Is.EquivalentTo( new HexPos?[]
	                             {
		                             HexPos.Zero + HexPos.QPlus, null, null, HexPos.Zero + HexPos.QMinus, null, null
	                             } )
                         );
        }

       

    }
}
