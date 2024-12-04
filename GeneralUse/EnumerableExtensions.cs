using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class EnumerableExtensions
    {
        public static float Median(this IEnumerable<float>? source)
        {
            if (source is null || !source.Any())
            {
                throw new InvalidOperationException("Cannot compute median for a null or empty set.");
            }

            var sortedList = source.OrderBy(number => number).ToList();

            int itemIndex = sortedList.Count / 2;

            if (sortedList.Count % 2 == 0)
            {
                // Even number of items.
                return (sortedList[itemIndex] + sortedList[itemIndex - 1]) / 2;
            }
            else
            {
                // Odd number of items.
                return sortedList[itemIndex];
            }
        }

        public static Vector2 Average(this IEnumerable<Vector2>? source)
        {
            if (source is null || !source.Any())
            {
                throw new InvalidOperationException("Cannot compute average for a null or empty set.");
            }

            return new Vector2(source.Average(v => v.x), source.Average(v => v.y));
        }
    }
}