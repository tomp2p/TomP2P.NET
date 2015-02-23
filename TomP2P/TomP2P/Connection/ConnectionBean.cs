﻿using System.Threading;
using Timer = System.Timers.Timer;

namespace TomP2P.Connection
{
    /// <summary>
    /// A bean that holds sharable configuration settings for the peer.
    /// The non-sharable configurations are stored in a <see cref="PeerBean"/>.
    /// </summary>
    public class ConnectionBean
    {
        // TODO THREAD_NAME needed?
        public static readonly int DefaultTcpIdleSeconds = 5;
        public static readonly int DefaultUdpIdleSeconds = 5;
        public static readonly int DefaultConnectionTimeoutTcp = 3000;
        public static readonly int UdpLimit = 1400;

        /// <summary>
        /// The P2P ID.
        /// </summary>
        public int P2PId { get; private set; }
        /// <summary>
        /// The dispatcher object that receives all messages.
        /// </summary>
        public Dispatcher Dispatcher { get; private set; }
        /// <summary>
        /// The sender object that sends out messages.
        /// </summary>
        public Sender Sender { get; private set; }
        /// <summary>
        /// The channel server that listens on incoming connections.
        /// </summary>
        public ChannelServer ChannelServer { get; private set; }
        /// <summary>
        /// The connection reservation that is responsible for resource management.
        /// </summary>
        public Reservation Reservation { get; private set; }
        /// <summary>
        /// The configuration that is responsible for the resource numbers.
        /// </summary>
        public ChannelClientConfiguration ResourceConfiguration { get; private set; }
        /// <summary>
        /// The timer used for the discovery.
        /// </summary>
        public Timer Timer { get; private set; }
        /// <summary>
        /// .NET-specific: To be cancelled when the ConnectionBean.Timer stops.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        /// <summary>
        /// The connection bean with unmodifiable objects. Once it is set, it cannot be changed.
        /// If it is required to change, then the peer must be shut down and a new one created.
        /// </summary>
        /// <param name="p2pId">The P2P ID.</param>
        /// <param name="dispatcher">The dispatcher object that receives all messages.</param>
        /// <param name="sender">The sender object that sends out messages.</param>
        /// <param name="channelServer">The channel server that listens on incoming connections.</param>
        /// <param name="reservation">The connection reservation that is responsible for resource management.</param>
        /// <param name="resourceConfiguration">The configuration that is responsible for the resource numbers.</param>
        /// <param name="timer">The timer for the discovery process.</param>
        /// <param name="cts">.NET-specific: To be cancelled when the ConnectionBean.Timer stops.</param>
        public ConnectionBean(int p2pId, Dispatcher dispatcher, Sender sender, ChannelServer channelServer,
            Reservation reservation, ChannelClientConfiguration resourceConfiguration, Timer timer, CancellationTokenSource cts)
        {
            P2PId = p2pId;
            Dispatcher = dispatcher;
            Sender = sender;
            ChannelServer = channelServer;
            Reservation = reservation;
            ResourceConfiguration = resourceConfiguration;
            Timer = timer;
            CancellationTokenSource = cts;
        }
    }
}
