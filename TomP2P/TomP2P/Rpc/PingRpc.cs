using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
    /// The Ping requestMessage handler. Also used for NAT detection and other things.
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
                ConnectionBean.Dispatcher.RegisterIOHandler(peerBean.ServerPeerAddress.PeerId, peerBean.ServerPeerAddress.PeerId, this, Rpc.Commands.Ping.GetNr());
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

        public Task<Message.Message> PingUdpAsync(PeerAddress remotePeer, ChannelCreator channelCreator,
            IConnectionConfiguration configuration)
        {
            return Ping(remotePeer, configuration).SendUdpAsync(channelCreator);
        }

        /// <summary>
        /// Ping a UDP peer, but don't expect an answer.
        /// </summary>
        /// <param name="remotePeer">The destination peer.</param>
        /// <param name="channelCreator">The channel creator where we create a UDP channel.</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Task<Message.Message> FireUdp(PeerAddress remotePeer, ChannelCreator channelCreator,
            IConnectionConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ping a TCP peer, but don't expect an answer.
        /// </summary>
        /// <param name="remotePeer">The destination peer.</param>
        /// <param name="channelCreator">The channel creator where we create a TCP channel.</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Task<Message.Message> FireTcp(PeerAddress remotePeer, ChannelCreator channelCreator,
            IConnectionConfiguration configuration)
        {
            throw new NotImplementedException();
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
            // .NET-specific: create message and store it as AsyncState in the TaskCompletionSource
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Ping.GetNr(), type);

            var tcs = new TaskCompletionSource<Message.Message>(message);

            return new RequestHandler<FutureResponse>(tcs, PeerBean, ConnectionBean, configuration);
        }

        public override Message.Message HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder, bool isUdp, Socket channel)
        {
            // server-side:
            // comes from DispatchHandler
            // Responder now responds the result...

            if (!((requestMessage.Type == Message.Message.MessageType.RequestFf1 
                || requestMessage.Type == Message.Message.MessageType.Request1
                || requestMessage.Type == Message.Message.MessageType.Request2
                || requestMessage.Type == Message.Message.MessageType.Request3
                || requestMessage.Type == Message.Message.MessageType.Request3) 
                && requestMessage.Command == Rpc.Commands.Ping.GetNr()))
            {
                throw new ArgumentException("Request message type or command is wrong for this handler.");
            }

            Message.Message responseMessage;

            // probe
            if (requestMessage.Type == Message.Message.MessageType.Request3)
            {
                Logger.Debug("Respond to probing. Firing message to {0}.", requestMessage.Sender);
                responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);

                if (requestMessage.IsUdp)
                {
                    ConnectionBean.Reservation.CreateAsync(1, 0).ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                        {
                            Logger.Debug("Fire UDP to {0}.", requestMessage.Sender);
                            var taskResponse = FireUdp(requestMessage.Sender, t.Result,
                                ConnectionBean.ChannelServer.ChannelServerConfiguration);
                            Utils.Utils.AddReleaseListener(t.Result, taskResponse);
                        }
                        else
                        {
                            Utils.Utils.AddReleaseListener(t.Result);
                            Logger.Warn("Handling response for Request3 failed. (UDP) {0}", t.Exception);
                        }
                    });
                }
                else
                {
                    ConnectionBean.Reservation.CreateAsync(0, 1).ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                        {
                            Logger.Debug("Fire TCP to {0}.", requestMessage.Sender);
                            var taskResponse = FireTcp(requestMessage.Sender, t.Result,
                                ConnectionBean.ChannelServer.ChannelServerConfiguration);
                            Utils.Utils.AddReleaseListener(t.Result, taskResponse);
                        }
                        else
                        {
                            Utils.Utils.AddReleaseListener(t.Result);
                            Logger.Warn("Handling response for Request3 failed. (TCP) {0}", t.Exception);
                        }
                    });
                }
            }
            // discover
            else if (requestMessage.Type == Message.Message.MessageType.Request2)
            {
                Logger.Debug("Respond to discovering. Found {0}.", requestMessage.Sender);
                responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);
                responseMessage.SetNeighborSet(CreateNeighborSet(requestMessage.Sender));
            }
            // regular ping
            else if (requestMessage.Type == Message.Message.MessageType.Request1 ||
                     requestMessage.Type == Message.Message.MessageType.Request4)
            {
                Logger.Debug("Respond to regular ping. {0}.", requestMessage.Sender);
                // Test, if this is a broadcast message to ourselves.
                // If it is, do not reply.
                if (requestMessage.IsUdp // TODO this actually would be true for UDP --> see flag set in Decoder
                    && requestMessage.Sender.PeerId.Equals(PeerBean.ServerPeerAddress.PeerId)
                    && requestMessage.Recipient.PeerId.Equals(Number160.Zero))
                {
                    Logger.Warn("Don't respond. We are on the same peer, you should make this call.");
                    responder.ResponseFireAndForget(isUdp);
                }
                if (_enable)
                {
                    responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);
                    if (_wait)
                    {
                        Thread.Sleep(WaitTime);
                    }
                }
                else
                {
                    Logger.Debug("Don't respond.");
                    // used for debugging
                    if (_wait)
                    {
                        Thread.Sleep(WaitTime);
                    }
                    return null; // TODO correct?
                }
                if (requestMessage.Type == Message.Message.MessageType.Request4)
                {
                    lock (_receivedBroadcastPingListeners)
                    {
                        foreach (IPeerReceivedBroadcastPing listener in _receivedBroadcastPingListeners)
                        {
                            listener.BroadcastPingReceived(requestMessage.Sender);
                        }
                    }
                }
            }
            else
            {
                // fire-and-forget if requestMessage.Type == MessageType.RequestFf1
                // we received a fire-and forget ping
                // this means we are reachable from the outside
                PeerAddress serverAddress = PeerBean.ServerPeerAddress;
                if (requestMessage.IsUdp)
                {
                    // UDP
                    PeerAddress newServerAddress = serverAddress.ChangeIsFirewalledUdp(false);
                    PeerBean.SetServerPeerAddress(newServerAddress);
                    lock (_reachableListeners)
                    {
                        foreach (IPeerReachable listener in _reachableListeners)
                        {
                            listener.PeerWellConnected(newServerAddress, requestMessage.Sender, false);
                        }
                    }
                    responseMessage = requestMessage;
                }
                else
                {
                    // TCP
                    PeerAddress newServerAddress = serverAddress.ChangeIsFirewalledTcp(false);
                    PeerBean.SetServerPeerAddress(newServerAddress);
                    lock (_reachableListeners)
                    {
                        foreach (IPeerReachable listener in _reachableListeners)
                        {
                            listener.PeerWellConnected(newServerAddress, requestMessage.Sender, true);
                        }
                    }
                    responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);
                }
            }
            return responder.Response(responseMessage, isUdp, channel);
        }

        /// <summary>
        /// Create a neighbor set with one peer.
        /// We only support sending a neighbor set, so we need this wrapper class.
        /// </summary>
        /// <param name="self">The peer that be stored in the neighbor set.</param>
        /// <returns>The neighbor set with exactly one peer.</returns>
        private static NeighborSet CreateNeighborSet(PeerAddress self)
        {
            ICollection<PeerAddress> tmp = new List<PeerAddress>();
            tmp.Add(self);
            return new NeighborSet(-1, tmp);
        }
    }
}
