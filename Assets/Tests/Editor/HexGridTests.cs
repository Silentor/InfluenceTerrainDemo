using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
    [TestFixture]
    public class HexGridTests
    {
        [Test]
        public void TestSharedEdge()
        {
            var grid = new HexGrid<HexPos, int, int>( 10, 3 );

            //Create first node
            grid[HexPos.Zero].Value = HexPos.Zero;
            var edges = grid[ HexPos.Zero ].CellEdges;
            edges[HexDir.QPlus].Value = 1;

            //Create second node
            var neighborPosition = HexPos.Zero + HexPos.QPlus;
            grid[neighborPosition].Value = neighborPosition;

            //Assert that edge is shared
            Assert.IsTrue( grid[ neighborPosition ].CellEdges[HexDir.QMinus].Value == 1 );
        }

        [Test]
        public void TestSharedVertex()
        {
	        var grid = new HexGrid<HexPos, int, int>( 10, 4 );

	        //Create first node
	        grid[HexPos.Zero].Value = HexPos.Zero;
	        var vertices = grid[ HexPos.Zero ].CellVertices;
	        vertices[1].Value = 1;

	        //Create second node
	        var neighbor = HexPos.Zero + HexPos.SPlus;
	        grid[neighbor].Value = neighbor;

	        //Assert that vertex is shared
            //Assigned node
	        Assert.IsTrue( grid[ HexPos.Zero + HexPos.SPlus].CellVertices[5].Value == 1 );
            //Unassigned node
	        Assert.IsTrue( grid[ HexPos.Zero + HexPos.QPlus].CellVertices[3].Value == 1 );
        }


        [Test]
        public void TestNeighbors( )
        {
	        var grid = new HexGrid<HexPos?, int, int>( 10, 4 );
            grid[HexPos.Zero].Value = HexPos.Zero;
            grid[HexPos.Zero + HexPos.QPlus].Value = HexPos.Zero + HexPos.QPlus;
            grid[HexPos.Zero + HexPos.QMinus].Value = HexPos.Zero + HexPos.QMinus;

            Assert.That( 
	            grid[HexPos.Zero].Neighbors.ToArray(  ), 
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

        [Test]
        public void TestEnumeration( )
        {
	        var grid         = new HexGrid<HexPos, int, int>( 10, 4 );
	        var allPositions = grid.ToArray( );

	        Debug.Log( allPositions.ToJoinedString() );

	        Assert.That( allPositions.Count, Is.EqualTo( 16 ) );
	        Assert.IsTrue( allPositions.Contains( new HexPos(-1, -1)) );
	        Assert.IsTrue( allPositions.Contains( new HexPos(-2, 2)) ) ;
	        Assert.IsTrue( allPositions.Contains( new HexPos(2, -1)) ) ;
	        Assert.IsTrue( allPositions.Contains( new HexPos(1, 2)) ) ;
        }

    }
}
