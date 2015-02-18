using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// Simple LRU cache implementation for .NET.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LruCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, TValue> _cacheMap;
        private readonly LinkedList<TKey> _lruList; // ordering of keys is LRU

        /// <summary>
        /// Creates a LRU cache with a fixed capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public LruCache(int capacity)
        {
            _capacity = capacity;
            _cacheMap = new Dictionary<TKey, TValue>();
            _lruList = new LinkedList<TKey>();
        }

        /// <summary>
        /// Associates the specified value with the specified key in this map.
        /// If the map previously contained a mapping for the key, the old value is replaced.
        /// The LRU priority for this item is updated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>The previous value associated with the key, or the default value if there
        /// was no mapping for the key.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual TValue Add(TKey key, TValue value)
        {
            if (_cacheMap.Count >= _capacity)
            {
                RemoveEldest();
            }

            // add to cache
            var retVal = _cacheMap.Put(key, value);

            // add to LRU priority
            UpdateLruPriority(key);

            return retVal;
        }

        /// <summary>
        /// Removes the value associated with this key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The previous value associated with the key, or the default value if there
        /// was no mapping for the key.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual TValue Remove(TKey key)
        {
            TValue value;
            if (_cacheMap.TryGetValue(key, out value))
            {
                // remove from cache
                _cacheMap.Remove(key);

                // remove from LRU priority
                _lruList.Remove(key);

                return value;
            }
            return default(TValue);
        }

        /// <summary>
        /// Gets the value associated with this key.
        /// The LRU priority for this item is updated.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public TValue Get(TKey key)
        {
            TValue value;
            if (_cacheMap.TryGetValue(key, out value))
            {
                UpdateLruPriority(key);
                return value;
            }
            return default(TValue);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ContainsKey(TKey key)
        {
            return _cacheMap.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ContainsValue(TValue value)
        {
            return _cacheMap.ContainsValue(value);
        }

        private void RemoveEldest()
        {
            // remove from LRU priority
            var eldestKey = _lruList.First.Value;
            _lruList.Remove(eldestKey);

            // remove from cache
            _cacheMap.Remove(eldestKey);
        }

        private void UpdateLruPriority(TKey key)
        {
            // if item exists already, reset priority
            if (_lruList.Contains(key))
            {
                _lruList.Remove(key);
            }
            // set priority to max
            _lruList.AddLast(key);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            _cacheMap.Clear();
            _lruList.Clear();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Count()
        {
            return _cacheMap.Count;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int GetHashCode()
        {
            return _cacheMap.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Dictionary<TKey, TValue>.KeyCollection KeySet()
        {
            return _cacheMap.Keys;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Dictionary<TKey, TValue>.ValueCollection Values()
        {
            return _cacheMap.Values;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _cacheMap.GetEnumerator();
        }
    }
}
