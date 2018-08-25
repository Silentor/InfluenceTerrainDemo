using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace TerrainDemo.Tools
{
    public static class EnumerableExtensions
    {
        public static string ToJoinedString<T>(this IEnumerable<T> elements)
        {
            return string.Join(", ", elements.Select(e => e.ToString()).ToArray());
        }

        public static string ToJoinedString<T>(this IEnumerable<T> elements, string delimiter)
        {
            return string.Join(delimiter, elements.Select(e => e.ToString()).ToArray());
        }

        public static string ToJoinedString<T>(this IEnumerable<T> elements, [NotNull] Func<T, string> toString)
        {
            if (toString == null) throw new ArgumentNullException(nameof(toString));

            return string.Join(", ", elements.Select(toString).ToArray());
        }
    }
}
