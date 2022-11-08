using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RikusGameDevToolbox.Discrete3d
{
    /// <summary>
    /// Stores objects with Vec as key (usually for position). Several objects can be in one 
    /// position.
    /// </summary> 
    public class VecBasedCollection<T> : IEnumerable<T[]> where T : class
    {
        protected Dictionary<Vec, T[]> items = new Dictionary<Vec, T[]>();

        // Contents can be iterated as arrays of objects with same key
        public IEnumerator<T[]> GetEnumerator()
        {
            foreach (T[] itemArray in items.Values)
            {
                yield return itemArray;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        // Add object to given key
        public void Add(Vec key, T item)
        {
            if (items.ContainsKey(key))
            {
                T[] array = items[key];
                Array.Resize(ref array, array.Length + 1);
                array[array.Length - 1] = item;
                items[key] = array;
            }
            else
            {
                items.Add(key, new T[1] { item });
            }

        }

        /// <summary>
        /// Array of items with given key, null if there's none.
        /// </summary>
        public T[] ItemsAt(Vec key)
        {
            T[] result;

            if (items.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        // Deletes object at given key with index n.
        public void RemoveAt(Vec key, int n)
        {
            if (!items.ContainsKey(key))
            {
                throw new System.Exception("Tried to remove non-existing item.");
            }

            T[] array = items[key];

            if (array.Length - 1 < n)
            {
                throw new System.Exception("Tried to remove non-existing item.");
            }

            if (array.Length == 1)
            {
                items.Remove(key);
            }
            else
            {
                T[] newArray = new T[array.Length - 1];


                for (int i = 0; i < n; i++)
                {
                    newArray[i] = array[i];
                }
                for (int i = n; i < newArray.Length; i++)
                {
                    newArray[i] = array[i + 1];
                }

                items[key] = newArray;

            }
        }

        public bool HasItem(Vec key, T item)
        {
            if (items.ContainsKey(key)) return false;

            T[] itemsWithKey = ItemsAt(key);
            foreach (T i in itemsWithKey)
            {
                if (i == item) return true;
            }
            return false;
        }

        // Deletes given object at given key.
        public void RemoveAt(Vec key, T item)
        {
            bool success = false;
            T[] result;
            if (items.TryGetValue(key, out result))
            {
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i].Equals(item))
                    {
                        RemoveAt(key, i);
                        success = true;
                    }
                }
            }
            if (!success)
            {
                throw new System.Exception("Tried to remove non-existing item.");
            }
        }

        public void Clear()
        {
            items.Clear();
        }


        public List<T> ItemsInside(Box box)
        {
            List<T> list = new List<T>();

            foreach (Vec pos in items.Keys)
            {
                if (box.IsInside(pos))
                {
                    list.AddRange(items[pos]);
                }
            }

            return list;
        }

        public List<T> ItemsOutside(Box box)
        {
            List<T> list = new List<T>();

            foreach (Vec pos in items.Keys)
            {
                if (box.IsOutside(pos))
                {
                    list.AddRange(items[pos]);
                }
            }

            return list;
        }



        public int Count()
        {
            int count = 0;
            foreach (Vec pos in items.Keys)
            {
                count += items[pos].Length;
            }

            return count;
        }

    }
}