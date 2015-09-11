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
        public float Height_ = 0;

        [Header("Coarse octave")]
        [Range(0, 0.9f)]
        public float OutScale1_ = 30f;

        [Header("Fine octave")]
        [Range(0, 0.9f)]
        public float OutScale2_ = 5f;

        [Header("Global octave")]
        [Range(0, 0.9f)]
        public float OutScale3_ = 30f;


        public Color LandColor { get { return LandColor_; } }
        public float Height { get { return Height_; } }

        public float OutScale1 { get { return OutScale1_; } }
        public float OutScale2 { get { return OutScale2_; } }

        public float OutScale3 { get { return OutScale3_; } }

        public static IZoneNoiseSettings Lerp(IEnumerable<ZoneSettings> zones, ZoneRatio ratio)
        {
            var outScale1 = 0f;
            var outScale2 = 0f;
            var outScale3 = 0f;
            var inScale1 = 0f;
            var inScale2 = 0f;
            var inScale3 = 0f;
            var height = 0f;

            foreach (var zoneNoiseSettingse in zones)
            {
                var zoneRatio = ratio[zoneNoiseSettingse.Type];
                height += zoneNoiseSettingse.Height * zoneRatio;
                outScale1 += zoneNoiseSettingse.OutScale1 * zoneRatio;
                outScale2 += zoneNoiseSettingse.OutScale2 * zoneRatio;
                outScale3 += zoneNoiseSettingse.OutScale3 * zoneRatio;
            }

            var result = new ZoneSettings2(Color.black, height, inScale1, inScale2, inScale3, outScale1, outScale2, outScale3);

            return result;
        }
    }
}