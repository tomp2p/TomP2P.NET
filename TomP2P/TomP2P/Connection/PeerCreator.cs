using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private readonly ConnectionBean _connectionBean;
        private readonly PeerBean _peerBean;

        private readonly IList<PeerCreator> _childConnections = new List<PeerCreator>();

        // TODO the 2 EventLoopGroups from Netty needed?

        private readonly bool _master;

        // TODO find FutureDone equivalent

        // TODO find ScheduledExecutorService timer equivalent

        /// <summary>
        /// Creates a master peer and starts UDP and TCP channels.
        /// </summary>
        /// <param name="p2pId">The ID of the network.</param>
        /// <param name="peerId">The ID of this peer.</param>
        /// <param name="keyPair">The key pair or null.</param>
        /// <param name="channelServerConfiguration">The server configuration to create the 
        /// channel server that is used for listening for incoming connections.</param>
        /// <param name="channelClientConfiguration">The client-side configuration.</param>
        public PeerCreator(int p2pId, Number160 peerId, KeyPair keyPair,
            ChannelServerConfiguration channelServerConfiguration,
            ChannelClientConfiguration channelClientConfiguration)
        {
            // peer bean
            _peerBean = new PeerBean(keyPair);
            PeerAddress self = FindPeerAddress(peerId, channelClientConfiguration, channelServerConfiguration);
            _peerBean.SetServerPeerAddress(self);
            Logger.Info("Visible address to other peers: {0}.", self);

            // start server
            // TODO find EventLoogGroup equivalents

            var dispatcher = new Dispatcher(p2pId, _peerBean, channelServerConfiguration.HearBeatMillis);
            var channelServer = new ChannelServer(channelServerConfiguration, dispatcher, _peerBean.PeerStatusListeners);
            if (!channelServer.Startup())
            {
                // TODO shutdown "Netty"
                throw new IOException("Cannot bind to TCP or UDP port.");
            }

            // connection bean
            var sender = new Sender(peerId, _peerBean.PeerStatusListeners, channelClientConfiguration, dispatcher);
            Reservation reservation = new Reservation(channelClientConfiguration);
            _connectionBean = new ConnectionBean(p2pId, dispatcher, sender, channelServer, reservation, channelClientConfiguration); // TODO provide .NET timer
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
            _connectionBean = parent._connectionBean;
            _peerBean = new PeerBean(keyPair);
            PeerAddress self = parent._peerBean.ServerPeerAddress.ChangePeerId(peerId);
            _peerBean.SetServerPeerAddress(self);
            _master = false;
        }
    }
}
