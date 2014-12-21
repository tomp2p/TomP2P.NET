using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// The Ping message handler. Also used for NAT detection and other things.
    /// </summary>
    public class PingRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly int WaitTime = 10*1000;

        private readonly IList<IPeerReachable> _reachableListeners = new List<IPeerReachable>(1);
        private readonly IList<IPeerReceivedBroadcastPing> _receivedBroadcastPingListeners = new List<IPeerReceivedBroadcastPing>(1);

        // TODO needed?
        // used for testing and debugging 
        private readonly bool _enable;
        private readonly bool _wait;

        /// <summary>
        /// Creates a new handshake RPC with listeners.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        public PingRpc(PeerBean peerBean, ConnectionBean connectionBean)
            : this(peerBean, connectionBean, true, true, false)
        { }

        /// <summary>
        /// Constructor that is only called from this class or from test cases.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="enable">Used for test cases, set to true in production.</param>
        /// <param name="register">Used for test cases, set to true in production.</param>
        /// <param name="wait">Used for test cases, set to false in production.</param>
        private PingRpc(PeerBean peerBean, ConnectionBean connectionBean, bool enable, bool register, bool wait)
            : base(peerBean, connectionBean)
        {
            _enable = enable;
            _wait = wait;
            if (register)
            {
                // TODO implement
            }
        }

        // TODO return RequestHandler<FutureResponse>
        /// <summary>
        /// Ping with UDP or TCP, but do not send yet.
        /// </summary>
        /// <param name="remotePeer"></param>
        /// <param name="configuration"></param>
        public void Ping(PeerAddress remotePeer, IConnectionConfiguration configuration)
        {
            CreateHandler(remotePeer, Message.Message.MessageType.Request1, configuration);
        }

        // TODO return RequestHandler<FutureResponse>
        private void CreateHandler(PeerAddress remotePeer, Message.Message.MessageType type,
            IConnectionConfiguration configuration)
        {
            var message = CreateMessage(remotePeer, Rpc.Commands.Ping.GetNr(), type);

            // TODO implement FutureResponse and ReuqestHandler<FutureResponse>
        }

        /// <summary>
        /// Creates a request message and fills it with peer bean and connection bean parameters.
        /// </summary>
        /// <param name="recipient">The recipient of this message.</param>
        /// <param name="name">The command type.</param>
        /// <param name="type">The request type.</param>
        /// <returns>The created request message.</returns>
        public Message.Message CreateMessage(PeerAddress recipient, sbyte name, Message.Message.MessageType type)
        {
            return new Message.Message()
                .SetRecipient(recipient)
                .SetSender(PeerBean.ServerPeerAddress())
                .SetCommand(name)
                .SetType(type)
                .SetVersion(ConnectionBean.P2PId());
        }
    }
}
