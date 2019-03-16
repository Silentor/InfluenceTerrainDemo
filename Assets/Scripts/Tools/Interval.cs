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

        public float Lenght => Max - Min;

        public static readonly Interval Empty = new Interval(0, 0);

        public bool IsEmpty => Min == 0 && Max == 0;

        public bool IsNAN => float.IsNaN(Min) || float.IsNaN(Max);

        public bool IsInfinity => float.IsInfinity(Min) || float.IsInfinity(Max);

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

        public bool IsIntersects(Interval other)
        {
            return !(other.Min > Max || other.Max < Min);
        }

        public Interval Intersect(Interval other)
        {
            return new Interval(Math.Max(Min, other.Min), Math.Min(Max, other.Max));
        }

        public IEnumerable<Interval> Subtract2(Interval other)
        {
            var result = Subtract(other);

            if (!result.Item1.IsEmpty)
                yield return result.Item1;

            if (!result.Item2.IsEmpty)
                yield return result.Item2;
        }

        public (Interval minPart, Interval maxPart) Subtract(Interval other)
        {
            if (IsEmpty)
            {
                return (Empty, Empty);
            }

            if (other.IsEmpty || !IsIntersects(other))
            {
                return (this, Empty);
            }

            Interval first = Empty, second = Empty;
            if (other.Min <= Min)
            {
                if (other.Max < Max)
                {
                    first = new Interval(other.Max, Max);
                }
            }
            else
            {
                first = new Interval(Min, other.Min);

                if (other.Max < Max)
                {
                    second = new Interval(other.Max, Max);
                }
            }

            return (first, second);

        }

        public override string ToString()
        {
            return IsEmpty ? "(Empty)" : $"({Min} {Max})";
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
