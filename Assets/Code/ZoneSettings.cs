using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code
{
    [CreateAssetMenu]
    public class ZoneSettings : ScriptableObject, IZoneNoiseSettings
    {
        public string Name;
        public Color LandColor_ = Color.magenta;
        public ZoneType Type;
        public BlockType DefaultBlock;

        public float Height_ = 0;

        [Header("Coarse octave")]
        public float OutScale1_ = 30f;

        [Header("Fine octave")]
        public float OutScale2_ = 5f;

        [Header("Global octave")]
        public float OutScale3_ = 30f;


        public Color LandColor { get { return LandColor_; } }
        public float Height { get { return Height_; } }

        public float OutScale1 { get { return OutScale1_; } }

        public float OutScale2 { get { return OutScale2_; } }

        public float OutScale3 { get { return OutScale3_; } }

        public static IZoneNoiseSettings Lerp(ZoneSettings[] zones, ZoneRatio ratio)
        {
            var outScale1 = 0f;
            var outScale2 = 0f;
            var outScale3 = 0f;
            var height = 0f;

            for (int i = 0; i < zones.Length; i++)
            {
                var zoneNoiseSettings = zones[i];
                var zoneRatio = ratio[zoneNoiseSettings.Type];
                height += zoneNoiseSettings.Height*zoneRatio;
                outScale1 += zoneNoiseSettings.OutScale1*zoneRatio;
                outScale2 += zoneNoiseSettings.OutScale2*zoneRatio;
                outScale3 += zoneNoiseSettings.OutScale3*zoneRatio;
            }

            var result = new ZoneSettings2(Color.black, height, outScale1, outScale2, outScale3);

            return result;
        }
    }
}