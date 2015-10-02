using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Settings;
using UnityEngine;

namespace Assets.Code.Generators
{
    public class MountainsGenerator : ZoneGenerator
    {
        public MountainsGenerator(Zone zone, Land land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        public override BlockType DefaultBLock { get {return BlockType.Grass;} }

        protected override float GenerateBaseHeight(int worldX, int worldZ, IZoneNoiseSettings settings)
        {
            var height = base.GenerateBaseHeight(worldX, worldZ, settings);
            var mountInfluence = Land.GetInfluence(new Vector2(worldX, worldZ))[ZoneType.Mountains];

            if (mountInfluence > 0.7f)
            {
                var additional = Math.Pow(((mountInfluence - 0.7f)*4), 2) + 1;
                height *= (float)additional;
            }

            return height;
        }

        protected override BlockType GenerateBlock(int blockX, int blockZ)
        {
            var mountInfluence = Land.GetInfluence(new Vector2(blockX, blockZ))[ZoneType.Mountains];
            if(mountInfluence > 0.7f)
                return BlockType.Rock;
            else
                return base.GenerateBlock(blockX, blockZ);
        }
    }
}
