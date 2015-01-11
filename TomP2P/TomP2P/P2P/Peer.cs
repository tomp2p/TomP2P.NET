using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P
{
    // TODO document this class according to the newest version
    /// <summary>
    /// This is the main class to start DHT operations.
    /// </summary>
    public class Peer
    {
        // as soon as the user calls listen, this connection handler is set
        public PeerCreator PeerCreator { get; private set; }
        /// <summary>
        /// The ID of this peer.
        /// </summary>
        public Number160 PeerId { get; private set; }
        /// <summary>
        /// The P2P network identifier, two different networks can use the same ports.
        /// </summary>
        public int P2PId { get; private set; }

        // distributed
        private DistributedRouting _distributedRouting;

        // RPC
        private PingRpc _pingRpc;
        // TODO add other Rpc's

        private volatile bool _shutdown = false;

        // TODO add the two lists

        /// <summary>
        /// Creates a peer. Please use <see cref="PeerBuilder"/> to create a <see cref="Peer"/> instance.
        /// </summary>
        /// <param name="p2pId">The P2P ID.</param>
        /// <param name="peerId">The ID of the peer.</param>
        /// <param name="peerCreator">The peer creator that holds the peer bean and the connection bean.</param>
        internal Peer(int p2pId, Number160 peerId, PeerCreator peerCreator)
        {
            P2PId = p2pId;
            PeerId = peerId;
            PeerCreator = peerCreator;
        }

        public PingRpc PingRpc
        {
            get
            {
                if (_pingRpc == null)
                {
                    throw new SystemException("Ping RPC not enabled. Please enable this RPC in the PeerBuilder.");
                }
                return _pingRpc;
            }
        }

        public Peer SetPingRpc(PingRpc pingRpc)
        {
            _pingRpc = pingRpc;
            return this;
        }

        public DistributedRouting DistributedRouting
        {
            get
            {
                if (_distributedRouting == null)
                {
                    throw new SystemException("DistributedRouting not enabled. Please enable this P2P function in the PeerBuilder.");
                }
                return _distributedRouting;
            }
        }

        public void SetDistributedRouting(DistributedRouting distributedRouting)
        {
            _distributedRouting = distributedRouting;
        }

        public PeerBean PeerBean
        {
            get { return PeerCreator.PeerBean; }
        }

        public ConnectionBean ConnectionBean
        {
            get { return PeerCreator.ConnectionBean; }
        }
    }
}
