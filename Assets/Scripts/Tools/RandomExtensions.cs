using UnityEngine;

namespace TerrainDemo.Tools
{
    public static class RandomExtensions
    {
        public static int GetRandomRange(this Vector2 value)
        {
            return Mathf.RoundToInt(Random.Range(value.x, value.y));
        }
    }
}
