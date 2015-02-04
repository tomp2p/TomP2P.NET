using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// Creates a peer and listens to incoming connections. The result of creating this class
    /// is the connection bean and the peer bean. While the connection bean holds information
    /// that can be shared, the peer bean holds information that is unique for each peer.
    /// </summary>
    public class PeerCreator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The bean that holds information that may be shared among peers.
        /// </summary>
        public ConnectionBean ConnectionBean { get; private set; }
        /// <summary>
        /// The bean that holds information that is unique for all peers.
        /// </summary>
        public PeerBean PeerBean { get; private set; }

        private readonly IList<PeerCreator> _childConnections = new List<PeerCreator>();

        // TODO the 2 EventLoopGroups from Netty needed?

        private readonly bool _master;

        private readonly TaskCompletionSource<object> _tcsServerDone = new TaskCompletionSource<object>(); 

        /// <summary>
        /// Creates a master peer and starts UDP and TCP channels.
        /// </summary>
        /// <param name="p2pId">The ID of the network.</param>
        /// <param name="peerId">The ID of this peer.</param>
        /// <param name="keyPair">The key pair or null.</param>
        /// <param name="channelServerConfiguration">The server configuration to create the 
        /// channel server that is used for listening for incoming connections.</param>
        /// <param name="channelClientConfiguration">The client-side configuration.</param>
        /// <param name="timer"></param>
        public PeerCreator(int p2pId, Number160 peerId, KeyPair keyPair,
            ChannelServerConfiguration channelServerConfiguration,
            ChannelClientConfiguration channelClientConfiguration,
            Timer timer)
        {
            // peer bean
            PeerBean = new PeerBean(keyPair);
            PeerAddress self = FindPeerAddress(peerId, channelClientConfiguration, channelServerConfiguration);
            PeerBean.SetServerPeerAddress(self);
            Logger.Info("Visible address to other peers: {0}.", self);

            // start server
            // TODO find EventLoogGroup equivalents
            var dispatcher = new Dispatcher(p2pId, PeerBean, channelServerConfiguration.HearBeatMillis);
            var channelServer = new ChannelServer(channelServerConfiguration, dispatcher, PeerBean.PeerStatusListeners);
            if (!channelServer.Startup())
            {
                ShutdownNetty();
                throw new IOException("Cannot bind to TCP or UDP port.");
            }

            // connection bean
            var sender = new Sender(peerId, PeerBean.PeerStatusListeners, channelClientConfiguration, dispatcher);
            var reservation = new Reservation(channelClientConfiguration);
            ConnectionBean = new ConnectionBean(p2pId, dispatcher, sender, channelServer, reservation, channelClientConfiguration, timer);
            _master = true;
        }

        /// <summary>
        /// Creates a slave peer that will attach itself to a master peer.
        /// </summary>
        /// <param name="parent">The parent peer.</param>
        /// <param name="peerId">The ID of this peer.</param>
        /// <param name="keyPair">The key pair or null.</param>
        public PeerCreator(PeerCreator parent, Number160 peerId, KeyPair keyPair)
        {
            parent._childConnections.Add(this);
            // TODO overtake worker groups
            ConnectionBean = parent.ConnectionBean;
            PeerBean = new PeerBean(keyPair);
            PeerAddress self = parent.PeerBean.ServerPeerAddress.ChangePeerId(peerId);
            PeerBean.SetServerPeerAddress(self);
            _master = false;
        }

        public Task ShutdownAsync()
        {
            if (_master)
            {
                Logger.Debug("Shutting down...");
            }
            // unregister in dispatcher
            ConnectionBean.Dispatcher.RemoveIOHandlers(PeerBean.ServerPeerAddress.PeerId,
                PeerBean.ServerPeerAddress.PeerId);

            // shutdown running tasks for this peer
            if (PeerBean.MaintenanceTask != null)
            {
                PeerBean.MaintenanceTask.Shutdown();
            }

            // shutdown all children
            if (!_master)
            {
                foreach (var peerCreator in _childConnections)
                {
                    peerCreator.ShutdownAsync();
                }
                _tcsServerDone.SetResult(null); // complete task
                return _tcsServerDone.Task;
            }

            // shutdown the timer
            ConnectionBean.Timer.Stop(); // TODO sufficient?

            Logger.Debug("Shutting down client...");
            ConnectionBean.Reservation.ShutdownAsync().ContinueWith(delegate
            {
                ConnectionBean.ChannelServer.ShutdownAsync().ContinueWith(delegate
                {
                    ShutdownNetty();
                });
            });

            // this is blocking
            return _tcsServerDone.Task;
        }

        /// <summary>
        /// Creates the peer address based on the network discovery that was done./>
        /// </summary>
        /// <param name="peerId">The ID of this peer.</param>
        /// <param name="channelClientConfiguration"></param>
        /// <param name="channelServerConfiguration"></param>
        /// <returns>The peer address of this peer.</returns>
        private static PeerAddress FindPeerAddress(Number160 peerId,
            ChannelClientConfiguration channelClientConfiguration, ChannelServerConfiguration channelServerConfiguration)
        {
            string status = DiscoverNetworks.DiscoverInterfaces(channelClientConfiguration.BindingsOutgoing);
            Logger.Info("Status of external address search: " + status);

            IPAddress outsideAddress = channelClientConfiguration.BindingsOutgoing.FoundAddress;
            if (outsideAddress == null)
            {
                throw new IOException("Not listening to anything. Maybe the binding information is wrong.");
            }

            var peerSocketAddress = new PeerSocketAddress(outsideAddress,
                channelServerConfiguration.Ports.TcpPort,
                channelServerConfiguration.Ports.UdpPort);

            var self = new PeerAddress(peerId, peerSocketAddress,
                channelServerConfiguration.IsBehindFirewall,
                channelServerConfiguration.IsBehindFirewall,
                false, PeerAddress.EmptyPeerSocketAddresses);

            return self;
        }

        private void ShutdownNetty()
        {
            // in .NET, there is nothing like Netty, so this method is only for
            // portability reasons (code-flow)

            Logger.Debug("Client shut down.");
            _tcsServerDone.SetResult(null); // complete task
        }
    }
}
