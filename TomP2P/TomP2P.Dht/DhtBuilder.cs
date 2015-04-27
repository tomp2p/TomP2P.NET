using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.Futures;
using TomP2P.Core.P2P;
using TomP2P.Core.P2P.Builder;
using TomP2P.Core.Peers;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    /// <summary>
    /// Every DHT builder has those methods in common.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DhtBuilder<T> : DefaultConnectionConfiguration, IBasicBuilder<T>, ISignatureBuilder<T> where T : DhtBuilder<T>
    {
        protected readonly PeerDht PeerDht;
        public Number160 LocationKey { get; protected set; }
        public Number160 DomainKey { get; protected set; }
        public Number160 VersionKey { get; protected set; }

        /// <summary>
        /// The configuration for the routing options.
        /// </summary>
        public RoutingConfiguration RoutingConfiguration { get; protected set; }

        /// <summary>
        /// The configuration for the P2P request options.
        /// </summary>
        public RequestP2PConfiguration RequestP2PConfiguration { get; protected set; }
        /// <summary>
        /// The task of the created channel.
        /// </summary>
        public Task<ChannelCreator> TaskChannelCreator { get; protected set; }

        /// <summary>
        /// Set to true if the domain should be set to protected. This means that 
        /// this domain is flagged and a public key is stored for this entry. 
        /// An update or removal can only be made with the matching private key.
        /// </summary>
        public bool IsProtectDomain { get; private set; }
        public KeyPair KeyPair { get; private set; }
        /// <summary>
        /// True, if streaming should be used.
        /// </summary>
        public bool IsStreaming { get; private set; }

        public ICollection<IPeerMapFilter> PeerMapFilters { get; private set; }
        public ICollection<IPostRoutingFilter> PostRoutingFilters { get; private set; }

        public T Self { get; private set; }

        public DhtBuilder(PeerDht peerDht, Number160 locationKey)
        {
            PeerDht = peerDht;
            LocationKey = locationKey;
        }

        public void SetSelf(T self)
        {
            Self = self;
        }

        public T SetDomainKey(Number160 domainKey)
        {
            DomainKey = domainKey;
            return Self;
        }

        public T SetVersionKey(Number160 versionKey)
        {
            VersionKey = versionKey;
            return Self;
        }

        public T SetRoutingConfiguration(RoutingConfiguration routingConfiguration)
        {
            RoutingConfiguration = routingConfiguration;
            return Self;
        }

        public T SetRequestP2PConfiguration(RequestP2PConfiguration requestP2PConfiguration)
        {
            RequestP2PConfiguration = requestP2PConfiguration;
            return Self;
        }

        public T SetTaskChannelCreator(Task<ChannelCreator> taskChannelCreator)
        {
            TaskChannelCreator = taskChannelCreator;
            return Self;
        }

        /// <summary>
        /// Set to true if the domain should be set to protected. This means that 
        /// this domain is flagged and a public key is stored for this entry. 
        /// An update or removal can only be made with the matching private key.
        /// </summary>
        /// <returns></returns>
        public T SetIsProtectDomain()
        {
            SetIsProtectDomain(true);
            if (KeyPair == null)
            {
                SetSign();
            }
            return Self;
        }

        /// <summary>
        /// Set to true if the domain should be set to protected. This means that 
        /// this domain is flagged and a public key is stored for this entry. 
        /// An update or removal can only be made with the matching private key.
        /// </summary>
        /// <param name="isProtectDomain"></param>
        /// <returns></returns>
        public T SetIsProtectDomain(bool isProtectDomain)
        {
            IsProtectDomain = isProtectDomain;
            return Self;
        }

        public bool IsSign
        {
            get { return KeyPair != null; }
        }

        public T SetSign()
        {
            KeyPair = PeerDht.Peer.PeerBean.KeyPair;
            return Self;
        }

        public T SetSign(bool signMessage)
        {
            if (signMessage)
            {
                SetSign();
            }
            else
            {
                KeyPair = null;
            }
            return Self;
        }

        public T SetKeyPair(KeyPair keyPair)
        {
            KeyPair = keyPair;
            return Self;
        }

        /// <summary>
        /// Sets streaming to true.
        /// </summary>
        /// <returns></returns>
        public T SetStreaming()
        {
            SetStreaming(true);
            return Self;
        }

        /// <summary>
        /// Set streaming. If streaming is set to true, then the data can be added
        /// after Start() has been called.
        /// </summary>
        /// <param name="isStreaming">True, if streaming should be used.</param>
        /// <returns></returns>
        public T SetStreaming(bool isStreaming)
        {
            IsStreaming = isStreaming;
            return Self;
        }

        public T AddPeerMapFilter(IPeerMapFilter peerMapFilter)
        {
            if (PeerMapFilters == null)
            {
                // most likely we have 1-2 filters
                PeerMapFilters = new List<IPeerMapFilter>(2);
            }
            PeerMapFilters.Add(peerMapFilter);
            return Self;
        }

        public T AddPostRoutingFilter(IPostRoutingFilter postRoutingFilter)
        {
            if (PostRoutingFilters == null)
            {
                // most likely we have 1-2 filters
                PostRoutingFilters = new List<IPostRoutingFilter>(2);
            }
            PostRoutingFilters.Add(postRoutingFilter);
            return Self;
        }

        protected void PreBuild()
        {
            if (DomainKey == null)
            {
                DomainKey = Number160.Zero;
            }
            if (VersionKey == null)
            {
                VersionKey = Number160.Zero;
            }
            if (RoutingConfiguration == null)
            {
                RoutingConfiguration = new RoutingConfiguration(5, 10, 2);
            }
            if (RequestP2PConfiguration == null)
            {
                RequestP2PConfiguration = new RequestP2PConfiguration(3, 5, 5);
            }
            var size = PeerDht.Peer.PeerBean.PeerMap.Size + 1;
            RequestP2PConfiguration = RequestP2PConfiguration.AdjustMinimumResult(size);
            if (TaskChannelCreator == null
                || (TaskChannelCreator.Result != null && TaskChannelCreator.Result.IsShutdown))
            {
                TaskChannelCreator = PeerDht.Peer.ConnectionBean.Reservation.CreateAsync(RoutingConfiguration,
                    RequestP2PConfiguration, this);
            }
        }

        public RoutingBuilder CreateBuilder(RequestP2PConfiguration requestP2PConfiguration,
            RoutingConfiguration routingConfiguration)
        {
            var routingBuilder = new RoutingBuilder();
            routingBuilder.Parallel = routingConfiguration.Parallel;
            routingBuilder.MaxNoNewInfo = routingConfiguration.MaxNoNewInfo(requestP2PConfiguration.MinimumResults);
            routingBuilder.MaxDirectHits = routingConfiguration.MaxDirectHits;
            routingBuilder.MaxFailures = routingConfiguration.MaxFailures;
            routingBuilder.MaxSuccess = routingConfiguration.MaxSuccess;
            return routingBuilder;
        }
    }
}
