using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    public class DiscoverBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static TcsDiscover TcsDiscoverShutdown;

        private readonly Peer _peer;
        public IPAddress InetAddress { get; private set; }
        public int PortUdp { get; private set; }
        public int PortTcp { get; private set; }
        public PeerAddress PeerAddress { get; private set; }
        private PeerAddress _senderAddress;

        public int DiscoverTimeoutSec { get; private set; }

        private IConnectionConfiguration _configuration;

        public TcsDiscover TcsDiscover { get; private set; }

        // static constructor
        static DiscoverBuilder()
        {
            // TODO implement
            throw new NotImplementedException();
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
        }
    }
}
