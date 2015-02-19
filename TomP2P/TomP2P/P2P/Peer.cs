using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P
{
    // TODO finish implementation of Peer class
    // - enable RPCs
    // - finish basic operations

    /// <summary>
    /// This is the main class to start DHT operations. This class makes use of the build pattern
    /// and for each DHT operation, a builder class is returned. The main operations can be initiated
    /// with Put(), Get(), Add(), AddTracker(), GetTracker(), Remove(), Submit(), Send(), SendDirect(),
    /// Broadcast(). Each of those operations returns a builder that offers more options.
    /// One of the main difference to a "regular" DHT is that TomP2P can store a map (key-values) instead
    /// of just values.
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
        private QuitRpc _quitRpc;
        //private NeighborRpc _neighborRpc;
        //private DirectDataRpc _directDataRpc;
        private BroadcastRpc _broadcastRpc;

        private volatile bool _shutdown = false;

        private IList<IAutomaticTask> _automaticTasks = new SynchronizedCollection<IAutomaticTask>();
        private IList<IShutdown> _shutdownListeners = new SynchronizedCollection<IShutdown>();

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

        public QuitRpc QuitRpc
        {
            get
            {
                if (_quitRpc == null)
                {
                    throw new SystemException("Quit RPC not enabled. Please enable this RPC in the PeerBuilder.");
                }
                return _quitRpc;
            }
        }

        public Peer SetQuitRpc(QuitRpc quitRpc)
        {
            _quitRpc = quitRpc;
            return this;
        }
        /*
        public NeighborRpc NeighborRpc
        {
            get
            {
                if (_neighborRpc == null)
                {
                    throw new SystemException("Neighbor RPC not enabled. Please enable this RPC in the PeerBuilder.");
                }
                return _neighborRpc;
            }
        }
        
        public Peer SetNeighborRpc(NeighborRpc neighborRpc)
        {
            _neighborRpc = neighborRpc;
            return this;
        }

        public DirectDataRpc DirectDataRpc
        {
            get
            {
                if (_directDataRpc == null)
                {
                    throw new SystemException("Direct data RPC not enabled. Please enable this RPC in the PeerBuilder.");
                }
                return _directDataRpc;
            }
        }

        public Peer SetDirectDataRpc(DirectDataRpc directDataRpc)
        {
            _directDataRpc = directDataRpc;
            return this;
        }
        */
        public BroadcastRpc BroadcastRpc
        {
            get
            {
                if (_broadcastRpc == null)
                {
                    throw new SystemException("Broadcast RPC not enabled. Please enable this RPC in the PeerBuilder.");
                }
                return _broadcastRpc;
            }
        }

        public Peer SetBroadcastRpc(BroadcastRpc broadcastRpc)
        {
            _broadcastRpc = broadcastRpc;
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

        #region Basic P2P operations

        // TODO implement basic P2P operations in Peer

        #region Direct, Bootstrap, Ping, Broadcast



        public BootstrapBuilder Bootstrap()
        {
            return new BootstrapBuilder(this);
        }

        public PingBuilder Ping()
        {
            return new PingBuilder(this);
        }

        #endregion

        #endregion

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

        public Peer AddShutdownListener(IShutdown shutdown)
        {
            _shutdownListeners.Add(shutdown);
            return this;
        }

        public Peer RemoveShutdownListener(IShutdown shutdown)
        {
            _shutdownListeners.Remove(shutdown);
            return this;
        }

        public Peer AddAutomaticTask(IAutomaticTask automaticTask)
        {
            _automaticTasks.Add(automaticTask);
            return this;
        }

        public Peer RemoveAutomaticTask(IAutomaticTask automaticTask)
        {
            _automaticTasks.Remove(automaticTask);
            return this;
        }
    }
}
