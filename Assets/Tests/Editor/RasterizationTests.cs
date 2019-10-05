using System;
using System.Diagnostics;
using NUnit.Framework;
using OpenToolkit.Mathematics;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Tests.Editor
{
    public class RasterizationTests
    {

        [Test]
        public void TestLineRasterization()
        {
            var from = (Vector2i) new Vector2(-3.52f, -4.14f);
            var to = (Vector2i) new Vector2(-2.32f, -2.48f);
            var rasterize = Rasterization.lineNoDiag(from.X, from.Z, to.X, to.Z);
            //var rasterize = Rasterization.DDA((Vector2i)new Vector2(-3.52f, -4.14f), (Vector2i)new Vector2(-2.32f, -2.48f), true);
            //var rasterize = Rasterization.BresenhamInt((Vector2i)new Vector2(-3.52f, -4.14f), (Vector2i)new Vector2(-2.32f, -2.48f));
            Assert.That(rasterize, Is.EquivalentTo(new[]{new Vector2i(-4, -5), new Vector2i(-4, -4), new Vector2i(-3, -4), new Vector2i(-3, -3) }));
        }

        [Test]
        public void TestCellRasterization()
        {
            var v1 = new Vector2(1.2f, 1.5f);
            var v2 = new Vector2(0.55f, -1.55f);
            var v3 = new Vector2(-2.4f, -1.1f);
            var v4 = new Vector2(-0.5f, 2.3f);

            var h1 = new HalfPlane(v1, v2, Vector2.Zero);
            var h2 = new HalfPlane(v2, v3, Vector2.Zero);
            var h3 = new HalfPlane(v3, v4, Vector2.Zero);
            var h4 = new HalfPlane(v4, v1, Vector2.Zero);

            var isContains = new Predicate<Vector2>( p => HalfPlane.ContainsInConvex(p, new []{h1, h2, h3, h4}));

            var boundingBox = new Box2(-2.4f, 2.3f, 1.2f, -1.55f);
            var result = Rasterization.ConvexToBlocks(isContains, boundingBox);
            var correctAnswer = new[]{new Vector2i(0, 1), new Vector2i(0, 0), new Vector2i(0, -1), new Vector2i(0, -2),
                new Vector2i(-1, 1), new Vector2i(-1, 0), new Vector2i(-1, -1),
                new Vector2i(-2, 0), new Vector2i(-2, -1)};

            Assert.That(result, Is.EquivalentTo(correctAnswer));

            //Draw polygon
            //UnityEngine.Debug.DrawLine(v1, v2, Color.blue, 10);
            //UnityEngine.Debug.DrawLine(v2, v3, Color.blue, 10);
            //UnityEngine.Debug.DrawLine(v3, v4, Color.blue, 10);
            //UnityEngine.Debug.DrawLine(v4, v1, Color.blue, 10);

            //Draw bbox
            //DrawRectangle.ForDebug();


        }

    }
}
