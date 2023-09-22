using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RikusGameDevToolbox.Discrete3d
{
    /// <summary>
    /// Stores objects with Vec position. Several objects can be in one
    /// position. Objects are stored in array for duration.
    /// </summary>
    public class PositionArray<T> where T : class
    {
        Vec size;
        T[,,][] items;
        public int count { get; private set; }

        public PositionArray(Vec size)
        {
            this.size = size;
            items = new T[size.x, size.y, size.z][];
            count = 0;
        }



        // Add object to given position
        public void Add(Vec position, T item)
        {

            T[] itemsAtPos = items[position.x, position.y, position.z];

            if (itemsAtPos == null)
            {
                items[position.x, position.y, position.z] = new T[] { item };
                count++;
            }
            else
            {
                Array.Resize(ref itemsAtPos, itemsAtPos.Length + 1);
                itemsAtPos[itemsAtPos.Length - 1] = item;
                items[position.x, position.y, position.z] = itemsAtPos;
                count++;
            }
        }



        public T[] ItemsAt(Vec position)
        {
            T[] itemsAtPos = items[position.x, position.y, position.z];

            return itemsAtPos ?? new T[0];


            // TODO: Prevent user from messing with items

        }

        // Deletes object at given position with index n.
        //TODO: Error handling
        public void RemoveAt(Vec position, int n)
        {
            T[] itemsAtPos = items[position.x, position.y, position.z];

            if (itemsAtPos.Length - 1 < n)
            {
                Debug.LogError("Tried to remove non-existing item.");
            }
            else
            {
                T[] newArray = new T[itemsAtPos.Length - 1];


                for (int i = 0; i < n; i++)
                {
                    newArray[i] = itemsAtPos[i];
                }
                for (int i = n; i < newArray.Length; i++)
                {
                    newArray[i] = itemsAtPos[i + 1];
                }

                items[position.x, position.y, position.z] = newArray;
                count--;

            }
        }

        // Deletes given object at given position.
        // TODO: Error handling
        public void RemoveAt(Vec position, T item)
        {
            T[] itemsAtPosition = items[position.x, position.y, position.z];

            if (itemsAtPosition == null || itemsAtPosition.Length == 0)
                throw new Exception("Tried to remove an Obj from " + position.ToString() + " - No items at that position.");


            for (int i = 0; i < itemsAtPosition.Length; i++)
            {
                if (itemsAtPosition[i] == item)
                {
                    RemoveAt(position, i);
                    return;
                }

            }

            throw new Exception("Tried to remove an Obj from " + position.ToString() + " - Cannot be found.");
        }

        // TODO: Remove this and replace with json stuff
        public List<T> AsList()
        {
            List<T> list = new List<T>();


            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        T[] itemsAtPos = ItemsAt(new Vec(x, y, z));
                        if (itemsAtPos != null)
                            list.AddRange(itemsAtPos);
                    }
                }
            }


            return list;
        }

    }
}