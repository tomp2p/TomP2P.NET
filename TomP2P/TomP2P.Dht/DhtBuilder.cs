using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Core.Connection;
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
    public abstract class DhtBuilder<T> : DefaultConnectionConfiguration, IBasicBuilder<T>, IConnectionConfiguration, ISignatureBuilder<T> where T : DhtBuilder<T>
    {
        protected readonly PeerDht _peer;
        protected readonly Number160 _locationKey;
        protected Number160 _domainKey;
        protected Number160 _versionKey;

        protected RoutingConfiguration _routingConfiguration;
        protected RequestP2PConfiguration _requestP2PConfiguration;
        protected TaskCompletionSource<ChannelCreator> _tcsChannelCreator;

        private bool _protectDomain;
        private KeyPair _keyPair;
        private bool _streaming;

        private IEnumerable<IPeerMapFilter> _peerMapFilters;
        private IEnumerable<IPostRoutingFilter> _postRoutingFilters;
    }
}
