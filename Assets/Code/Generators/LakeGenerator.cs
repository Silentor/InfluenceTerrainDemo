using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;

namespace Assets.Code.Generators
{
    public class LakeGenerator : ZoneGenerator
    {
        public LakeGenerator(Zone zone, Land land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBLock { get {return BlockType.Water;} }
    }
}
