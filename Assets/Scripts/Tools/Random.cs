using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainDemo.Tools
{
    /// <summary>
    /// Wrapper around some random generator (currently BCL Random)
    /// </summary>
    public class Random
    {
        public readonly int Seed;

        public Random(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int Range(int min, int maxInclusive)
        {
            return _random.Next(min, maxInclusive + 1);
        }

        public float Range(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public int Range(Vector2Int range)
        {
            return _random.Next(range.x, range.y + 1);
        }

        public int Range(int maxExclusive)
        {
            return _random.Next(maxExclusive);
        }

        public T Item<T>(IEnumerable<T> collection)
        {
            return collection.ElementAt(_random.Next(collection.Count()));
        }

        public T Item<T>(IList<T> collection)
        {
            return collection[_random.Next(collection.Count)];
        }

        private readonly System.Random _random;
    }
}
