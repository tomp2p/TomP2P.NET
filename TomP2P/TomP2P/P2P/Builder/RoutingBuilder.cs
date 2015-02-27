using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P.Builder
{
    public class RoutingBuilder : DefaultConnectionConfiguration
    {
        public Number160 LocationKey { get; set; }
        public Number160 DomainKey { get; set; }
        public Number160 ContentKey { get; set; }

        public SimpleBloomFilter<Number160> KeyBloomFilter { get; set; }
        public SimpleBloomFilter<Number160> ContentBloomFilter { get; set; }

        public Number640 From { get; private set; }
        public Number640 To { get; private set; }

        public ICollection<IPeerFilter> PeerFilters { get; private set; }

        public int MaxDirectHits { get; set; }
        public int MaxNoNewInfo { get; set; }
        public int MaxSuccess { get; set; }
        public int MaxFailures { get; set; }
        public int Parallel { get; set; }
        public bool IsBootstrap { get; set; }
        public bool IsForeceRoutingOnlyToSelf { get; set; }
        public bool IsRoutingToOthers { get; private set; }

        public RoutingBuilder SetPeerBuilder(ICollection<IPeerFilter> peerFilters)
        {
            PeerFilters = peerFilters;
            return this;
        }

        public RoutingBuilder SetIsRoutingOnlyToSelf(bool isRoutingOnlyToSelf)
        {
            IsRoutingToOthers = !isRoutingOnlyToSelf;
            return this;
        }

        public void SetRange(Number640 from, Number640 to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// The search values for the neighbor request, or null if no content key is specified.
        /// </summary>
        /// <returns></returns>
        public SearchValues SearchValues()
        {
            if (ContentKey != null)
            {
                return new SearchValues(LocationKey, DomainKey, ContentKey);
            }
            if (From != null && To != null)
            {
                return new SearchValues(LocationKey, DomainKey, From, To);
            }
            if (ContentBloomFilter == null && KeyBloomFilter != null)
            {
                return new SearchValues(LocationKey, DomainKey, KeyBloomFilter);
            }
            if (ContentBloomFilter != null && KeyBloomFilter != null)
            {
                return new SearchValues(LocationKey, DomainKey, KeyBloomFilter, ContentBloomFilter);
            }
            return new SearchValues(LocationKey, DomainKey);
        }

        public RoutingMechanism CreateRoutingMechanism(TcsRouting tcsRouting)
        {
            var tcsResponses = new TaskCompletionSource<Message.Message>[Parallel];
            var tcsResponseArray = new VolatileReferenceArray<TaskCompletionSource<Message.Message>>(tcsResponses);
            var routingMechanism = new RoutingMechanism(tcsResponseArray, tcsRouting, PeerFilters)
            {
                MaxDirectHits = MaxDirectHits,
                MaxFailures = MaxFailures,
                MaxNoNewInfo = MaxNoNewInfo,
                MaxSucess = MaxSuccess
            };
            return routingMechanism;
        }
    }
}
