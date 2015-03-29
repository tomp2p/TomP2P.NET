using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection.Windows;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Message;
using TomP2P.Core.Peers;

namespace TomP2P.Core.Connection
{
    /// <summary>
    /// The "server" part that accepts connections.
    /// </summary>
    public sealed class ChannelServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private MyUdpServer _udpServer;
        private MyTcpServer _tcpServer;

        // setup
        private readonly Bindings _interfaceBindings;

        /// <summary>
        /// The channel server configuration.
        /// </summary>
        public ChannelServerConfiguration ChannelServerConfiguration { get; private set; }
        private readonly Dispatcher _dispatcher;
        private readonly IList<IPeerStatusListener> _peerStatusListeners;

        private readonly DropConnectionInboundHandler _tcpDropConnectionInboundHandler;
        private readonly DropConnectionInboundHandler _udpDropConnectionInboundHandler;
        private readonly TomP2PSinglePacketUdp _udpDecoderHandler;

        // .NET
        private readonly TomP2POutbound _encoderHandler;
        private readonly TomP2PCumulationTcp _tcpDecoderHandler;

        /// <summary>
        /// Sets parameters and starts network device discovery.
        /// </summary>
        /// <param name="channelServerConfiguration">The server configuration that contains e.g. the handlers</param>
        /// <param name="dispatcher">The shared dispatcher.</param>
        /// <param name="peerStatusListeners">The status listeners for offline peers.</param>
        public ChannelServer(ChannelServerConfiguration channelServerConfiguration, Dispatcher dispatcher,
            IList<IPeerStatusListener> peerStatusListeners)
        {
            ChannelServerConfiguration = channelServerConfiguration;
            _interfaceBindings = channelServerConfiguration.BindingsIncoming;
            _dispatcher = dispatcher;
            _peerStatusListeners = peerStatusListeners;
            string status = DiscoverNetworks.DiscoverInterfaces(_interfaceBindings);
            Logger.Info("Status of interface search: {0}.", status);

            _tcpDropConnectionInboundHandler = new DropConnectionInboundHandler(channelServerConfiguration.MaxTcpIncomingConnections);
            _udpDropConnectionInboundHandler = new DropConnectionInboundHandler(channelServerConfiguration.MaxUdpIncomingConnections);
            _udpDecoderHandler = new TomP2PSinglePacketUdp(channelServerConfiguration.SignatureFactory);
            _tcpDecoderHandler = new TomP2PCumulationTcp(channelServerConfiguration.SignatureFactory);
            _encoderHandler = new TomP2POutbound(false, channelServerConfiguration.SignatureFactory);
        }

        /// <summary>
        /// Starts to listen to UDP and TCP ports.
        /// </summary>
        /// <returns></returns>
        public bool Startup()
        {
            if (!ChannelServerConfiguration.IsDisableBind)
            {
                if (_interfaceBindings.IsListenAll)
                {
                    Logger.Info("Listening for broadcasts on UDP port {0} and TCP port {1}.",
                        ChannelServerConfiguration.Ports.UdpPort,
                        ChannelServerConfiguration.Ports.TcpPort);
                    if (!StartupTcp(new IPEndPoint(IPAddress.Any, ChannelServerConfiguration.Ports.TcpPort))
                        || !StartupUdp(new IPEndPoint(IPAddress.Any, ChannelServerConfiguration.Ports.UdpPort)))
                    {
                        Logger.Warn("Cannot bind TCP or UDP.");
                        return false;
                    }
                }
                else
                {
                    foreach (IPAddress address in _interfaceBindings.FoundAddresses)
                    {
                        Logger.Info("Listening on address {0}, UDP port {1}, TCP port {2}.", address,
                            ChannelServerConfiguration.Ports.UdpPort,
                            ChannelServerConfiguration.Ports.TcpPort);
                        if (!StartupTcp(new IPEndPoint(address, ChannelServerConfiguration.Ports.TcpPort))
                            || !StartupUdp(new IPEndPoint(address, ChannelServerConfiguration.Ports.UdpPort)))
                        {
                            Logger.Warn("Cannot bind TCP or UDP.");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Starts to listen on a UDP port.
        /// </summary>
        /// <param name="listenAddress">The address to listen to.</param>
        /// <returns>True, if startup was successful.</returns>
        private bool StartupUdp(IPEndPoint listenAddress)
        {
            try
            {
                var pipeline = GetPipeline(false);
                _udpServer = new MyUdpServer(listenAddress, pipeline);
                // rest of config done in MyUdpServer
                _udpServer.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured when starting up UDP server.", ex);
                return false;
            }
        }

        /// <summary>
        /// Starts to listen on a TCP port.
        /// </summary>
        /// <param name="listenAddress">The address to listen to.</param>
        /// <returns>True, if startup was successful.</returns>
        private bool StartupTcp(IPEndPoint listenAddress)
        {
            try
            {
                var pipeline = GetPipeline(true);
                _tcpServer = new MyTcpServer(listenAddress, pipeline);
                // rest of config done in MyTcpServer
                _tcpServer.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured when starting up TCP server.", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates the handler pipeline. After this, it passes the user-set pipeline 
        /// filter where the handlers can be modified.
        /// </summary>
        /// <param name="isTcp"></param>
        private Pipeline GetPipeline(bool isTcp)
        {
            var timeoutFactory = new TimeoutFactory(null, ChannelServerConfiguration.IdleTcpSeconds,
                _peerStatusListeners, "Server");
            var pipeline = new Pipeline();

            if (isTcp)
            {
                pipeline.AddLast("dropconnection", _tcpDropConnectionInboundHandler);
                pipeline.AddLast("timeout0", timeoutFactory.CreateIdleStateHandlerTomP2P());
                pipeline.AddLast("timeout1", timeoutFactory.CreateTimeHandler());
                pipeline.AddLast("decoder", _tcpDecoderHandler);
            }
            else
            {
                // no need for a timeout handler, since whole packet arrives or nothing
                // different from TCP where the stream can be closed by the remote peer
                // in the middle of the transmission
                pipeline.AddLast("dropconnection", _udpDropConnectionInboundHandler);
                pipeline.AddLast("decoder", _udpDecoderHandler);
            }
            pipeline.AddLast("encoder", _encoderHandler);
            pipeline.AddLast("dispatcher", _dispatcher);

            var filteredPipeline = ChannelServerConfiguration.PipelineFilter.Filter(pipeline, isTcp, false);
            return filteredPipeline;
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public async Task ShutdownAsync()
        {
            // TODO check if server can be shut down async, otherwise remove async
            // shutdown both UDP and TCP server sockets
            if (_udpServer != null)
            {
                Logger.Debug("Shutting down UDP server...");
                _udpServer.Stop();
                Logger.Debug("UDP server shut down.");
            }
            if (_tcpServer != null)
            {
                Logger.Debug("Shutting down TCP server...");
                _tcpServer.Stop();
                Logger.Debug("TCP server shut down.");
            }
        }
    }
}
