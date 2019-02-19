using System.Collections;
using NUnit.Framework;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using UnityEngine.TestTools;

namespace TerrainDemo.Tests.Editor
{
    public class BlockTests
    {
        [Test]
        public void TestCreateAutoFix()
        {
            //Good solid block
            var b = new Blocks(BlockType.Grass, BlockType.GoldOre, new Heights(3, 2, 1));
            Assert.That(b.Ground == BlockType.Grass && b.Underground == BlockType.GoldOre && b.Base == BlockType.Bedrock 
                        && b.Height.Main == 3 && b.Height.Underground == 2 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Good cave block
            b = new Blocks(BlockType.Grass, BlockType.Cave, new Heights(3, 2, 1));
            Assert.That(b.Ground == BlockType.Grass && b.Underground == BlockType.Cave && b.Base == BlockType.Bedrock
                        && b.Height.Main == 3 && b.Height.Underground == 2 && b.Height.Base == 1 && !b.IsSimple && !b.IsEmpty);

            //Good ore block without grass
            b = new Blocks(BlockType.Empty, BlockType.GoldOre, new Heights(2, 2, 1));
            Assert.That(b.Ground == BlockType.Empty && b.Underground == BlockType.GoldOre && b.Base == BlockType.Bedrock
                        && b.Height.Main == 2 && b.Height.Underground == 2 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block without grass with height
            b = new Blocks(BlockType.Empty, BlockType.GoldOre, new Heights(3, 2, 1));
            Assert.That(b.Ground == BlockType.Empty && b.Underground == BlockType.GoldOre && b.Base == BlockType.Bedrock
                        && b.Height.Main == 2 && b.Height.Underground == 2 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block without ore with height
            b = new Blocks(BlockType.Grass, BlockType.Empty, new Heights(3, 2, 1));
            Assert.That(b.Ground == BlockType.Grass && b.Underground == BlockType.Empty && b.Base == BlockType.Bedrock
                        && b.Height.Main == 3 && b.Height.Underground == 1 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block with grass without height
            b = new Blocks(BlockType.Grass, BlockType.GoldOre, new Heights(2, 2, 1));
            Assert.That(b.Ground == BlockType.Empty && b.Underground == BlockType.GoldOre && b.Base == BlockType.Bedrock
                        && b.Height.Main == 2 && b.Height.Underground == 2 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block with ore without height
            b = new Blocks(BlockType.Grass, BlockType.GoldOre, new Heights(3, 0, 1));
            Assert.That(b.Ground == BlockType.Grass && b.Underground == BlockType.Empty && b.Base == BlockType.Bedrock
                        && b.Height.Main == 3 && b.Height.Underground == 1 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block with cave without grass
            b = new Blocks(BlockType.Empty, BlockType.Cave, new Heights(3, 2, 1));
            Assert.That(b.Ground == BlockType.Empty && b.Underground == BlockType.Empty && b.Base == BlockType.Bedrock
                        && b.Height.Main == 1 && b.Height.Underground == 1 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block with cave without height
            b = new Blocks(BlockType.Grass, BlockType.Cave, new Heights(3, 0, 1));
            Assert.That(b.Ground == BlockType.Grass && b.Underground == BlockType.Empty && b.Base == BlockType.Bedrock
                        && b.Height.Main == 3 && b.Height.Underground == 1 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);

            //Bad block with cave and without grass height
            b = new Blocks(BlockType.Grass, BlockType.Cave, new Heights(3, 3, 1));
            Assert.That(b.Ground == BlockType.Empty && b.Underground == BlockType.Empty && b.Base == BlockType.Bedrock
                        && b.Height.Main == 1 && b.Height.Underground == 1 && b.Height.Base == 1 && b.IsSimple && !b.IsEmpty);


        }

    }
}
