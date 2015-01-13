using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.P2P
{
    /// <summary>
    /// The builder of a <see cref="Peer"/> class.
    /// </summary>
    public class PeerBuilder
    {
        public static readonly IPublicKey EmptyPublicKey = null; // TODO implement accurate empty key
        private static readonly KeyPair EmptyKeyPair = new KeyPair(EmptyPublicKey, null);

        // if the permits are chosen too high, then we might run into timeout
        // as we can't handle that many connections within the time limit
        private const int MaxPermitsPermanentTcp = 250;
        private const int MaxPermitsUdp = 250;
        private const int MaxPermitsTcp = 250;

        // required
        public Number160 PeerId { get; private set; }

        // optional with reasonable defaults
        public KeyPair KeyPair { get; private set; }
        public int P2PId { get; private set; }
        public int TcpPort { get; private set; }
        public int UdpPort { get; private set; }
        public int TcpPortForwarding { get; private set; }
        public int UdpPortForwarding { get; private set; }
        public Bindings InterfaceBindings { get; private set; }
        public Bindings ExternalBindings { get; private set; }
        public PeerMap PeerMap { get; private set; }
        public Peer MasterPeer { get; private set; }
        public ChannelServerConfiguration ChannelServerConfiguration { get; private set; }
        public ChannelClientConfiguration ChannelClientConfiguration { get; private set; }
        private bool _behindFirewall = false; // TODO Java uses reference type and null
        public IBroadcastHandler BroadcastHandler { get; private set; }
        private IBloomfilterFactory _bloomfilterFactory = null;
        // TODO find ScheduledExecuterService equivalent
        private MaintenanceTask _maintenanceTask = null;
        private Random _random = null;
        private IList<IPeerInit> _toInitialize = new List<IPeerInit>(1);

        // enable/disable RPC/P2P/other
        private bool _enableHandshakeRpc = true;
        private bool _enableNeighborRpc = true;
        private bool _enableDirectDataRpc = true;
        private bool _enableBroadcast = true;
        private bool _enableRouting = true;
        private bool _enableMaintenance = true;
        private bool _enableQuitRpc = true;

        /// <summary>
        /// .NET constructor used to set default property values.
        /// </summary>
        private PeerBuilder(Number160 peerId, KeyPair keyPair)
        {
            PeerId = peerId;
            KeyPair = keyPair;

            P2PId = -1;
            TcpPort = -1;
            UdpPort = -1;
            TcpPortForwarding = -1;
            UdpPortForwarding = -1;
            InterfaceBindings = null;
            ExternalBindings = null;
            PeerMap = null;
            MasterPeer = null;
            ChannelServerConfiguration = null;
            ChannelClientConfiguration = null;
            BroadcastHandler = null;
        }

        /// <summary>
        /// Creates a peer builder with the provided peer ID and an empty key pair.
        /// </summary>
        /// <param name="peerId">The peer ID.</param>
        public PeerBuilder(Number160 peerId)
            : this(peerId, null)
        { }

        /// <summary>
        /// Creates a peer builder with the provided key pair and a peer ID that is
        /// generated out of this key pair.
        /// </summary>
        /// <param name="keyPair">The public private key pair.</param>
        public PeerBuilder(KeyPair keyPair)
            : this(Utils.Utils.MakeShaHash(keyPair.PublicKey.GetEncoded()), keyPair)
        { }

        /// <summary>
        /// Creates a peer and starts to listen for incoming connections.
        /// </summary>
        /// <returns>The peer that can operate in the P2P network.</returns>
        public Peer Start()
        {
            /*if (_behindFirewall == null)
            {
                _behindFirewall = false;
            }*/

            if (_channel)
        }

        public PeerBuilder SetKeyPair(KeyPair keyPair)
        {
            KeyPair = keyPair;
            return this;
        }

        public PeerBuilder SetP2PId(int p2pId)
        {
            P2PId = p2pId;
            return this;
        }

        public PeerBuilder SetTcpPortForwarding(int tcpPortForwarding)
        {
            TcpPortForwarding = tcpPortForwarding;
            return this;
        }

        public PeerBuilder SetUdpPortForwarding(int udpPortForwarding)
        {
            UdpPortForwarding = udpPortForwarding;
            return this;
        }

        public PeerBuilder SetTcpPort(int tcpPort)
        {
            TcpPort = tcpPort;
            return this;
        }

        public PeerBuilder SetUdpPort(int udpPort)
        {
            UdpPort = udpPort;
            return this;
        }

        /// <summary>
        /// Sets the UDP and TCP ports to the specified value.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public PeerBuilder SetPorts(int port)
        {
            UdpPort = port;
            TcpPort = port;
            return this;
        }

        /// <summary>
        /// Sets the external UDP and TCP ports to the specified value.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public PeerBuilder SetPortsExternal(int port)
        {
            UdpPortForwarding = port;
            TcpPortForwarding = port;
            return this;
        }

        /// <summary>
        /// Sets the interface- and external bindings to the specified value.
        /// </summary>
        /// <param name="bindings"></param>
        /// <returns></returns>
        public PeerBuilder SetBindings(Bindings bindings)
        {
            InterfaceBindings = bindings;
            ExternalBindings = bindings;
            return this;
        }

        public PeerBuilder SetInterfaceBindings(Bindings interfaceBindings)
        {
            InterfaceBindings = interfaceBindings;
            return this;
        }

        public PeerBuilder SetExternalBindings(Bindings externalBindings)
        {
            ExternalBindings = externalBindings;
            return this;
        }

        public PeerBuilder SetPeerMap(PeerMap peerMap)
        {
            PeerMap = peerMap;
            return this;
        }

        public PeerBuilder SetMasterPeer(Peer masterPeer)
        {
            MasterPeer = masterPeer;
            return this;
        }

        public PeerBuilder SetChannelServerConfiguration(ChannelServerConfiguration channelServerConfiguration)
        {
            ChannelServerConfiguration = channelServerConfiguration;
            return this;
        }

        public PeerBuilder SetChannelClientConfiguration(ChannelClientConfiguration channelClientConfiguration)
        {
            ChannelClientConfiguration = channelClientConfiguration;
            return this;
        }

        public PeerBuilder SetBroadcastHandler(IBroadcastHandler broadcastHandler)
        {
            BroadcastHandler = broadcastHandler;
            return this;
        }
    }
}
