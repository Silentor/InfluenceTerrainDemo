using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public struct ZoneSettings2 : IZoneSettings
    {
        public Color LandColor { get; private set; }
        public float Height { get; private set; }
        public float InScale1 { get; private set; }
        public float InScale2 { get; private set; }
        public float InScale3 { get; private set; }
        public float OutScale1 { get; private set; }
        public float OutScale2 { get; private set; }
        public float OutScale3 { get; private set; }

        public ZoneSettings2(Color landColor, float height, float inScale1, float inScale2, float inScale3, float outScale1, float outScale2, float outScale3) : this()
        {
            LandColor = landColor;
            Height = height;
            InScale1 = inScale1;
            InScale2 = inScale2;
            InScale3 = inScale3;
            OutScale1 = outScale1;
            OutScale2 = outScale2;
            OutScale3 = outScale3;
        }
    }
}
