using System;
using System.Collections.Generic;

namespace RikusGameDevToolbox.GeneralUse
{
    // This collection can be used like Dictionary, but the data is held in an array in continuous fashion.
    // The array can be accessed directly via Span.
    
    public class SpannableDictionary<TKey, TValue>
    {
        private Dictionary<TKey, int> _KeyToIndex;
        private Dictionary<int, TKey> _IndexToKey;
        private TValue[] _array;

        public int Count => _KeyToIndex.Count;
        
        public SpannableDictionary(int maxNumEntries)
        {
            _KeyToIndex = new Dictionary<TKey, int>();
            _IndexToKey = new Dictionary<int, TKey>();
            _array = new TValue[maxNumEntries];
        }


        public TValue Get(TKey key)
        {
            return _array[_KeyToIndex[key]];
        }
        
        public void Add(TKey key, TValue value)
        {
            int index = _KeyToIndex.Count;
            _array[index] = value;
            _KeyToIndex.Add(key, index);
            _IndexToKey.Add(index, key);
        }

        public void Remove(TKey key)
        {
            int index = _KeyToIndex[key];
            int lastIndex = _KeyToIndex.Count - 1;

            _KeyToIndex.Remove(key);
            _IndexToKey.Remove(index);
            
            if (index == lastIndex) return;
            
            // Move last element in array to the empty spot so that the array is contiguous:
            _array[index] = _array[lastIndex];

            //..and adjust the dictionaries accordingly:
            TKey lastElementKey = _IndexToKey[lastIndex];
            _KeyToIndex[lastElementKey] = index;
            _IndexToKey[index] = lastElementKey;
            _IndexToKey.Remove(lastIndex);
        }

        public void Update(TKey key, TValue value)
        {
            _array[_KeyToIndex[key]] = value;
        }

        public void Clear()
        {
            _KeyToIndex.Clear();
            _IndexToKey.Clear();
        }
        
        public Span<TValue> AsSpan()
        {
            return _array.AsSpan().Slice(0, _KeyToIndex.Count);
        }
        
        public int SpanIndexOf(TKey key)
        {
            return _KeyToIndex[key];
        }
    }
}
