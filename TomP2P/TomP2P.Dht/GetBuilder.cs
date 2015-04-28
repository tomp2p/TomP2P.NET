using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    // TODO move builder commonalities to base class
    public class GetBuilder : DhtBuilder<GetBuilder>, ISearchableBuilder
    {
        private static readonly TcsGet TcsGetShutdown = new TcsGet(null);
        private static readonly ICollection<Number160> NumberZeroContentKeys = new List<Number160>(1);

        public ICollection<Number160> ContentKeys { get; private set; }
        public ICollection<Number640> Keys { get; private set; }
        public Number160 ContentKey { get; private set; }

        public SimpleBloomFilter<Number160> ContentKeyBloomFilter { get; private set; } 
        public SimpleBloomFilter<Number160> VersionKeyBloomFilter { get; private set; } 
        public SimpleBloomFilter<Number160> ContentBloomFilter { get; private set; }

        public IEvaluationSchemeDht EvaluationScheme { get; private set; }
        public Number640 From { get; private set; }
        public Number640 To { get; private set; }

        public bool IsAll { get; private set; }
        public bool IsReturnBloomFilter { get; private set; }
        public bool IsAscending { get; private set; }
        public bool IsBloomFilterAnd { get; private set; }
        public bool IsFastGet { get; private set; }
        public bool IsGetLatest { get; private set; }
        public bool IsWithDigest { get; private set; }

        public int ReturnNr { get; private set; }

        // static constructor
        static GetBuilder()
        {
            TcsGetShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            NumberZeroContentKeys.Add(Number160.Zero);
        }

        public GetBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            IsAscending = true;
            IsBloomFilterAnd = true;
            IsFastGet = true;
            ReturnNr = -1;
            SetSelf(this);
        }

        public TcsGet Start()
        {
            if (PeerDht.Peer.IsShutdown)
            {
                return TcsGetShutdown;
            }
            PreBuild();
            if (IsAll)
            {
                ContentKeys = null;
            }
            else if (ContentKeys == null && !IsAll)
            {
                if (ContentKey == null)
                {
                    ContentKeys = NumberZeroContentKeys;
                }
                else
                {
                    ContentKeys = new List<Number160>(1);
                    ContentKeys.Add(ContentKey);
                }
            }
            if (EvaluationScheme == null)
            {
                EvaluationScheme = new VotingSchemeDht();
            }
            if (IsGetLatest)
            {
                if (ContentKey == null)
                {
                    ContentKey = Number160.Zero;
                }
            }
            return PeerDht.Dht.Get(this);
        }

        public GetBuilder SetContentKeys(ICollection<Number160> contentKeys)
        {
            ContentKeys = contentKeys;
            return this;
        }

        public GetBuilder SetKeys(ICollection<Number640> keys)
        {
            Keys = keys;
            return this;
        }

        public GetBuilder SetContentKey(Number160 contentKey)
        {
            ContentKey = contentKey;
            return this;
        }

        public GetBuilder SetVersionKeyBloomFilter(SimpleBloomFilter<Number160> versionKeyBloomFilter)
        {
            VersionKeyBloomFilter = versionKeyBloomFilter;
            return this;
        }

        public GetBuilder SetContentKeyBloomFilter(SimpleBloomFilter<Number160> contentKeyBloomFilter)
        {
            ContentKeyBloomFilter = contentKeyBloomFilter;
            return this;
        }

        public GetBuilder SetContentBloomFilter(SimpleBloomFilter<Number160> contentBloomFilter)
        {
            ContentBloomFilter = contentBloomFilter;
            return this;
        }

        public GetBuilder SetEvaluationScheme(IEvaluationSchemeDht evaluationScheme)
        {
            EvaluationScheme = evaluationScheme;
            return this;
        }

        public GetBuilder SetFrom(Number640 from)
        {
            From = from;
            return this;
        }

        public GetBuilder SetTo(Number640 to)
        {
            To = to;
            return this;
        }
        
        public bool IsRange
        {
            get { return From != null && To != null; }
        }

        public GetBuilder SetIsAll()
        {
            return SetIsAll(true);
        }

        public GetBuilder SetIsAll(bool isAll)
        {
            IsAll = isAll;
            return this;
        }

        public GetBuilder SetIsReturnBloomFilter()
        {
            return SetIsReturnBloomFilter(true);
        }

        public GetBuilder SetIsReturnBloomFilter(bool isReturnBloomFilter)
        {
            IsReturnBloomFilter = isReturnBloomFilter;
            return this;
        }

        public GetBuilder SetIsAscending()
        {
            return SetIsAscending(true);
        }

        public GetBuilder SetIsDescending()
        {
            return SetIsAscending(false);
        }

        public GetBuilder SetIsAscending(bool isAscending)
        {
            IsAscending = isAscending;
            return this;
        }

        public bool IsDescending
        {
            get { return !IsAscending; }
        }

        public GetBuilder SetIsBloomFilterAnd()
        {
            return SetIsBloomFilterAnd(true);
        }

        public GetBuilder SetIsBloomFilterIntersect()
        {
            return SetIsBloomFilterAnd(false);
        }

        public GetBuilder SetIsBloomFilterAnd(bool isBloomFilterAnd)
        {
            IsBloomFilterAnd = isBloomFilterAnd;
            return this;
        }

        public bool IsBloomFilterIntersect
        {
            get { return !IsBloomFilterAnd; }
        }

        public GetBuilder SetIsFastGet()
        {
            return SetIsFastGet(true);
        }

        public GetBuilder SetIsFastGet(bool isFastGet)
        {
            IsFastGet = isFastGet;
            return this;
        }

        public GetBuilder SetIsGetLatest()
        {
            return SetIsGetLatest(true);
        }

        public GetBuilder SetIsGetLatest(bool isGetLatest)
        {
            IsGetLatest = isGetLatest;
            return this;
        }

        public GetBuilder SetIsWithDigest()
        {
            return SetIsWithDigest(true);
        }

        public GetBuilder SetIsWithDigest(bool isWithDigest)
        {
            IsWithDigest = isWithDigest;
            return this;
        }

        public GetBuilder SetReturnNr(int returnNr)
        {
            ReturnNr = returnNr;
            return this;
        }
    }
}
