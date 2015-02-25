using System;
using System.Collections.Generic;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;

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
        private bool? _behindFirewall = null;
        public IBroadcastHandler BroadcastHandler { get; private set; }
        public IBloomfilterFactory BloomfilterFactory { get; private set; }
        public ExecutorService Timer { get; private set; }
        public MaintenanceTask MaintenanceTask { get; private set; }
        public Random Random { get; private set; }
        private readonly IList<IPeerInit> _toInitialize = new List<IPeerInit>(1);

        // enable/disable RPC/P2P/other
        public bool IsEnabledHandshakeRpc { get; private set; }
        public bool IsEnabledNeighborRpc { get; private set; }
        public bool IsEnabledDirectDataRpc { get; private set; }
        public bool IsEnabledBroadcastRpc { get; private set; }
        public bool IsEnabledRoutingRpc { get; private set; }
        public bool IsEnabledMaintenanceRpc { get; private set; }
        public bool IsEnabledQuitRpc { get; private set; }

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
            BloomfilterFactory = null;
            MaintenanceTask = null;

            IsEnabledHandshakeRpc = true;
            IsEnabledNeighborRpc = true;
            IsEnabledDirectDataRpc = true;
            IsEnabledBroadcastRpc = true;
            IsEnabledRoutingRpc = true;
            IsEnabledMaintenanceRpc = true;
            IsEnabledQuitRpc = true;
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
            if (_behindFirewall == null)
            {
                _behindFirewall = false;
            }

            if (ChannelServerConfiguration == null)
            {
                ChannelServerConfiguration = CreateDefaultChannelServerConfiguration();
                ChannelServerConfiguration.SetPortsForwarding(new Ports(TcpPortForwarding, UdpPortForwarding));
                if (TcpPort == -1)
                {
                    TcpPort = Ports.DefaultPort;
                }
                if (UdpPort == -1)
                {
                    UdpPort = Ports.DefaultPort;
                }
                ChannelServerConfiguration.SetPorts(new Ports(TcpPort, UdpPort));
                ChannelServerConfiguration.SetIsBehindFirewall(_behindFirewall.Value);
            }
            if (ChannelClientConfiguration == null)
            {
                ChannelClientConfiguration = CreateDefaultChannelClientConfiguration();
            }

            if (KeyPair == null)
            {
                KeyPair = EmptyKeyPair;
            }
            if (P2PId == -1)
            {
                P2PId = 1;
            }

            if (InterfaceBindings == null)
            {
                InterfaceBindings = new Bindings();
            }
            ChannelServerConfiguration.SetBindingsIncoming(InterfaceBindings);
            if (ExternalBindings == null)
            {
                ExternalBindings = new Bindings();
            }
            ChannelClientConfiguration.SetBindingsOutgoing(ExternalBindings);

            if (PeerMap == null)
            {
                PeerMap = new PeerMap(new PeerMapConfiguration(PeerId));
            }

            if (MasterPeer == null && Timer == null)
            {
                Timer = new ExecutorService();
            }

            PeerCreator peerCreator;
            if (MasterPeer != null)
            {
                // create slave peer
                peerCreator = new PeerCreator(MasterPeer.PeerCreator, PeerId, KeyPair);
            }
            else
            {
                // create master peer
                peerCreator = new PeerCreator(P2PId, PeerId, KeyPair, ChannelServerConfiguration, ChannelClientConfiguration, Timer);
            }

            var peer = new Peer(P2PId, PeerId, peerCreator);

            var peerBean = peerCreator.PeerBean;
            peerBean.AddPeerStatusListener(PeerMap);

            var connectionBean = peerCreator.ConnectionBean;

            peerBean.SetPeerMap(PeerMap);
            peerBean.SetKeyPair(KeyPair);

            if (BloomfilterFactory == null)
            {
                peerBean.SetBloomfilterFactory(new DefaultBloomFilterFactory());
            }

            if (BroadcastHandler == null)
            {
                BroadcastHandler = new DefaultBroadcastHandler(peer, new Random());
            }

            // set/enable RPC
            if (IsEnabledHandshakeRpc)
            {
                var pingRpc = new PingRpc(peerBean, connectionBean);
                peer.SetPingRpc(pingRpc);
            }
            if (IsEnabledQuitRpc)
            {
                var quitRpc = new QuitRpc(peerBean, connectionBean);
                quitRpc.AddPeerStatusListener(PeerMap);
                peer.SetQuitRpc(quitRpc);
            }
            if (IsEnabledNeighborRpc)
            {
                var neighborRpc = new NeighborRpc(peerBean, connectionBean);
                peer.SetNeighborRpc(neighborRpc);
            }
            if (IsEnabledDirectDataRpc)
            {
                var directDataRpc = new DirectDataRpc(peerBean, connectionBean);
                peer.SetDirectDataRpc(directDataRpc);
            }
            if (IsEnabledBroadcastRpc)
            {
                var broadcastRpc = new BroadcastRpc(peerBean, connectionBean, BroadcastHandler);
                peer.SetBroadcastRpc(broadcastRpc);
            }
            if (IsEnabledRoutingRpc && IsEnabledNeighborRpc)
            {
                var routing = new DistributedRouting(peerBean, peer.NeighborRpc);
                peer.SetDistributedRouting(routing);
            }

            if (MaintenanceTask == null && IsEnabledMaintenanceRpc)
            {
                MaintenanceTask = new MaintenanceTask();
            }
            if (MaintenanceTask != null)
            {
                MaintenanceTask.Init(peer, connectionBean.Timer);
                MaintenanceTask.AddMaintainable(PeerMap);
            }
            peerBean.SetMaintenanceTask(MaintenanceTask);

            // set the ping builder for the heart beat
            connectionBean.Sender.SetPingBuilderFactory(new PingBuilderFactory(peer));

            foreach (var peerInit in _toInitialize)
            {
                peerInit.Init(peer);
            }
            return peer;
        }

        public static ChannelServerConfiguration CreateDefaultChannelServerConfiguration()
        {
            return new ChannelServerConfiguration().
                SetBindingsIncoming(new Bindings()).
                SetIsBehindFirewall(false).
                SetPipelineFilter(new DefaultPipelineFilter()).
                SetSignatureFactory(new DsaSignatureFactory()).
                // these two values may be overwritten in the peer builder
                SetPorts(new Ports(Ports.DefaultPort, Ports.DefaultPort)).
                SetPortsForwarding(new Ports(Ports.DefaultPort, Ports.DefaultPort));
        }

        public static ChannelClientConfiguration CreateDefaultChannelClientConfiguration()
        {
            return new ChannelClientConfiguration().
                SetBindingsOutgoing(new Bindings()).
                SetMaxPermitsPermanentTcp(MaxPermitsPermanentTcp).
                SetMaxPermitsTcp(MaxPermitsTcp).
                SetMaxPermitsUdp(MaxPermitsUdp).
                SetPipelineFilter(new DefaultPipelineFilter()).
                SetSignatureFactory(new DsaSignatureFactory());
        }

        public PeerBuilder SetKeyPair(KeyPair keyPair)
        {
            KeyPair = keyPair;
            return this;
        }

        public PeerBuilder SetP2PId(int p2PId)
        {
            P2PId = p2PId;
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

        public PeerBuilder SetBloomfilterFactory(IBloomfilterFactory bloomfilterFactory)
        {
            BloomfilterFactory = bloomfilterFactory;
            return this;
        }

        public PeerBuilder SetMaintenanceTask(MaintenanceTask maintenanceTask)
        {
            MaintenanceTask = maintenanceTask;
            return this;
        }

        public PeerBuilder SetRandom(Random random)
        {
            Random = random;
            return this;
        }

        public PeerBuilder Init(IPeerInit init)
        {
            _toInitialize.Add(init);
            return this;
        }

        public PeerBuilder Init(params IPeerInit[] inits)
        {
            foreach (var init in inits)
            {
                _toInitialize.Add(init);
            }
            return this;
        }

        public PeerBuilder SetTimer(ExecutorService timer)
        {
            Timer = timer;
            return this;
        }

        public PeerBuilder SetEnableHandshakeRpc(bool enableHandshakeRpc)
        {
            IsEnabledHandshakeRpc = enableHandshakeRpc;
            return this;
        }

        public PeerBuilder SetEnableNeighborRpc(bool enableNeighborRpc)
        {
            IsEnabledNeighborRpc = enableNeighborRpc;
            return this;
        }

        public PeerBuilder SetEnableDirectDataRpc(bool enabeDirectDataRpc)
        {
            IsEnabledDirectDataRpc = enabeDirectDataRpc;
            return this;
        }

        public PeerBuilder SetEnableBroadcastRpc(bool enableBroadcastRpc)
        {
            IsEnabledBroadcastRpc = enableBroadcastRpc;
            return this;
        }

        public PeerBuilder SetEnableRoutingRpc(bool enableRoutingRpc)
        {
            IsEnabledRoutingRpc = enableRoutingRpc;
            return this;
        }

        public PeerBuilder SetEnableMaintenanceRpc(bool enableMaintenanceRpc)
        {
            IsEnabledMaintenanceRpc = enableMaintenanceRpc;
            return this;
        }

        public PeerBuilder SetEnableQuitRpc(bool enableQuitRpc)
        {
            IsEnabledQuitRpc = enableQuitRpc;
            return this;
        }

        /// <summary>
        /// True, if this peer is behind a firewall and cannot be accessed directly.
        /// </summary>
        public bool IsBehindFirewall
        {
            get { return _behindFirewall != null && _behindFirewall.Value; }
        }

        /// <summary>
        /// Sets peer to be behind a firewall and not directly accessable.
        /// </summary>
        /// <returns></returns>
        public PeerBuilder SetBehindFirewall()
        {
            return SetBehindFirewall(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behindFirewall">Set to true, if this peer is behind a firewall and
        /// cannot be accessed directly.</param>
        /// <returns></returns>
        public PeerBuilder SetBehindFirewall(bool behindFirewall)
        {
            _behindFirewall = behindFirewall;
            return this;
        }

        /// <summary>
        /// Default ping builder factory for the sender (heart beat).
        /// </summary>
        private class PingBuilderFactory : IPingBuilderFactory
        {
            private readonly Peer _peer;

            public PingBuilderFactory(Peer peer)
            {
                _peer = peer;
            }

            public PingBuilder Create()
            {
                return _peer.Ping();
            }
        }
    }
}
