using System;
using System.Collections.Generic;
using System.Threading;
using TomP2P.Extensions;

namespace TomP2P.Dht
{
    public sealed class RangeLock<T> where T : IComparable<T>
    {
        private readonly object _lock = new object();
        private readonly SortedDictionary<T, long> _cache = new SortedDictionary<T, long>();

        public struct Range
        {
            public T FromKey { get; }
            public T ToKey { get; }
            private readonly RangeLock<T> _reference;

            public Range(T fromKey, T toKey, RangeLock<T> reference)
            {
                FromKey = fromKey;
                ToKey = toKey;
                _reference = reference;
            }

            public void Unlock()
            {
                _reference.Unlock(this);
            }
        }

        /// <summary>
        /// The same thread can lock a range twice. The first unlock for range X unlocks all range X.
        /// </summary>
        /// <param name="fromKey"></param>
        /// <param name="toKey"></param>
        /// <returns></returns>
        public Range Lock(T fromKey, T toKey)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            lock (_lock)
            {
                var subMap = _cache.SubDictionary(fromKey, true, toKey, true);
                while (subMap.Count > 0)
                {
                    if (MapSizeFiltered(threadId, subMap) == 0)
                    {
                        break;
                    }
                    Monitor.Wait(_lock);
                }
                _cache.Add(fromKey, threadId);
                _cache.Add(toKey, threadId);
            }
            return new Range(fromKey, toKey, this);
        }

        public void Unlock(Range range)
        {
            lock (_lock)
            {
                _cache.Remove(range.FromKey);
                _cache.Remove(range.ToKey);
                Monitor.PulseAll(_lock);
            }
        }

        private static int MapSizeFiltered(long threadId, SortedDictionary<T, long> subMap)
        {
            var counter = 0;
            foreach (var id in subMap.Values)
            {
                if (id != threadId)
                {
                    counter++;
                }
            }
            return counter;
        }

        public int Size
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }
    }
}
