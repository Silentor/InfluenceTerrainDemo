using Haus.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Wrapper around some random generator (currently XOR Shift)
    /// </summary>
    public class Random
    {
        public readonly int Seed;

        public Random( ) : this (Guid.NewGuid().GetHashCode())
        {
        }

        public Random(int seed)
        {
            Seed = seed;
            _random = new XorShiftRandom( (ulong)seed );
        }

        public int Range(int min, int maxInclusive)
        {
            var range = maxInclusive - min + 1;
            return (int)(_random.NextUInt32() % range) + min;
        }

        public float Range(float min, float max)
        {
            var range = max - min;
            return (_random.NextFloat() % range) + min;
        }

        public int Range(Vector2Int range)
        {
            return Range(range.x, range.y);
        }

        public int Range(int maxExclusive)
        {
            return (int)(_random.NextUInt32() % maxExclusive);
        }

        public double Value()
        {
            return _random.NextDouble();
        }

        public T Item<T>(IEnumerable<T> collection)
        {
            return collection.ElementAt(Range(collection.Count()));
        }

        public T Item<T>(IList<T> collection)
        {
            return collection[Range(collection.Count)];
        }

        private readonly XorShiftRandom _random;
    }
}
