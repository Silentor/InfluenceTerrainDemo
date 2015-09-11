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

        public ZoneRatio(ZoneType maxZoneType)
        {
            _maxZoneType = maxZoneType;
            _value = new float[(int)maxZoneType + 1];
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


        public float this[ZoneType i]
        {
            get { return _value[(int)i]; }
            set { _value[(int)i] = value; }
        }

        public static ZoneRatio operator *(ZoneRatio a, float b)
        {
            var result = new ZoneRatio(a._maxZoneType);
            for (int i = 0; i < a._value.Length; i++)
                result._value[i] = a._value[i]*b;

            return result;
        }

        public static ZoneRatio operator *(float a, ZoneRatio b)
        {
            var result = new ZoneRatio(b._maxZoneType);
            for (int i = 0; i < b._value.Length; i++)
                result._value[i] = b._value[i] * a;

            return result;
        }

        public static ZoneRatio operator +(ZoneRatio a, ZoneRatio b)
        {
            var result = new ZoneRatio(a._maxZoneType);

            for (int i = 0; i < a._value.Length; i++)
                result._value[i] = a._value[i] + b._value[i];

            return result;
        }

        private readonly float[] _value;
        private readonly ZoneType _maxZoneType;
    }
}
