using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Futures;
using TomP2P.Message;
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

        /// <summary>
        /// Ping with UDP or TCP, but do not send yet.
        /// </summary>
        /// <param name="remotePeer"></param>
        /// <param name="configuration"></param>
        public RequestHandler<FutureResponse> Ping(PeerAddress remotePeer, IConnectionConfiguration configuration)
        {
            return CreateHandler(remotePeer, Message.Message.MessageType.Request1, configuration);
        }

        public Task<Message.Message> PingUdp(PeerAddress remotePeer, ChannelCreator channelCreator,
            IConnectionConfiguration configuration)
        {
            return Ping(remotePeer, configuration).SendUdpAsync(channelCreator);

            // TODO return the TCS task from the RequestHandler
        }

        /// <summary>
        /// Creates a RequestHandler.
        /// </summary>
        /// <param name="remotePeer">The destination peer.</param>
        /// <param name="type">The type of the request.</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private RequestHandler<FutureResponse> CreateHandler(PeerAddress remotePeer, Message.Message.MessageType type,
            IConnectionConfiguration configuration)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Ping.GetNr(), type);
            
            var tcs = new TaskCompletionSource<Message.Message>(TaskCreationOptions.None);

            return new RequestHandler<FutureResponse>(tcs, PeerBean, ConnectionBean, configuration);
        }

        public override void HandleResponse(Message.Message message, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            // server-side:
            // comes from DispatchHandler
            // Responder now responds the result...
            throw new NotImplementedException();
        }
    }
}
