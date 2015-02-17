using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Utils
{
    public class ConcurrentCacheMap<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Number of segments that can be accessed concurrently.
        /// </summary>
        public const int SegmentNr = 16;
        /// <summary>
        /// Max. number of entries that the map can hold until the least recently used gets replaced.
        /// </summary>
        public const int MaxEntries = 1024;
        /// <summary>
        /// Time to live for a value. The value may stay longer in the map, but it is considered invalid.
        /// </summary>
        public const int DefaultTimeToLive = 60;

        private readonly CacheMap<TKey, ExpiringObject>[] _segments;

        private readonly int _timeToLiveSeconds;
        private readonly bool _refreshTimeout;
        private readonly VolatileInteger _removedCounter = new VolatileInteger();

        /// <summary>
        /// Creates a new instance of ConcurrentCacheMap using the default values
        /// and a <see cref="CacheMap{TKey,TValue}"/> for the internal data structure.
        /// </summary>
        public ConcurrentCacheMap()
            : this(DefaultTimeToLive, MaxEntries, true)
        { }

        /// <summary>
        /// Creates a new instance of ConcurrentCacheMap using the supplied values
        /// and a <see cref="CacheMap{TKey,TValue}"/> for the internal data structure.
        /// </summary>
        /// <param name="timeToLiveSeconds">The time-to-live value (seconds).</param>
        /// <param name="maxEntries">The maximum number of entries until items gets replaced with LRU.</param>
        public ConcurrentCacheMap(int timeToLiveSeconds, int maxEntries)
            : this(timeToLiveSeconds, maxEntries, true)
        { }

        /// <summary>
        /// Creates a new instance of ConcurrentCacheMap using the default values
        /// and a <see cref="CacheMap{TKey,TValue}"/> for the internal data structure.
        /// </summary>
        /// /// <param name="timeToLiveSeconds">The time-to-live value (seconds).</param>
        /// <param name="maxEntries">The maximum number of entries until items gets replaced with LRU.</param>
        /// <param name="refreshTimeout">If set to true, timeout will be reset in case of PutIfAbsent().</param>
        public ConcurrentCacheMap(int timeToLiveSeconds, int maxEntries, bool refreshTimeout)
        {
            _segments = new CacheMap<TKey, ExpiringObject>[SegmentNr];
            int maxEntriesPerSegment = maxEntries/SegmentNr;
            for (int i = 0; i < SegmentNr; i++)
            {
                // set updateOnInsert to true, since it should behave as a regular map
                _segments[i] = new CacheMap<TKey, ExpiringObject>(maxEntriesPerSegment, true);
            }
            _timeToLiveSeconds = timeToLiveSeconds;
            _refreshTimeout = refreshTimeout;
        }

        /// <summary>
        /// An object that also holds expiration information.
        /// </summary>
        private class ExpiringObject
        {
            
        }
    }
}
