using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TerrainDemo.Tools;

namespace TerrainDemo.Tools
{
    public static class RandomExtensions
    {
        private static readonly Random _random = new Random( );

        public static int GetRandomRange(this Vector2 value)
        {
            return Mathf.RoundToInt(_random.Range(value.x, value.y));
        }

        public static int GetRandomRange(this Vector2 value, Random random)
        {
            return Mathf.RoundToInt(random.Range( value.x, value.y ));
        }

        public static int GetRandomRange(this Vector2Int value)
        {
            return _random.Range(value.x, value.y + 1);
        }

        public static T GetRandomItem<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(_random.Range(0, collection.Count()));
        }

        public static T GetRandomItem<T>(this IEnumerable<T> collection, Random random)
        {
            return collection.ElementAt(random.Range(collection.Count()));
        }
    }
}
