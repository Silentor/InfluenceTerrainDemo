﻿using System;
using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators.Debug
{
    public class ConeGenerator : ZoneGenerator
    {
        public ConeGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] LandSettings landSettings) : base(ZoneType.Cone, zone, land, landSettings)
        {
        }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            var distanceFromCenter = Vector2.Distance(Vector2.zero, new Vector2(worldX, worldZ));
            distanceFromCenter = Math.Max(distanceFromCenter, 0.1f);
            var height = Mathf.Clamp((100/distanceFromCenter), 0, 50);
            //var settings = Land.GetZoneNoiseSettings(influence);
            return _zoneSettings.Height + height;
        }
    }
}
