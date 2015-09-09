using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Code.Generators
{
    public class HillsGenerator : ZoneGenerator
    {
        public HillsGenerator(Zone zone, Land land, int blocksCount, int blockSize) : base(zone, land, blocksCount, blockSize)
        {

        }

        public override BlockType DefaultBLock { get {return BlockType.Grass;} }
    }
}
