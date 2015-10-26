using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Code.Layout;
using Assets.Code.Settings;
using UnityEngine;

namespace Assets.Code.Generators
{
    public class LakeGenerator : ZoneGenerator
    {
        public LakeGenerator(ZoneLayout zone, LandLayout land, ILandSettings landSettings) : base(zone, land, landSettings)
        {
        }

        protected override BlockType GenerateBlock(int blockX, int blockZ)
        {
            var turbulenceX = (Mathf.PerlinNoise(blockX * 0.05f, blockZ * 0.05f) - 0.5f) * 10;
            var turbulenceZ = (Mathf.PerlinNoise(blockZ * 0.05f, blockX * 0.05f) - 0.5f) * 10;

            if (Land.GetInfluence(new Vector2(blockX + turbulenceX, blockZ + turbulenceZ))[ZoneType.Lake] > 0.85f)
                return BlockType.Water;

            return base.GenerateBlock(blockX, blockZ);
        }
    }
}
