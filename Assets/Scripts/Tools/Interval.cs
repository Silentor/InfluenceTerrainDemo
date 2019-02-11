using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Inclusive range between min and max float values
    /// </summary>
    public readonly struct Interval : IEquatable<Interval>
    {
        public readonly float Min;
        public readonly float Max;
        public bool IsEmpty => Min == 0 && Max == 0;

        public static readonly Interval Empty = new Interval(0, 0);

        public Interval(float min, float max)
        {
            if (max < min)
            {
                max = min = 0;
            }

            Min = min;
            Max = max;
        }

        public bool Contains(float value)
        {
            return value >= Min && value <= Max;
        }

        public bool IsIntersect(Interval other)
        {
            return !(other.Min > Max || other.Max < Min);
        }

        public IEnumerable<Interval> Subtract(Interval other)
        {
            if (IsEmpty)
            {
                yield return Empty;
                yield break;
            }

            if (other.IsEmpty || !IsIntersect(other))
            {
                yield return this;
                yield break;
            }

            if (other.Min <= Min)
            {
                if (other.Max < Max)
                {
                    yield return new Interval(other.Max, Max);
                }
            }
            else
            {
                yield return new Interval(Min, other.Min);

                if (other.Max < Max)
                {
                    yield return new Interval(other.Max, Max);
                }
            }

        }

        public override string ToString()
        {
            return $"({Min}-{Max}";
        }

        #region IEquatable

        public bool Equals(Interval other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Interval other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Min.GetHashCode();
                hashCode = (hashCode * 397) ^ Max.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Interval left, Interval right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Interval left, Interval right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
