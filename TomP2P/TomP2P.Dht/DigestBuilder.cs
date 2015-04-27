using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public class DigestBuilder : DhtBuilder<DigestBuilder>, ISearchableBuilder
    {
        private static readonly TcsDigest TcsDigestShutdown = new TcsDigest(null);
        private static readonly ICollection<Number160> NumberZeroContentKeys = new List<Number160>(1);

        public ICollection<Number160> ContentKeys { get; private set; }
        public ICollection<Number640> Keys { get; private set; }
        public Number160 ContentKey { get; private set; }

        public SimpleBloomFilter<Number160> KeyBloomFilter { get; private set; }
        public SimpleBloomFilter<Number160> ContentBloomFilter { get; private set; }

        public IEvaluationSchemeDht EvaluationScheme { get; private set; }
        public Number640 From { get; private set; }
        public Number640 To { get; private set; }

        public bool IsAll { get; private set; }
        public bool IsReturnBloomFilter { get; private set; }
        public bool IsReturnAllBloomFilter { get; private set; }
        public bool IsAscending { get; private set; }
        public bool IsBloomFilterAnd { get; private set; }
        public bool IsReturnMetaValues { get; private set; }
        public bool IsFastGet { get; private set; }

        private int _returnNr = -1;

        // static constructor
        static DigestBuilder()
        {
            TcsDigestShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            NumberZeroContentKeys.Add(Number160.Zero);
        }

        public DigestBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            IsAscending = true;
            IsBloomFilterAnd = true;
            IsFastGet = true;
            SetSelf(this);
        }

        public TcsDigest Start()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the content keys that should be found. Please note that if the content keys are too large, you may need to
        /// switch to TCP during routing. The default routing is UDP. Currently, the header is 59 bytes, and the length of the
        /// content keys is as follows: 4 bytes for the length, 20 bytes per content key. The user is warned if it will exceed
        /// the UDP size of 1400 bytes.
        /// </summary>
        /// <param name="contentKeys"></param>
        /// <returns></returns>
        public DigestBuilder SetContentKeys(ICollection<Number160> contentKeys)
        {
            ContentKeys = contentKeys;
            return this;
        }

        public DigestBuilder SetKeys(ICollection<Number640> keys)
        {
            Keys = keys;
            return this;
        }

        public DigestBuilder SetContentKey(Number160 contentKey)
        {
            ContentKey = contentKey;
            return this;
        }

        public DigestBuilder SetKeyBloomFilter(SimpleBloomFilter<Number160> keyBloomFilter)
        {
            KeyBloomFilter = keyBloomFilter;
            return this;
        }

        public DigestBuilder SetContentBloomFilter(SimpleBloomFilter<Number160> contentBloomFilter)
        {
            ContentBloomFilter = contentBloomFilter;
            return this;
        }

        public DigestBuilder SetEvaluationScheme(IEvaluationSchemeDht evaluationScheme)
        {
            EvaluationScheme = evaluationScheme;
            return this;
        }

        public DigestBuilder SetFrom(Number640 from)
        {
            From = from;
            return this;
        }

        public DigestBuilder SetTo(Number640 to)
        {
            To = to;
            return this;
        }

        public DigestBuilder SetIsAll()
        {
            return SetIsAll(true);   
        }

        public DigestBuilder SetIsAll(bool isAll)
        {
            IsAll = isAll;
            return this;
        }

        public DigestBuilder SetIsReturnBloomFilter()
        {
            return SetIsReturnBloomFilter(true);
        }

        public DigestBuilder SetIsReturnBloomFilter(bool isReturnBloomFilter)
        {
            IsReturnBloomFilter = isReturnBloomFilter;
            return this;
        }

        public DigestBuilder SetIsReturnAllBloomFilter()
        {
            return SetIsReturnAllBloomFilter(true);
        }

        public DigestBuilder SetIsReturnAllBloomFilter(bool isReturnAllBloomFilter)
        {
            IsReturnAllBloomFilter = isReturnAllBloomFilter;
            return this;
        }

        public DigestBuilder SetIsAscending()
        {
            return SetIsAscending(true);
        }

        public DigestBuilder SetIsDescending()
        {
            return SetIsAscending(false);
        }

        public DigestBuilder SetIsAscending(bool isAscending)
        {
            IsAscending = isAscending;
            return this;
        }

        public bool IsDescending
        {
            get { return !IsAscending; }
        }

        public DigestBuilder SetIsBloomFilterAnd()
        {
            return SetIsBloomFilterAnd(true);
        }

        public DigestBuilder SetIsBloomFilterAnd(bool isBloomFilterAnd)
        {
            IsBloomFilterAnd = isBloomFilterAnd;
            return this;
        }
    }
}
