using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace TerrainDemo
{
    /// <summary>
    /// Collection of all zones influences for some point
    /// </summary>
    public struct ZoneRatio : IEnumerable<ZoneValue>
    {
        public bool IsEmpty { get { return _value == null || _value.Length == 0; } }

        public ZoneRatio([NotNull] ZoneValue[] values, int zonesCount)
        {
            if (values == null) throw new ArgumentNullException("values");
            //Fast debug precondition
            for(var i = 0; i < values.Length; i++)
                if(values[i].Zone == ZoneType.Empty)
                    throw new ArgumentException("values");

            Array.Sort(values);
            _value = new ZoneValue[zonesCount];
            Array.Copy(values, _value, zonesCount);
            Normalize();
        }

        private ZoneRatio([NotNull] ZoneValue[] values)
        {
            if (values == null) throw new ArgumentNullException("values");

            _value = values;
            Normalize();
        }

        public void Normalize()
        {
            //Normalize
            var sum = 0d;
            for (var i = 0; i < _value.Length; i++)
            {
                if (_value[i].Value > 0)
                    sum += _value[i].Value;
                else
                    _value[i].Value = 0f;
            }

            if (sum != 0)
                for (var i = 0; i < _value.Length; i++)
                    _value[i].Value /= sum;
            else
                for (var i = 0; i < _value.Length; i++)
                    _value[i].Value = 0;
        }

        /// <summary>
        /// Return only zone ratios larger than threshold
        /// </summary>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public ZoneRatio Pack(float threshold)
        {
            if (IsEmpty) return this;

            var result = new ZoneValue[_value.Length];

            int i;
            for (i = 0; i < _value.Length; i++)
                if (_value[i].Value >= threshold)
                    result[i] = _value[i];

            Array.Resize(ref result, i + 1);

            return new ZoneRatio(result);
        }

        /// <summary>
        /// Return only largest <see cref="remainValues"/> ratios
        /// </summary>
        /// <param name="remainValues"></param>
        /// <returns></returns>
        public ZoneRatio Pack(int remainValues)
        {
            if (IsEmpty) return this;

            if (remainValues > _value.Length) remainValues = _value.Length;

            var result = new ZoneValue[remainValues];

            for (var i = 0; i < remainValues; i++)
                result[i] = _value[i];

            return new ZoneRatio(result);
        }

        public double this[ZoneType i]
        {
            get
            {
                foreach (var zoneValue in _value)
                    if (zoneValue.Zone == i)
                        return zoneValue.Value;

                return 0;
            }
        }

        public ZoneValue this[int i]
        {
            get { return _value[i]; }
        }

        public static ZoneRatio operator *(ZoneRatio a, float b)
        {
            var result = new ZoneValue[a._value.Length];
            for (int i = 0; i < a._value.Length; i++)
                result[i] = new ZoneValue(a._value[i].Zone, a._value[i].Value*b); 

            return new ZoneRatio(result);
        }

        public static ZoneRatio operator *(float a, ZoneRatio b)
        {
            var result = new ZoneValue[b._value.Length];
            for (int i = 0; i < b._value.Length; i++)
                result[i] = new ZoneValue(b._value[i].Zone, b._value[i].Value * a);

            return new ZoneRatio(result);
        }

        public static ZoneRatio operator +(ZoneRatio a, ZoneRatio b)
        {
            var result = new ZoneValue[a._value.Length];

            for (int i = 0; i < a._value.Length; i++)
                result[i] = new ZoneValue(a._value[i].Zone, a._value[i].Value + b._value[i].Value);

            return new ZoneRatio(result);
        }

        private readonly ZoneValue[] _value;

        public IEnumerator<ZoneValue> GetEnumerator()
        {
            return ((IEnumerable<ZoneValue>)_value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            for (var i = 0; i < _value.Length; i++)
                result.AppendFormat("[{0}] - {1}, ", _value[i].Zone, _value[i].Value);

            return result.ToString();
        }
    }

    [DebuggerDisplay("{Zone} = {Value}")]
    public struct ZoneValue : IComparable<ZoneValue>
    {
        public readonly ZoneType Zone;
        public double Value;

        public ZoneValue(ZoneType type, double value)
        {
            Zone = type;
            Value = value;
        }

        public int CompareTo(ZoneValue other)
        {
            return other.Value.CompareTo(Value);
        }
    }
}
