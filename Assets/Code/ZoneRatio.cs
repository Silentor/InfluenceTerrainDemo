using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    /// <summary>
    /// Collection of all zones influences for some point
    /// </summary>
    public class ZoneRatio
    {
        public int ZonesCount { get { return _value.Length; } }

        public ZoneRatio(int zonesCount)
        {
            _value = new float[zonesCount];
        }

        public ZoneRatio(float[] ratios)
        {
            _value = ratios;
        }

        public void Normalize()
        {
            //Normalize
            var sum = 0f;
            for (var i = 0; i < _value.Length; i++)
                sum += _value[i];

            for (var i = 0; i < _value.Length; i++)
                _value[i] /= sum;
        }


        public float this[int i]
        {
            get { return _value[i]; }
            set { _value[i] = value; }
        }

        public static ZoneRatio operator *(ZoneRatio a, float b)
        {
            var result = new ZoneRatio(a.ZonesCount);
            for (int i = 0; i < a.ZonesCount; i++)
            {
                result[i] = a[i]*b;
            }

            return result;
        }

        public static ZoneRatio operator *(float a, ZoneRatio b)
        {
            var result = new ZoneRatio(b.ZonesCount);
            for (int i = 0; i < b.ZonesCount; i++)
            {
                result[i] = b[i] * a;
            }

            return result;
        }

        public static ZoneRatio operator +(ZoneRatio a, ZoneRatio b)
        {
            var result = new ZoneRatio(a.ZonesCount);

            for (int i = 0; i < a.ZonesCount; i++)
                result[i] = a[i] + b[i];

            return result;
        }

        private readonly float[] _value;
    }
}
