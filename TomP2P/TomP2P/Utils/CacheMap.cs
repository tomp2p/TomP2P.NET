using System.Collections.Generic;
using TomP2P.Extensions;

namespace TomP2P.Utils
{
    public class CacheMap<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private readonly int _maxEntries;
        private readonly bool _updateEntryOnInsert;

        // .NET-specific:
        // keeps track of the order of items (key only)
        private readonly LinkedList<TKey> _linkedList;

        /// <summary>
        /// Creates a new CacheMap with a fixed capacity.
        /// </summary>
        /// <param name="maxEntries">The number of entries that can be stored in this map.</param>
        /// <param name="updateEntryOnInstert">True to update (overwrite) values. 
        /// False to not overwrite the values if there is a value.</param>
        public CacheMap(int maxEntries, bool updateEntryOnInstert)
        {
            _maxEntries = maxEntries;
            _updateEntryOnInsert = updateEntryOnInstert;
            _linkedList = new LinkedList<TKey>();
        }

        public new TValue Add(TKey key, TValue value)
        {
            if (_updateEntryOnInsert)
            {
                var retVal = this.Put(key, value);
                _linkedList.AddLast(key);
                RemoveEldestEntry();
                return retVal;
            }
            return PutIfAbsent(key, value);
        }

        /// <summary>
        /// If the key is not associated with a value, associate it with the value.
        /// </summary>
        /// <param name="key">The key with which the value is to be associated.</param>
        /// <param name="value">The value to be associated with the key.</param>
        /// <returns>The value previously associated with the key, or null if there was no mapping for this key.</returns>
        private TValue PutIfAbsent(TKey key, TValue value)
        {
            if (!ContainsKey(key))
            {
                var retVal = this.Put(key, value);
                _linkedList.AddLast(key);
                RemoveEldestEntry();
                return retVal;
            }
            return base[key];
        }

        /// <summary>
        /// From Java. Enables a dictionary to keep a certain capacity. It is invoked
        /// by Put() and PutAll().
        /// </summary>
        private void RemoveEldestEntry()
        {
            // .NET-specific: it is ok to remove the eldest element in this mehtod directly
            // -> return false
            if (Count > _maxEntries)
            {
                // remove from odering list
                var eldest = _linkedList.First;
                _linkedList.Remove(eldest);

                // remove from dictionary
                Remove(eldest.Value);
            }
        }
    }
}
