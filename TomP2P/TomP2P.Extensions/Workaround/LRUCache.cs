using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// Simple LRU cache implementation for .NET.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<LruCacheItem<TKey, TValue>>> _cacheMap;
        private readonly LinkedList<LruCacheItem<TKey, TValue>> _lruList;

        /// <summary>
        /// Creates a LRU cache with a fixed capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public LruCache(int capacity)
        {
            _capacity = capacity;
            _cacheMap = new Dictionary<TKey, LinkedListNode<LruCacheItem<TKey, TValue>>>();
            _lruList = new LinkedList<LruCacheItem<TKey, TValue>>();
        }

        /// <summary>
        /// Associates the specified value with the specified key in this map.
        /// If the map previously contained a mapping for the key, the old value is replaced.
        /// The LRU priority for this item is updated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual TValue Add(TKey key, TValue value)
        {
            if (_cacheMap.Count >= _capacity)
            {
                RemoveEldest();
            }
            var cacheItem = new LruCacheItem<TKey, TValue>(key, value);
            var node = new LinkedListNode<LruCacheItem<TKey, TValue>>(cacheItem);

            // add to cache
            var retVal = _cacheMap.Put(key, node);

            // add to LRU priority
            UpdateLruPriority(node);

            return retVal.Value.Value;
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
            LinkedListNode<LruCacheItem<TKey, TValue>> node;
            if (_cacheMap.TryGetValue(key, out node))
            {
                UpdateLruPriority(node);
                return node.Value.Value;
            }
            return default(TValue);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ContainsKey(TKey key)
        {
            return _cacheMap.ContainsKey(key);
        }

        private void RemoveEldest()
        {
            // remove from LRU priority
            var node = _lruList.First;
            _lruList.RemoveFirst();

            // remove from cache
            _cacheMap.Remove(node.Value.Key);
        }

        private void UpdateLruPriority(LinkedListNode<LruCacheItem<TKey, TValue>> node)
        {
            // if item exists already, reset priority
            if (_lruList.Contains(node.Value))
            {
                _lruList.Remove(node);
            }
            // set priority to max
            _lruList.AddLast(node);
        }
    }

    internal class LruCacheItem<TKey, TValue>
    {
        public LruCacheItem(TKey k, TValue v)
        {
            Key = k;
            Value = v;
        }
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }
    }
}
