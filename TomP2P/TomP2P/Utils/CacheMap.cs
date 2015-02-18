using TomP2P.Extensions.Workaround;

namespace TomP2P.Utils
{
    /// <summary>
    /// The CacheMap is a LRU cache with a given capacity. The elements that do not fit into the cache will be removed.
    /// The flag "updateEntryOnInsert" will determine if Put() or PutIfAbsent() will be used. This is useful for entries 
    /// that have timing information and that should not be updated if the same key is going to be used.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class CacheMap<TKey, TValue> : LruCache<TKey, TValue>
        where TValue : class
    {
        private readonly bool _updateEntryOnInsert;

        /// <summary>
        /// Creates a new CacheMap with a fixed capacity.
        /// </summary>
        /// <param name="maxEntries">The number of entries that can be stored in this map.</param>
        /// <param name="updateEntryOnInsert">True to update (overwrite) values. 
        /// False to not overwrite the values if there is a value.</param>
        public CacheMap(int maxEntries, bool updateEntryOnInsert)
            : base(maxEntries)
        {
            _updateEntryOnInsert = updateEntryOnInsert;
        }

        public override TValue Add(TKey key, TValue value)
        {
            if (_updateEntryOnInsert)
            {
                return base.Add(key, value);
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
                return base.Add(key, value);
            }
            return base.Get(key);
        }
    }
}