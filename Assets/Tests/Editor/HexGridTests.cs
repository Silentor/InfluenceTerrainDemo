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
            var edges = grid.GetEdgesValue( HexPos.Zero );
            edges[HexDir.QPlus] = 1;

            //Create second node
            var neighbor = HexPos.Zero + HexPos.QPlus;
            grid[neighbor] = neighbor;

            //Assert that edge is shared
            Assert.IsTrue( grid.GetEdgesValue( neighbor )[HexDir.QMinus] == 1 );
        }

        [Test]
        public void TestVertex()
        {
	        var grid = new HexGrid<HexPos, int, int>( 10, 2 );

	        //Create first node
	        grid[HexPos.Zero] = HexPos.Zero;
	        var vertices = grid.GetVerticesValue( HexPos.Zero );
	        vertices[1] = 1;

	        //Create second node
	        var neighbor = HexPos.Zero + HexPos.SPlus;
	        grid[neighbor] = neighbor;

	        //Assert that vertex is shared
            //Assigned node
	        Assert.IsTrue( grid.GetVerticesValue( HexPos.Zero + HexPos.SPlus)[5] == 1 );
            //Unassigned node
	        Assert.IsTrue( grid.GetVerticesValue( HexPos.Zero + HexPos.QPlus)[3] == 1 );
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

        [Test]
        public void TestLineRasterization( )
        {
	        var grid = new HexGrid<HexPos?, int, int>( 10, 5 );
	        var line = grid.RasterizeLine( HexPos.Zero, new HexPos( 3, 0 ) );

            Assert.That( line, Is.EquivalentTo( new HexPos[]
                                                {
	                                                new HexPos(0, 0),
	                                                new HexPos(1, 0),
	                                                new HexPos(2, 0),
	                                                new HexPos(3, 0),
                                                }) );
        }

        [Test]
        public void TestCoordsConversion( )
        {
	        var grid = new HexGrid<HexPos, int, int>( 10, 5 );

            var source = new HexPos(2, -1);
            var dest = grid.HexToArray2d( source.Q, source.R );
            var dest2 = grid.Array2dToHex( dest.x, dest.y );

            Assert.IsTrue( source.Q == dest2.q && source.R == dest2.r );
        }

    }
}
