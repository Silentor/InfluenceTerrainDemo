using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class ZoneRatio
    {
        public readonly GeneratorSettings Zone1;
        public readonly GeneratorSettings Zone2;

        public readonly float Ratio1;
        public readonly float Ratio2;

        public ZoneRatio(GeneratorSettings zone1, GeneratorSettings zone2, float ratio1)
        {
            Zone1 = zone1;
            Zone2 = zone2;
            Ratio1 = Mathf.Clamp01(ratio1);
            Ratio2 = 1 - Ratio1;
        }
    }
}
