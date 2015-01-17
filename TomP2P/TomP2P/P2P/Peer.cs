using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Connection.NET_Helper;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P
{
    // TODO finish implementation of Peer class

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
        private SynchronizedCollection<IShutdown> _shutdownListeners = new SynchronizedCollection<IShutdown>();

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

        public Peer SetDistributedRouting(DistributedRouting distributedRouting)
        {
            _distributedRouting = distributedRouting;
            return this;
        }

        public PeerBean PeerBean
        {
            get { return PeerCreator.PeerBean; }
        }

        public ConnectionBean ConnectionBean
        {
            get { return PeerCreator.ConnectionBean; }
        }

        public PeerAddress PeerAddress
        {
            get { return PeerBean.ServerPeerAddress; }
        }

        public PeerAddress NotifyAutomaticFutures()
        {
            // TODO find .NET equivalent
            throw new NotImplementedException();
        }

        // Basic P2P operations
        // TODO implement basic P2P operations in Peer

        /// <summary>
        /// Shuts down everything.
        /// </summary>
        /// <returns>The task for when the shutdown is completed.</returns>
        public Task ShutdownAsync()
        {
            // prevent shutdown from being called twice
            if (!_shutdown)
            {
                _shutdown = true;

                // TODO lock not needed with .NET BlockingCollection class?
                IList<IShutdown> copy = _shutdownListeners.ToList();
                IList<Task> tasks = new List<Task>(_shutdownListeners.Count + 1);
                foreach (var shutdown in copy)
                {
                    tasks.Add(shutdown.ShutdownAsync());
                    RemoveShutdownListener(shutdown);
                }
                tasks.Add(PeerCreator.ShutdownAsync());
                return Task.WhenAll(tasks);
            }
            else
            {
                // TODO correct?
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new TaskFailedException("Already shutting / shut down."));
                return tcs.Task;
            }
        }

        /// <summary>
        /// True, if the peer is about to be shut down or has done so already.
        /// </summary>
        public bool IsShutdown
        {
            get { return _shutdown; }
        }

        public Peer RemoveShutdownListener(IShutdown shutdown)
        {
            _shutdownListeners.Remove(shutdown);
            return this;
        }
    }
}
