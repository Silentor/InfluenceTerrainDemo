using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class ZoneSettings2 : IZoneNoiseSettings
    {
        public Color LandColor { get; private set; }
        public float Height { get; private set; }
        public float OutScale1 { get; private set; }
        public float OutScale2 { get; private set; }
        public float OutScale3 { get; private set; }

        public ZoneSettings2(Color landColor, float height, float outScale1, float outScale2, float outScale3) 
        {
            LandColor = landColor;
            Height = height;
            OutScale1 = outScale1;
            OutScale2 = outScale2;
            OutScale3 = outScale3;
        }
    }
}
