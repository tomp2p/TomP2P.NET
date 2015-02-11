using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    /// <summary>
    /// Boostraps to a known peer. First, channels are reserved, then Discover(PeerAddress) is called to verify this Internet
    /// connection settings using the "peerAddress" argument . Then the routing is initiated to the peers specified in
    /// "bootstrapTo". Please be aware that in order to boostrap, you need to know the peer ID of all peers in the "bootstrapTo" collection. 
    /// Passing Number160.ZERO does *not* work.
    /// </summary>
    public class BootstrapBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // TODO do these need to be of type "FutureWrapper"?
        private static readonly Task<ICollection<PeerAddress>> TaskBootstrapShutdown;
        private static readonly Task<ICollection<PeerAddress>> TaskBootstrapNoAddress;

        private readonly Peer _peer;
        public ICollection<PeerAddress> BootstrapTo { get; private set; }
        public PeerAddress PeerAddress { get; private set; }
        public IPAddress InetAddress { get; private set; }
        public int PortUdp { get; private set; }
        public int PortTcp { get; private set; }
        public RoutingConfiguration RoutingConfiguration { get; private set; }
        public bool IsForceRoutingOnlyToSelf { get; private set; }
        public bool IsBroadcast { get; private set; }

        // static constructor
        static BootstrapBuilder()
        {
            var tcsBootstrapShutdown = new TaskCompletionSource<ICollection<PeerAddress>>();
            tcsBootstrapShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            TaskBootstrapShutdown = tcsBootstrapShutdown.Task;

            var tcsBootstrapNoAddress = new TaskCompletionSource<ICollection<PeerAddress>>();
            tcsBootstrapNoAddress.SetException(new TaskFailedException("No addresses to bootstrap to have been provided. Or maybe, the provided address has peer ID set to zero."));
            TaskBootstrapNoAddress = tcsBootstrapNoAddress.Task;
        }

        public BootstrapBuilder(Peer peer)
        {
            _peer = peer;

            PortUdp = Ports.DefaultPort;
            PortTcp = Ports.DefaultPort;
        }

        public BootstrapBuilder SetBootstrapTo(ICollection<PeerAddress> bootstrapTo)
        {
            BootstrapTo = bootstrapTo;
            return this;
        }

        /// <summary>
        /// Sets the peer address to bootstrap to.
        /// Please note that the peer address needs to know the peer ID of the bootstrap peer.
        /// If this is not known, use SetInetAddress(IPEndPoint) instead.
        /// </summary>
        /// <param name="peerAddress">The full address of the peer to bootstrap to (including the peer ID of the bootstrap peer).</param>
        /// <returns></returns>
        public BootstrapBuilder SetPeerAddress(PeerAddress peerAddress)
        {
            if (peerAddress != null && peerAddress.PeerId.Equals(Number160.Zero))
            {
                Logger.Warn("Peer address with peer ID zero provided. Bootstrapping is impossible, because no peer with peer ID set to zero is allowed to exist.");
                return this;
            }
            PeerAddress = peerAddress;
            return this;
        }

        public BootstrapBuilder SetInetAddress(IPAddress inetAddress)
        {
            InetAddress = inetAddress;
            return this;
        }

        public BootstrapBuilder SetInetSocketAddress(IPEndPoint socket)
        {
            InetAddress = socket.Address;
            PortTcp = socket.Port;
            PortUdp = socket.Port;
            return this;
        }

        public BootstrapBuilder SetPortUdp(int portUdp)
        {
            PortUdp = portUdp;
            return this;
        }

        public BootstrapBuilder SetPortTcp(int portTcp)
        {
            PortTcp = portTcp;
            return this;
        }

        public BootstrapBuilder SetPorts(int port)
        {
            PortTcp = port;
            PortUdp = port;
            return this;
        }

        public BootstrapBuilder SetRoutingConfiguration(RoutingConfiguration routingConfiguration)
        {
            RoutingConfiguration = routingConfiguration;
            return this;
        }

        public BootstrapBuilder SetIsForceRoutingOnlyToSelf()
        {
            return SetIsForceRoutingOnlyToSelf(true);
        }

        public BootstrapBuilder SetIsForceRoutingOnlyToSelf(bool forceRoutingOnlyToSelf)
        {
            IsForceRoutingOnlyToSelf = forceRoutingOnlyToSelf;
            return this;
        }

        public BootstrapBuilder SetIsBroadcast()
        {
            return SetIsBroadcast(true);
        }

        public BootstrapBuilder SetIsBroadcast(bool broadcast)
        {
            IsBroadcast = broadcast;
            return this;
        }

        public Task<ICollection<PeerAddress>> Start()
        {
            if (_peer.IsShutdown)
            {
                return TaskBootstrapShutdown;
            }

            if (RoutingConfiguration == null)
            {
                RoutingConfiguration = new RoutingConfiguration(8, 10, 2);
            }

            if (IsBroadcast)
            {
                return Broadcast0();
            }
            if (PeerAddress == null && InetAddress != null && BootstrapTo == null)
            {
                PeerAddress = new PeerAddress(Number160.Zero, InetAddress, PortTcp, PortUdp);
                return BootstrapPing(PeerAddress);
            }
            else if (PeerAddress != null && BootstrapTo == null)
            {
                BootstrapTo = new List<PeerAddress>(1) {PeerAddress};
                return Bootstrap();
            }
            else if (BootstrapTo != null)
            {
                return Bootstrap();
            }
            else
            {
                return TaskBootstrapNoAddress;
            }
        }

        private Task<ICollection<PeerAddress>> Bootstrap()
        {
            var result = new TaskCompletionSource<Task<Pair<FutureRouting, FutureRouting>>>();
        }
    }
}
