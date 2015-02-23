using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    public class DiscoverBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly TcsDiscover TcsDiscoverShutdown = new TcsDiscover();

        private readonly Peer _peer;
        public IPAddress InetAddress { get; private set; }
        public int PortUdp { get; private set; }
        public int PortTcp { get; private set; }
        public PeerAddress PeerAddress { get; private set; }
        public PeerAddress SenderAddress { get; private set; }

        public int DiscoverTimeoutSec { get; private set; }

        private IConnectionConfiguration _configuration;

        public TcsDiscover TcsDiscover { get; private set; }

        // static constructor
        static DiscoverBuilder()
        {
            TcsDiscoverShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public DiscoverBuilder(Peer peer)
        {
            _peer = peer;
            PortUdp = Ports.DefaultPort;
            PortTcp = Ports.DefaultPort;
            DiscoverTimeoutSec = 5;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress)
        {
            InetAddress = inetAddress;
            return this;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress, int port)
        {
            InetAddress = inetAddress;
            PortTcp = port;
            PortUdp = port;
            return this;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress, int portTcp, int portUdp)
        {
            InetAddress = inetAddress;
            PortTcp = portTcp;
            PortUdp = portUdp;
            return this;
        }

        public DiscoverBuilder SetPortTcp(int portTcp)
        {
            PortTcp = portTcp;
            return this;
        }

        public DiscoverBuilder SetPortUdp(int portUdp)
        {
            PortUdp = portUdp;
            return this;
        }

        public DiscoverBuilder SetPorts(int port)
        {
            PortTcp = port;
            PortUdp = port;
            return this;
        }

        public DiscoverBuilder SetPeerAddress(PeerAddress peerAddress)
        {
            PeerAddress = peerAddress;
            return this;
        }

        public DiscoverBuilder SetSenderAddress(PeerAddress senderAddress)
        {
            SenderAddress = senderAddress;
            return this;
        }

        public DiscoverBuilder SetDiscoverTimeoutSec(int discoverTimeoutSec)
        {
            DiscoverTimeoutSec = discoverTimeoutSec;
            return this;
        }

        public DiscoverBuilder SetTcsDiscover(TcsDiscover tcsDiscover)
        {
            TcsDiscover = tcsDiscover;
            return this;
        }

        public TcsDiscover Start()
        {
            if (_peer.IsShutdown)
            {
                return TcsDiscoverShutdown;
            }

            if (PeerAddress == null && InetAddress != null)
            {
                PeerAddress = new PeerAddress(Number160.Zero, InetAddress, PortTcp, PortUdp);
            }
            if (PeerAddress == null)
            {
                throw new ArgumentException("Peer address or inet address required.");
            }
            if (_configuration == null)
            {
                _configuration = new DefaultConnectionConfiguration();
            }
            if (TcsDiscover == null)
            {
                TcsDiscover = new TcsDiscover();
            }

            return Discover(PeerAddress, _configuration, TcsDiscover);
        }

        /// <summary>
        /// Discover attempts to find the external IP address of this peer. This is done by first trying to set
        /// UPNP with port forwarding (gives us the external address), query UPNP for the external address, and
        /// pinging a well-known peer. The fallback is NAT-PMP.
        /// </summary>
        /// <param name="peerAddress">The peer address. Since pings are used the peer ID can be Number160.Zero.</param>
        /// <param name="configuration"></param>
        /// <param name="tcsDiscover"></param>
        /// <returns>The future discover. This holds also the real ID of the peer we send the discover request.</returns>
        private TcsDiscover Discover(PeerAddress peerAddress, IConnectionConfiguration configuration,
            TcsDiscover tcsDiscover)
        {
            var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(1, 2);
            Utils.Utils.AddReleaseListener(taskCc, TcsDiscover.Task);
            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    Discover(tcsDiscover, peerAddress, tcc.Result, configuration);
                }
                else
                {
                    tcsDiscover.SetException(tcc.TryGetException());
                }
            });

            return tcsDiscover;
        }

        /// <summary>
        /// Needs 3 connections. Cleans up channel creator, which means they will be released.
        /// </summary>
        /// <param name="tcsDiscover"></param>
        /// <param name="peerAddress"></param>
        /// <param name="cc"></param>
        /// <param name="configuration"></param>
        private void Discover(TcsDiscover tcsDiscover, PeerAddress peerAddress, ChannelCreator cc,
            IConnectionConfiguration configuration)
        {
            _peer.PingRpc.

            // TODO implement
            throw new NotImplementedException();
        }
    }
}
