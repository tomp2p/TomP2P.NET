using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Utils
{
    public class ConcurrentCacheMap<TKey, TValue> where TValue : class 
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
        /// Returns the segment based on the key.
        /// </summary>
        /// <param name="key">The key where the hash code identifies the segment.</param>
        /// <returns>The cache map that corresponds to this segment.</returns>
        private CacheMap<TKey, ExpiringObject> Segment(object key)
        {
            // TODO works? interoperability concern if object.hashCode is impl by framework
            return _segments[(key.GetHashCode() & Int32.MaxValue) % SegmentNr];
        }

        public TValue Put(TKey key, TValue value)
        {
            var newValue = new ExpiringObject(value, Convenient.CurrentTimeMillis(), _timeToLiveSeconds);
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                oldValue = segment.Add(key, newValue);
            }
            if (oldValue == null || oldValue.IsExpired)
            {
                return null;
            }
            return oldValue.Value;
        }

        /// <summary>
        /// This does not reset the timer!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TValue PutIfAbsent(TKey key, TValue value)
        {
            var newValue = new ExpiringObject(value, Convenient.CurrentTimeMillis(), _timeToLiveSeconds);
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                if (!segment.ContainsKey(key))
                {
                    oldValue = segment.Add(key, newValue);
                }
                else
                {
                    oldValue = segment.Get(key);
                    if (oldValue.IsExpired)
                    {
                        segment.Add(key, newValue);
                    }
                    else if (_refreshTimeout)
                    {
                        oldValue = new ExpiringObject(oldValue.Value, Convenient.CurrentTimeMillis(), _timeToLiveSeconds);
                        segment.Add(key, oldValue);
                    }
                }
            }
            if (oldValue == null || oldValue.IsExpired)
            {
                return null;
            }
            return oldValue.Value;
        }

        // TODO in Java, this method allows key to be object
        public TValue Get(TKey key)
        {
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                oldValue = segment.Get(key);
            }
            if (oldValue != null)
            {
                if (Expire(segment, key, oldValue))
                {
                    return null;
                }
                else
                {
                    Logger.Debug("Get found. Key: {0}. Value: {1}.", key, oldValue.Value);
                    return oldValue.Value;
                }
            }
            Logger.Debug("Get not found. Key: {0}.", key);
            return null;
        }

        public TValue Remove(TKey key)
        {
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                oldValue = segment.Remove(key);
            }
            if (oldValue == null || oldValue.IsExpired)
            {
                return null;
            }
            return oldValue.Value;
        }

        public bool Remove(TKey key, TValue value)
        {
            var segment = Segment(key);
            ExpiringObject oldValue;
            bool removed = false;
            lock (segment)
            {
                oldValue = segment.Get(key);
                if (oldValue != null && oldValue.Equals(value) && !oldValue.IsExpired)
                {
                    removed = segment.Remove(key) != null;
                }
            }
            if (oldValue != null)
            {
                Expire(segment, key, oldValue);
            }
            return removed;
        }

        public bool ContainsKey(TKey key)
        {
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                oldValue = segment.Get(key);
            }
            if (oldValue != null)
            {
                if (!Expire(segment, key, oldValue))
                {
                    return true;
                }
            }
            return false;
        }

        // TODO ContainsValue(TValue value) needed?

        public int Size
        {
            get
            {
                var size = 0;
                foreach (var segment in _segments)
                {
                    lock (segment)
                    {
                        ExpireSegment(segment);
                        size += segment.Count();
                    }
                }
                return size;
            }
        }

        public bool IsEmpty
        {
            get
            {
                foreach (var segment in _segments)
                {
                    lock (segment)
                    {
                        ExpireSegment(segment);
                        if (segment.Count() != 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public void Clear()
        {
            foreach (var segment in _segments)
            {
                lock (segment)
                {
                    segment.Clear();
                }
            }
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            foreach (var segment in _segments)
            {
                lock (segment)
                {
                    ExpireSegment(segment);
                    hashCode += segment.GetHashCode();
                }
            }
            return hashCode;
        }

        public ISet<TKey> KeySet
        {
            get
            {
                var keySet = new HashSet<TKey>();
                foreach (var segment in _segments)
                {
                    lock (segment)
                    {
                        ExpireSegment(segment);
                        keySet.UnionWith(segment.KeySet());
                    }
                }
                return keySet;
            }
        }

        public void PutAll<TKey2, TValue2>(IDictionary<TKey2, TValue2> inMap)
            where TKey2 : TKey
            where TValue2 : TValue
        {
            foreach (var kvp in inMap)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                var values = new List<TValue>();
                foreach (var segment in _segments)
                {
                    lock (segment)
                    {
                        // .NET-specific: iterate over KeyValuePairs
                        foreach (var kvp in segment.ToList()) // iterate over copy
                        {
                            var expObj = kvp.Value;
                            if (expObj.IsExpired)
                            {
                                segment.Remove(kvp.Key); // remove from original
                                Logger.Debug("Removed from entry set: {0}.", expObj.Value);
                                _removedCounter.IncrementAndGet();
                            }
                            else
                            {
                                values.Add(kvp.Value.Value);
                            }
                        }
                    }
                }
                return values;
            }
        }

        public ISet<KeyValuePair<TKey, TValue>> EntrySet
        {
            get
            {
                var keyValuePairs = new HashSet<KeyValuePair<TKey, TValue>>();
                foreach (var segment in _segments)
                {
                    lock (segment)
                    {
                        // .NET-specific: iterate over KeyValuePairs
                        foreach (var kvp in segment.ToList()) // iterate over copy
                        {
                            var expObj = kvp.Value;
                            if (expObj.IsExpired)
                            {
                                segment.Remove(kvp.Key); // remove from original
                                Logger.Debug("Removed from entry set: {0}.", expObj.Value);
                            }
                            else
                            {
                                // TODO solve generics problem, in Java first
                                throw new NotImplementedException();
                            }
                        }
                    }
                }
                return keyValuePairs;
            }
        }

        public bool Replace(TKey key, TValue oldValue, TValue newValue)
        {
            var oldValue2 = new ExpiringObject(oldValue, 0, _timeToLiveSeconds);
            var newValue2 = new ExpiringObject(newValue, Convenient.CurrentTimeMillis(), _timeToLiveSeconds);

            var segment = Segment(key);
            ExpiringObject oldValue3;
            bool replaced = false;
            lock (segment)
            {
                oldValue3 = segment.Get(key);
                // TODO equals() seems wrong!
                if (oldValue3 != null && !oldValue3.IsExpired && oldValue2.Equals(oldValue3.Value))
                {
                    segment.Add(key, newValue2);
                    replaced = true;
                }
            }
            if (oldValue3 != null)
            {
                Expire(segment, key, oldValue3);
            }
            return replaced;
        }

        public TValue Replace(TKey key, TValue value)
        {
            var newValue = new ExpiringObject(value, Convenient.CurrentTimeMillis(), _timeToLiveSeconds);
            var segment = Segment(key);
            ExpiringObject oldValue;
            lock (segment)
            {
                oldValue = segment.Get(key);
                if (oldValue != null && !oldValue.IsExpired)
                {
                    segment.Add(key, newValue);
                }
            }
            if (oldValue == null)
            {
                return null;
            }
            return oldValue.Value;
        }

        /// <summary>
        /// Expires a key in a segment. If a key value pair is expired, it will get removed.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>True, if expired. False, otherwise.</returns>
        private bool Expire(CacheMap<TKey, ExpiringObject> segment, TKey key, ExpiringObject value)
        {
            if (value.IsExpired)
            {
                lock (segment)
                {
                    var tmp = segment.Get(key);
                    if (tmp != null && tmp.Equals(value))
                    {
                        segment.Remove(key);
                        Logger.Debug("Removed in expire: {0}.", value.Value);
                        _removedCounter.IncrementAndGet();
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Fast expiration. Since the ExpiringObject is ordered, the for-loop can break
        /// eaely if an object is not expired.
        /// </summary>
        /// <param name="segment">The segment.</param>
        private void ExpireSegment(CacheMap<TKey, ExpiringObject> segment)
        {
            //.NET-specific: iterate over KeyValuePairs
            foreach (var kvp in segment.ToList()) // iterate over copy
            {
                var expObj = kvp.Value;
                if (expObj.IsExpired)
                {
                    segment.Remove(kvp.Key); // remove from original
                    Logger.Debug("Removed in expire segment: {0}.", expObj.Value);
                    _removedCounter.IncrementAndGet();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// The number of expired objects.
        /// </summary>
        public int ExpiredCounter
        {
            get { return _removedCounter.Get(); }
        }

        /// <summary>
        /// An object that also holds expiration information.
        /// </summary>
        private class ExpiringObject : IEquatable<ExpiringObject>
        {
            /// <summary>
            /// The wrapped value.
            /// </summary>
            public TValue Value { get; private set; }
            private readonly long _lastAccessTimeMillis;

            //.NET-specific: use current _timeToLiveSeconds from ConcurrentCacheMap field
            private readonly int _timeToLiveSeconds;

            /// <summary>
            /// Creates a new expiring object with the given time of access.
            /// </summary>
            /// <param name="value">The value that is wrapped in this instance.</param>
            /// <param name="lastAccessTimeMillis">The time of access in milliseconds.</param>
            /// <param name="timeToLiveSeconds">.NET-specific: use current _timeToLiveSeconds from ConcurrentCacheMap field.</param>
            public ExpiringObject(TValue value, long lastAccessTimeMillis, int timeToLiveSeconds)
            {
                if (value == null)
                {
                    throw new ArgumentException("An expiring object cannot be null.");
                }
                Value = value;
                _lastAccessTimeMillis = lastAccessTimeMillis;
                _timeToLiveSeconds = timeToLiveSeconds;
            }

            /// <summary>
            /// Indicates whether the entry is expired.
            /// </summary>
            /// <returns></returns>
            public bool IsExpired
            {
                get
                {
                    // TODO correct?
                    return Convenient.CurrentTimeMillis() >=
                           _lastAccessTimeMillis + TimeSpan.FromSeconds(_timeToLiveSeconds).Milliseconds;
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                return Equals(obj as ExpiringObject);
            }

            public bool Equals(ExpiringObject other)
            {
                return Value.Equals(other.Value);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }
    }
}
