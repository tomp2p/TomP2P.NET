
namespace TomP2P.Connection
{
    /// <summary>
    /// The configuration for the server.
    /// </summary>
    public class ChannelServerConfiguration : IConnectionConfiguration
    {
        /// <summary>
        /// True, if this peer is behind a firewall and cannot be accessed directly.
        /// </summary>
        public bool IsBehindFirewall { get; private set; }
        /// <summary>
        /// True, if the bind to poerts should be omitted.
        /// </summary>
        public bool IsDisableBind { get; private set; }
        
        private int _idleTcpSeconds = ConnectionBean.DefaultTcpIdleSeconds;
        private int _idleUdpSeconds = ConnectionBean.DefaultUdpIdleSeconds;
        private int _connectionTimeoutTcpMillis = ConnectionBean.DefaultConnectionTimeoutTcp;

        private IPipelineFilter _pipelineFilter = null;

        // interface bindings
        public Bindings BindingsIncoming { get; private set; }

        /// <summary>
        /// The factory for the signature.
        /// </summary>
        public ISignatureFactory SignatureFactory { get; private set; }

        public bool IsForceTcp { get; private set; }
        public bool IsForceUdp { get; private set; }

        public Ports PortsForwarding { get; private set; }
        public Ports Ports { get; private set; }

        private int _maxTcpIncomingConnections = 1000;
        private int _maxUdpIncomingConnections = 1000;

        private int _heartBeatMillis = PeerConnection.HeartBeatMillisConst;

        /// <summary>
        /// Sets peer to be behind a firewall and not directly accessable.
        /// </summary>
        /// <returns>This class.</returns>
        public ChannelServerConfiguration SetIsBehindFirewall()
        {
            return SetIsBehindFirewall(true);
        }

        /// <summary>
        /// Set to true if this peer is behind a firewall and cannot be accessed directly.
        /// </summary>
        /// <param name="isBehindFirewall"></param>
        /// <returns>This class.</returns>
        public ChannelServerConfiguration SetIsBehindFirewall(bool isBehindFirewall)
        {
            IsBehindFirewall = isBehindFirewall;
            return this;
        }

        /// <summary>
        /// Sets that the bind to ports should be omitted.
        /// </summary>
        /// <returns>This class.</returns>
        public ChannelServerConfiguration SetIsDisableBind()
        {
            return SetIsDisableBind(true);
        }

        /// <summary>
        /// Set to true if the bind to ports should be omitted.
        /// </summary>
        /// <param name="isDisableBind"></param>
        /// <returns></returns>
        public ChannelServerConfiguration SetIsDisableBind(bool isDisableBind)
        {
            IsDisableBind = isDisableBind;
            return this;
        }

        /// <summary>
        /// The time that a connection can be idle before it is considered not active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        public int IdleTcpSeconds
        {
            get { return _idleTcpSeconds; }
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered not active for short-lived connections.
        /// </summary>
        /// <param name="idleTcpSeconds"></param>
        /// <returns>This class.</returns>
        public ChannelServerConfiguration SetIdleTcpSeconds(int idleTcpSeconds)
        {
            _idleTcpSeconds = idleTcpSeconds;
            return this;
        }

        /// <summary>
        /// The time that a connection can be idle before it is considered not active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        public int IdleUdpSeconds
        {
            get { return _idleUdpSeconds; }
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered not active for short-lived connections.
        /// </summary>
        /// <param name="idleUdpSeconds"></param>
        /// <returns></returns>
        public ChannelServerConfiguration SetIdleUdpSeconds(int idleUdpSeconds)
        {
            _idleUdpSeconds = idleUdpSeconds;
            return this;
        }

        /// <summary>
        /// Gets the filter for the pipeline, where the user can add, remove or change filters.
        /// </summary>
        public IPipelineFilter PipelineFilter
        {
            get { return _pipelineFilter; }
        }

        /// <summary>
        /// Sets the filter for the pipeline, where the user can add, remove or change filters.
        /// </summary>
        /// <returns></returns>
        public ChannelServerConfiguration SetPipelineFilter(IPipelineFilter pipelineFilter)
        {
            _pipelineFilter = pipelineFilter;
            return this;
        }

        /// <summary>
        /// Sets the factory for the signature.
        /// </summary>
        /// <param name="signatureFactory"></param>
        /// <returns>This class.</returns>
        public ChannelServerConfiguration SetSignatureFactory(ISignatureFactory signatureFactory)
        {
            SignatureFactory = signatureFactory;
            return this;
        }

        public int ConnectionTimeoutTcpMillis
        {
            get { return _connectionTimeoutTcpMillis; }
        }

        public ChannelServerConfiguration SetConnectionTimeoutTcpMillis(int connectionTimeoutTcpMillis)
        {
            _connectionTimeoutTcpMillis = connectionTimeoutTcpMillis;
            return this;
        }

        public ChannelServerConfiguration SetIsForceTcp()
        {
            return SetIsForceTcp(true);
        }

        public ChannelServerConfiguration SetIsForceTcp(bool isForceTcp)
        {
            IsForceTcp = isForceTcp;
            return this;
        }

        public ChannelServerConfiguration SetIsForceUdp()
        {
            return SetIsForceUdp(true);
        }

        public ChannelServerConfiguration SetIsForceUdp(bool isForceUdp)
        {
            IsForceUdp = isForceUdp;
            return this;
        }

        public ChannelServerConfiguration SetPortsForwarding(Ports portsForwarding)
        {
            PortsForwarding = portsForwarding;
            return this;
        }

        public ChannelServerConfiguration SetPorts(Ports ports)
        {
            Ports = ports;
            return this;
        }

        public ChannelServerConfiguration SetBindingsIncoming(Bindings bindingsIncoming)
        {
            BindingsIncoming = bindingsIncoming;
            return this;
        }

        public int MaxTcpIncomingConnections
        {
            get { return _maxTcpIncomingConnections; }
        }

        public ChannelServerConfiguration SetMaxTcpIncomingConnections(int maxTcpIncomingConnections)
        {
            _maxTcpIncomingConnections = maxTcpIncomingConnections;
            return this;
        }

        public int MaxUdpIncomingConnections
        {
            get { return _maxUdpIncomingConnections; }
        }

        public ChannelServerConfiguration SetMaxUdpIncomingConnections(int maxUdpIncomingConnections)
        {
            _maxUdpIncomingConnections = maxUdpIncomingConnections;
            return this;
        }

        public int HearBeatMillis
        {
            get { return _heartBeatMillis; }
        }

        public ChannelServerConfiguration SetHeartBeatMillis(int heartBeatMillis)
        {
            _heartBeatMillis = heartBeatMillis;
            return this;
        }
    }
}
