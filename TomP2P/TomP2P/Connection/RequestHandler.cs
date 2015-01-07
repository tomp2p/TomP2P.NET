using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Futures;
using TomP2P.Message;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Connection
{
    /// <summary>
    /// Is able to send TCP and UDP messages (as a request) and processes incoming responses.
    /// (It is important that this class handles close() because if we shutdown the connections, 
    /// then we need to notify the futures. In case of errors set the peer to offline.)
    /// </summary>
    /// <typeparam name="TFuture">The type of future to handle.</typeparam>
    public class RequestHandler<TFuture> : Inbox where TFuture : FutureResponse // TODO correct, remove
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // the FutureResponse which is currently being waited for
        /// <summary>
        /// The FutureResponse that will be called when we get an answer.
        /// </summary>
        public TFuture FutureResponse { get; private set; }

        // the node with which this request handler is associated with
        /// <summary>
        /// The peer bean.
        /// </summary>
        public PeerBean PeerBean { get; private set; }
        /// <summary>
        /// The connection bean.
        /// </summary>
        public ConnectionBean ConnectionBean { get; private set; }

        private readonly Message.Message _message;
        private readonly MessageId _sendMessageId;

        // modifiable variables
        /// <summary>
        /// The time that a TCP connection can be idle.
        /// </summary>
        public int IdleTcpSeconds { get; private set; }
        /// <summary>
        /// The time that a UDP connection can be idle.
        /// </summary>
        public int IdleUdpSeconds { get; private set; }
        /// <summary>
        /// The time a TCP connection is allowed to be established.
        /// </summary>
        public int ConnectionTimeoutTcpMillis { get; private set; }

        /// <summary>
        /// Creates a request handler that can send TCP and UDP messages.
        /// </summary>
        /// <param name="futureResponse">The future that will be called when we get an answer.</param>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="configuration">The client-side connection configuration.</param>
        public RequestHandler(TFuture futureResponse, PeerBean peerBean, ConnectionBean connectionBean, IConnectionConfiguration configuration)
        {
            FutureResponse = futureResponse;
            PeerBean = peerBean;
            ConnectionBean = connectionBean;
            _message = futureResponse.Request();
            _sendMessageId = new MessageId(_message);
            IdleTcpSeconds = configuration.IdleTcpSeconds;
            IdleUdpSeconds = configuration.IdleUdpSeconds;
            ConnectionTimeoutTcpMillis = configuration.ConnectionTimeoutTcpMillis;
        }

        /// <summary>
        /// Sends a UDP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public async Task<Message.Message> SendUdpAsync(ChannelCreator channelCreator)
        {
            var t = ConnectionBean.Sender.SendUdpAsync(false, _message, channelCreator, IdleUdpSeconds, false);

            Message.Message response = ResponseMessageReceived(await t);
            return response;
        }

        /// <summary>
        /// Sends a UDP message and doesn't expect a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public TFuture FireAndForgetUdp(ChannelCreator channelCreator)
        {
            throw new NotImplementedException();
            return FutureResponse;
        }

        /// <summary>
        /// Broadscasts a UDP message (layer 2) and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public TFuture SendBroadcastUdp(ChannelCreator channelCreator)
        {
            throw new NotImplementedException();
            return FutureResponse;
        }

        /// <summary>
        /// Sends a TCP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a TCP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public TFuture SendTcp(ChannelCreator channelCreator)
        {
            throw new NotImplementedException();
            return FutureResponse;
        }

        // TODO add documentation
        public TFuture SendTcp(PeerConnection peerConnection)
        {
            throw new NotImplementedException();
            return FutureResponse;
        }

        /// <summary>
        /// Sends a TCP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a TCP connection.</param>
        /// <param name="peerConnection"></param>
        /// <returns>The future that was added in the constructor.<</returns>
        public TFuture SendTcp(ChannelCreator channelCreator, PeerConnection peerConnection)
        {
            throw new NotImplementedException();
            return FutureResponse;
        }

        private Message.Message ResponseMessageReceived(Message.Message responseMessage)
        {
            // TODO give back this message in the SendUDP method
            // client-side:
            // here, the result for the awaitable task can be set
            // -> actually, this method can be synchronically called after each "async SendX()"

            var recvMessageId = new MessageId(responseMessage);

            // error handling
            if (responseMessage.Type == Message.Message.MessageType.UnknownId)
            {
                string msg =
                    "Message was not delivered successfully. Unknown ID (peer may be offline or unknown RPC handler): " +
                    _message;
                ExceptionCaught(new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg));
                return;
            }
            else if (responseMessage.Type == Message.Message.MessageType.Exception)
            {
                string msg = "Message caused an exception on the other side. Handle as PeerAbort: " + _message;
                ExceptionCaught(new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg));
                return;
            }
            else if (responseMessage.IsRequest())
            {
                // fireChannelRead -> go to next inbound handler
                return;
            }
            else if (!_sendMessageId.Equals(recvMessageId))
            {
                string msg =
                    String.Format(
                        "Response message [{0}] sent to the node is not the same as we expect. We sent request message [{1}].",
                        responseMessage, _message);
                ExceptionCaught(new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg));
                return;
            }
            // We need to exclude RCON Messages from the sanity check because we
            // use this RequestHandler for sending a Type.REQUEST_1,
            // RPC.Commands.RCON message on top of it. Therefore the response
            // type will never be the same Type as the one the user initially
            // used (e.g. DIRECT_DATA).
            else if (responseMessage.Command != Rpc.Rpc.Commands.Rcon.GetNr()
                     && _message.Recipient.IsRelayed != responseMessage.Sender.IsRelayed)
            {
                string msg =
                    String.Format(
                        "Response message [{0}] sent has a different relay flag than we sent with request message [{1}]. Recipient ({2}) / Sender ({3}).",
                        responseMessage, _message, _message.Recipient.IsRelayed, responseMessage.Sender.IsRelayed);
                ExceptionCaught(new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg));
                return;
            }

            // we got a good answer, let's mark the sender as alive
            if (responseMessage.IsOk() || responseMessage.IsNotOk())
            {
                lock (PeerBean.PeerStatusListeners())
                {
                    foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners())
                    {
                        if (responseMessage.Sender.IsRelayed && responseMessage.PeerSocketAddresses.Count != 0)
                        {
                            // use the response message as we have up-to-date data for the relays
                            PeerAddress remotePeer =
                                responseMessage.Sender.ChangePeerSocketAddresses(responseMessage.PeerSocketAddresses);
                            responseMessage.SetSender(remotePeer);
                        }
                        listener.PeerFound(responseMessage.Sender, null, null);
                    }
                }
            }

            // call this for streaming support
            // TODO futureResponse.progress(responseMessage)
            if (!responseMessage.IsDone)
            {
                Logger.Debug("Good message is streaming {0}.", responseMessage);
                return;
            }

            if (!_message.IsKeepAlive())
            {
                Logger.Debug("Good message, close channel {0}, {1}.", responseMessage, "TODO"); // TODO use channel info here, and close channel
                // TODO set the success now, but trigger the notify when we closed the channel
                // TODO futureResponse.responseLater()
                // TODO close channel
            }
            else
            {
                Logger.Debug("Good message, leave channel open {0}.", responseMessage);
                // TODO futureResponse.response(responseMessage)
            }
        }

        private void ExceptionCaught(Exception cause)
        {
            Logger.Debug("Error originating from {0}. Cause: {1}", _message, cause); // TODO futureResponse used
            // TODO check for completion and warn

            Logger.Debug("Exception caught, but handled properly: {0}", cause);
            if (cause is PeerException)
            {
                var pe = (PeerException) cause;
                if (pe.AbortCause != PeerException.AbortCauseEnum.UserAbort)
                {
                    // do not force if we ran into a timeout, the peer may be busy
                    lock (PeerBean.PeerStatusListeners())
                    {
                        foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners())
                        {
                            listener.PeerFailed(_message.Recipient, pe); // TODO futureResponse used
                        }
                    }
                    Logger.Debug("Removed from map, cause: {0}, message: {1}.", pe, _message);
                }
                else
                {
                    Logger.Warn("Error in request.");
                }
            }
            else
            {
                lock (PeerBean.PeerStatusListeners())
                {
                    foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners())
                    {
                        listener.PeerFailed(_message.Recipient, new PeerException(cause)); // TODO futureResponse used
                    }
                }
            }

            Logger.Debug("Report failure:", cause);
            // TODO futureResponse.failedLater
            // TODO close channel
        }
    }
}
