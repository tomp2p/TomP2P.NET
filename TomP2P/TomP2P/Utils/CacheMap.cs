using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Utils
{
    public class CacheMap<TKey, TValue> : SortedDictionary<TKey, TValue>
    {
        private readonly int _maxEntries;
        private readonly bool _updateEntryOnInsert;
    }
}
