using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Extensions;
using TomP2P.Futures;
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

        private static readonly TcsWrappedBootstrap<Task> TcsBootstrapShutdown = new TcsWrappedBootstrap<Task>();
        private static readonly TcsWrappedBootstrap<Task> TcsBootstrapNoAddress = new TcsWrappedBootstrap<Task>();

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
            TcsBootstrapShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            TcsBootstrapNoAddress.SetException(new TaskFailedException("No addresses to bootstrap to have been provided. Or maybe, the provided address has peer ID set to zero."));
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

        // .NET-specific:
        // returns TcsWrappedBootstrap instead of "FutureBootstrap"
        // -> exposes "BootstrapTo" property as well
        public TcsWrappedBootstrap<Task> Start()
        {
            if (_peer.IsShutdown)
            {
                return TcsBootstrapShutdown;
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
            if (PeerAddress != null && BootstrapTo == null)
            {
                BootstrapTo = new List<PeerAddress>(1) { PeerAddress };
                return Bootstrap();
            }
            if (BootstrapTo != null)
            {
                return Bootstrap();
            }
            return TcsBootstrapNoAddress;
        }

        private TcsWrappedBootstrap<Task> Broadcast0()
        {
            var tcsBootstrap = new TcsWrappedBootstrap<Task>();

            var taskPing = _peer.Ping().SetIsBroadcast().SetPort(PortUdp).Start();
            taskPing.ContinueWith(tp =>
            {
                if (!tp.IsFaulted)
                {
                    PeerAddress = tp.Result;
                    BootstrapTo = new List<PeerAddress>(1) { PeerAddress };
                    tcsBootstrap.SetBootstrapTo(BootstrapTo);
                    tcsBootstrap.WaitFor(Bootstrap().Task);
                }
                else
                {
                    tcsBootstrap.SetException(new TaskFailedException("Could not reach anyone with the broadcast. " + tp.TryGetException()));
                }
            });

            return tcsBootstrap;
        }

        private TcsWrappedBootstrap<Task> BootstrapPing(PeerAddress address)
        {
            var tcsBootstrap = new TcsWrappedBootstrap<Task>();

            var taskPing = _peer.Ping().SetPeerAddress(address).SetIsTcpPing().Start();
            taskPing.ContinueWith(tp =>
            {
                if (!tp.IsFaulted)
                {
                    PeerAddress = tp.Result;
                    BootstrapTo = new List<PeerAddress>(1) { PeerAddress };
                    tcsBootstrap.SetBootstrapTo(BootstrapTo);
                    tcsBootstrap.WaitFor(Bootstrap().Task);
                }
                else
                {
                    tcsBootstrap.SetException(new TaskFailedException("Could not reach anyone with bootstrap."));
                }
            });

            return tcsBootstrap;
        }

        private TcsWrappedBootstrap<Task> Bootstrap()
        {
            var tcsBootstrap = new TcsWrappedBootstrap<Task>();
            tcsBootstrap.SetBootstrapTo(BootstrapTo);

            int conn = RoutingConfiguration.Parallel;
            var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(conn, 0);
            Utils.Utils.AddReleaseListener(taskCc, tcsBootstrap.Task); // TODO correct?
            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    var routingBuilder = CreateBuilder(RoutingConfiguration, IsForceRoutingOnlyToSelf);
                    var taskBootstrapDone = _peer.DistributedRouting.Bootstrap(BootstrapTo, routingBuilder, tcc.Result);
                    tcsBootstrap.WaitFor(taskBootstrapDone);
                }
                else
                {
                    tcsBootstrap.SetException(tcc.TryGetException());
                }
            });

            return tcsBootstrap;
        }

        private static RoutingBuilder CreateBuilder(RoutingConfiguration routingConfiguration,
            bool forceRoutingOnlyToSelf)
        {
            var routingBuilder = new RoutingBuilder
            {
                Parallel = routingConfiguration.Parallel,
                MaxNoNewInfo = routingConfiguration.MaxNoNewInfoDiff,
                MaxDirectHits = Int32.MaxValue,
                MaxFailures = routingConfiguration.MaxFailures,
                MaxSuccess = routingConfiguration.MaxSuccess
            };
            routingBuilder.SetIsRoutingOnlyToSelf(forceRoutingOnlyToSelf);
            return routingBuilder;
        }
    }
}
