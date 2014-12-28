using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty.Transport;
using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// The class that sends out messages.
    /// </summary>
    public class Sender
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IPeerStatusListener> _peerStatusListeners;
        public ChannelClientConfiguration ChannelClientConfiguration { get; private set; }
        private readonly Dispatcher _dispatcher;
        private readonly InteropRandom _random;

        // this map caches all messages which are meant to be sent by a reverse connection setup
        private readonly ConcurrentDictionary<int, FutureResponse> _cachedRequests = new ConcurrentDictionary<int, FutureResponse>();

        public IPingBuilderFactory PingBuilderFactory { get; private set; }

        public Sender(Number160 peerId, IList<IPeerStatusListener> peerStatusListeners,
            ChannelClientConfiguration channelClientConfiguration, Dispatcher dispatcher)
        {
            _peerStatusListeners = peerStatusListeners;
            ChannelClientConfiguration = channelClientConfiguration;
            _dispatcher = dispatcher;
            _random = new InteropRandom((ulong) peerId.GetHashCode()); // TODO check if same results in Java
        }


        
    }
}
