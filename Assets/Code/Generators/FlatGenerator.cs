using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Settings;

namespace Assets.Code.Generators
{
    public class FlatGenerator : ZoneGenerator
    {
        public FlatGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBLock
        {
            get { return BlockType.Influence; }
        }

        protected override float GenerateBaseHeight(int worldX, int worldZ, IZoneNoiseSettings settings)
        {
            return 0;
        }
    }
}
