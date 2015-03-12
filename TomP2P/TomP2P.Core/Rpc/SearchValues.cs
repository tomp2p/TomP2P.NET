using TomP2P.Core.Peers;

namespace TomP2P.Core.Rpc
{
    /// <summary>
    /// The search values for fast get. You can either provide one content key.
    /// If you want to check for multiple keys, use the content key bloom filter. 
    /// You can also check for values with a bloom filter.
    /// </summary>
    public class SearchValues // TODO in Java, this is static
    {
        /// <summary>
        /// The bloom filter for multiple content keys. May contain false positives.
        /// </summary>
        public SimpleBloomFilter<Number160> KeyBloomFilter { get; private set; }
        /// <summary>
        /// The bloom filter for multiple content values. May contain false positives.
        /// </summary>
        public SimpleBloomFilter<Number160> ContentBloomFilter { get; private set; }

        public Number160 LocationKey { get; private set; }
        public Number160 DomainKey { get; private set; }
        public Number160 ContentKey { get; private set; }

        public Number640 From { get; private set; }
        public Number640 To { get; private set; }

        /// <summary>
        /// Searches for all content keys.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        public SearchValues(Number160 locationKey, Number160 domainKey)
        {
            LocationKey = locationKey;
            DomainKey = domainKey;
            ContentKey = null;
            KeyBloomFilter = null;
            ContentBloomFilter = null;
            From = null;
            To = null;
        }

        /// <summary>
        /// Searches for one content key.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        /// <param name="contentKey">For Get() and Remove(), one can provide a content key
        /// and the remote peer indicates if this key is on that peer.</param>
        public SearchValues(Number160 locationKey, Number160 domainKey, Number160 contentKey)
        {
            LocationKey = locationKey;
            DomainKey = domainKey;
            ContentKey = contentKey;
            KeyBloomFilter = null;
            ContentBloomFilter = null;
            From = null;
            To = null;
        }

        public SearchValues(Number160 locationKey, Number160 domainKey, Number640 from, Number640 to)
        {
            LocationKey = locationKey;
            DomainKey = domainKey;
            ContentKey = null;
            KeyBloomFilter = null;
            ContentBloomFilter = null;
            From = from;
            To = to;
        }

        /// <summary>
        /// Searches for multiple content keys. There may be false positives.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        /// <param name="keyBloomFilter">For Get() and Remove() one can provide a bloom filter of
        /// content keys and the remote peer indicates if those keys are on that peer.</param>
        public SearchValues(Number160 locationKey, Number160 domainKey, SimpleBloomFilter<Number160> keyBloomFilter)
        {
            LocationKey = locationKey;
            DomainKey = domainKey;
            ContentKey = null;
            KeyBloomFilter = keyBloomFilter;
            ContentBloomFilter = null;
            From = null;
            To = null;
        }

        /// <summary>
        /// Searhces for content key and values with a bloom filter. There may be false positives.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        /// <param name="keyBloomFilter">For Get() and Remove() one can provide a bloom filter of
        /// content keys and the remote peer indicates if those keys are on that peer.</param>
        /// <param name="contentBloomFilter">For Get() and Remove() one can provide a bloom filter of
        /// content values and the remote peer indicates if those keys are on that peer.</param>
        public SearchValues(Number160 locationKey, Number160 domainKey, SimpleBloomFilter<Number160> keyBloomFilter,
            SimpleBloomFilter<Number160> contentBloomFilter)
        {
            LocationKey = locationKey;
            DomainKey = domainKey;
            ContentKey = null;
            KeyBloomFilter = keyBloomFilter;
            ContentBloomFilter = contentBloomFilter;
            From = null;
            To = null;
        }
    }
}
