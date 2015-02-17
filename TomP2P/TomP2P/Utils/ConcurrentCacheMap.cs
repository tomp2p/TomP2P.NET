using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

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

    }
}
