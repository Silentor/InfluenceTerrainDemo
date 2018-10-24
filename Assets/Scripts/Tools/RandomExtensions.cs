using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TerrainDemo.Tools
{
    public static class RandomExtensions
    {
        public static int GetRandomRange(this Vector2 value)
        {
            return Mathf.RoundToInt(UnityEngine.Random.Range(value.x, value.y));
        }

        public static int GetRandomRange(this Vector2 value, System.Random random)
        {
            return Mathf.RoundToInt((float)(value.x + random.NextDouble() *  (value.y - value.x)));
        }

        public static int GetRandomRange(this Vector2Int value)
        {
            return UnityEngine.Random.Range(value.x, value.y + 1);
        }

        public static int GetRandomRange(this Vector2Int value, System.Random random)
        {
            return random.Next(value.x, value.y + 1);
        }

        public static T GetRandomItem<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(UnityEngine.Random.Range(0, collection.Count()));
        }

        public static T GetRandomItem<T>(this IEnumerable<T> collection, System.Random random)
        {
            return collection.ElementAt(random.Next(0, collection.Count()));
        }

        public static T GetRandomItem<T>(this IEnumerable<T> collection, Random random)
        {
            return collection.ElementAt(random.Range(collection.Count()));
        }
    }
}
