using System.Collections.Generic;
using System.Linq;

namespace RikusGameDevToolbox.Geometry2d
{
    internal static class Util
    {
        /// <summary>
        /// Returns the pairs of consequent elements in list. 
        /// </summary>
        internal static IEnumerable<(T, T)> Pairs<T>(List<T> list)
        {
            for (int i = 0; i < list.Count-1; i++)
            {
                yield return (list.ElementAt(i), list.ElementAt(i + 1));
            }
        }
        
        /// <summary>
        /// Returns the pairs of consequent elements in array. 
        /// </summary>
        internal static IEnumerable<(T, T)> Pairs<T>(T[] array)
        {
            for (int i = 0; i < array.Length - 1; i++)
            {
                yield return (array.ElementAt(i), array.ElementAt(i + 1));
            }
        }
        
        /// <summary>
        /// Returns the pairs of consequent elements in list. The last pair is the last and the first element.
        /// </summary>
        internal static IEnumerable<(T, T)> LoopingPairs<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                yield return i == list.Count - 1
                    ? (list.ElementAt(i), list.ElementAt(0))
                    : (list.ElementAt(i), list.ElementAt(i + 1));
            }
        }
        
        /// <summary>
        /// Returns the pairs of consequent elements in array. The last pair is the last and the first element.
        /// </summary>
        internal static IEnumerable<(T, T)> LoopingPairs<T>(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                yield return i == array.Length - 1
                    ? (array.ElementAt(i), array.ElementAt(0))
                    : (array.ElementAt(i), array.ElementAt(i + 1));
            }
        }
    }
}