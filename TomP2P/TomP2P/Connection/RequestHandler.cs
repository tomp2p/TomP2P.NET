using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Connection.Windows.Netty;
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
    public class RequestHandler : BaseInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // the FutureResponse which is currently being waited for
        /// <summary>
        /// The FutureResponse that will be called when we get an answer.
        /// </summary>
        private readonly TaskCompletionSource<Message.Message> _tcsResponse;

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

        // .NET-specific: to be able to clone this instace
        private readonly IConnectionConfiguration _configuration;

        /// <summary>
        /// Creates a request handler that can send TCP and UDP messages.
        /// </summary>
        /// <param name="tcsResponse">The future that will be called when we get an answer.</param>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="configuration">The client-side connection configuration.</param>
        public RequestHandler(TaskCompletionSource<Message.Message> tcsResponse, PeerBean peerBean, ConnectionBean connectionBean, IConnectionConfiguration configuration)
        {
            _tcsResponse = tcsResponse;
            PeerBean = peerBean;
            ConnectionBean = connectionBean;
            _message = tcsResponse.Task.AsyncState as Message.Message;
            _sendMessageId = new MessageId(_message);
            _configuration = configuration;
            IdleTcpSeconds = configuration.IdleTcpSeconds;
            IdleUdpSeconds = configuration.IdleUdpSeconds;
            ConnectionTimeoutTcpMillis = configuration.ConnectionTimeoutTcpMillis;

            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public TaskCompletionSource<Message.Message> TcsResponse
        {
            get { return _tcsResponse; }
        }

        /// <summary>
        /// Sends a UDP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future task that was added in the constructor.</returns>
        public Task<Message.Message> SendUdpAsync(ChannelCreator channelCreator)
        {
            // TODO find more efficient way instead of 1 thread per message
            // TODO remove code-duplicates -> extract method
            // so far, everything is sync -> invoke async / new thread
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendUdpAsync(this, _tcsResponse, _message, channelCreator, IdleUdpSeconds, false);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });

            return _tcsResponse.Task;
        }

        /// <summary>
        /// Broadscasts a UDP message (layer 2) and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future task that was added in the constructor.</returns>
        public Task<Message.Message> SendBroadcastUdpAsync(ChannelCreator channelCreator)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendUdpAsync(this, _tcsResponse, _message, channelCreator, IdleUdpSeconds, true);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });
            return _tcsResponse.Task;
        }

        /// <summary>
        /// Sends a UDP message and doesn't expect a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future task that was added in the constructor.</returns>
        public Task<Message.Message> FireAndForgetUdpAsync(ChannelCreator channelCreator)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendUdpAsync(null, _tcsResponse, _message, channelCreator, 0, false);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });
            return _tcsResponse.Task;
        }

        /// <summary>
        /// Sends a TCP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a TCP connection.</param>
        /// <returns>The future task that was added in the constructor.</returns>
        public Task<Message.Message> SendTcpAsync(ChannelCreator channelCreator)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendTcpAsync(this, _tcsResponse, _message, channelCreator, IdleTcpSeconds,
                        ConnectionTimeoutTcpMillis, null);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });
            return _tcsResponse.Task;
        }

        public Task<Message.Message> SendTcpAsync(PeerConnection peerConnection)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendTcpAsync(this, _tcsResponse, _message, null, IdleTcpSeconds, 
                        ConnectionTimeoutTcpMillis, peerConnection);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });
            return _tcsResponse.Task;
        }

        /// <summary>
        /// Sends a TCP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a TCP connection.</param>
        /// <param name="peerConnection"></param>
        /// <returns>The future task that was added in the constructor.</returns>
        public Task<Message.Message> SendTcpAsync(ChannelCreator channelCreator, PeerConnection peerConnection)
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ConnectionBean.Sender.SendTcpAsync(this, _tcsResponse, _message, channelCreator, IdleTcpSeconds,
                        ConnectionTimeoutTcpMillis, peerConnection);
                }
                catch (Exception ex)
                {
                    _tcsResponse.SetException(ex);
                }
            });
            return _tcsResponse.Task;
        }

        public override void Read(ChannelHandlerContext ctx, object msg)
        {
            // client-side:
            // Here, the result for the awaitable task can be set.
            // If it is not a fire-and-forget message, the "result" of the TCS
            // must be set here. (If FF, then it is set in Sender.)

            // Java uses a SimpleChannelInboundHandler that only expects Message objects
            var responseMessage = msg as Message.Message;
            if (responseMessage == null)
            {
                return;
            }

            var recvMessageId = new MessageId(responseMessage);

            // error handling
            if (responseMessage.Type == Message.Message.MessageType.UnknownId)
            {
                string msg2 =
                    "Message was not delivered successfully. Unknown ID (peer may be offline or unknown RPC handler): " +
                    _message;
                ExceptionCaught(ctx, new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg2));
                return;
            }
            if (responseMessage.Type == Message.Message.MessageType.Exception)
            {
                string msg2 = "Message caused an exception on the other side. Handle as PeerAbort: " + _message;
                ExceptionCaught(ctx, new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg2));
                return;
            }
            if (responseMessage.IsRequest())
            {
                ctx.FireRead(responseMessage);
                return;
            }
            if (!_sendMessageId.Equals(recvMessageId))
            {
                string msg2 =
                    String.Format(
                        "Response message [{0}] sent to the node is not the same as we expect. We sent request message [{1}].",
                        responseMessage, _message);
                ExceptionCaught(ctx, new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg2));
                return;
            }
            // We need to exclude RCON messages from the sanity check because we
            // use this RequestHandler for sending a Type.REQUEST_1,
            // RPC.Commands.RCON message on top of it. Therefore the response
            // type will never be the same Type as the one the user initially
            // used (e.g. DIRECT_DATA).
            if (responseMessage.Command != Rpc.Rpc.Commands.Rcon.GetNr()
                     && _message.Recipient.IsRelayed != responseMessage.Sender.IsRelayed)
            {
                string msg2 =
                    String.Format(
                        "Response message [{0}] sent has a different relay flag than we sent with request message [{1}]. Recipient ({2}) / Sender ({3}).",
                        responseMessage, _message, _message.Recipient.IsRelayed, responseMessage.Sender.IsRelayed);
                ExceptionCaught(ctx, new PeerException(PeerException.AbortCauseEnum.PeerAbort, msg2));
                return;
            }

            // we got a good answer, let's mark the sender as alive
            if (responseMessage.IsOk() || responseMessage.IsNotOk())
            {
                lock (PeerBean.PeerStatusListeners)
                {
                    if (responseMessage.Sender.IsRelayed && responseMessage.PeerSocketAddresses.Count != 0)
                    {
                        // use the response message as we have up-to-date content for the relays
                        PeerAddress remotePeer =
                            responseMessage.Sender.ChangePeerSocketAddresses(responseMessage.PeerSocketAddresses);
                        responseMessage.SetSender(remotePeer);
                    }
                    foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners)
                    {
                        listener.PeerFound(responseMessage.Sender, null, null);
                    }
                }
            }

            // call this for streaming support
            // TODO futureResponse.progress(responseMessage)
            if (!responseMessage.IsDone)
            {
                Logger.Debug("Good message is streaming. {0}", responseMessage);
                return;
            }

            if (!_message.IsKeepAlive())
            {
                Logger.Debug("Good message {0}. Close channel {1}.", responseMessage, ctx.Channel);
                
                // .NET-specific:
                // set the result now, but trigger the notify when the channel is closed
                _tcsResponse.ResponseLater(responseMessage, ctx);

                // in Java, the channel creator adds a listener that sets the 
                // future response result when the channel is closed
                ctx.Close(); // TODO needed?
            }
            else
            {
                Logger.Debug("Good message {0}. Leave channel {1} open.", responseMessage, ctx.Channel);
                _tcsResponse.SetResult(responseMessage);
                // TODO but shouldn't the Sender already have closed the channel?
            }
        }

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            Logger.Debug("Error originating from {0}. Cause: {1}", _message, cause);
            if (_tcsResponse.Task.IsCompleted)
            {
                Logger.Warn("Got exception, but ignored it. (Task completed.): {0}.", _tcsResponse.Task.Exception);
            }
            else
            {
                Logger.Debug("Exception caught, but handled properly: {0}.", cause.Message);
                var pe = cause as PeerException;
                if (pe != null)
                {
                    if (pe.AbortCause != PeerException.AbortCauseEnum.UserAbort)
                    {
                        // do not force if we ran into a timeout, the peer may be busy
                        lock (PeerBean.PeerStatusListeners)
                        {
                            foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners)
                            {
                                listener.PeerFailed(_message.Recipient, pe);
                            }
                        }
                        Logger.Debug("Removed from map. Cause: {0}. Message: {1}.", pe, _message);
                    }
                    else
                    {
                        Logger.Warn("Error in request.", cause);
                    }
                }
                else
                {
                    lock (PeerBean.PeerStatusListeners)
                    {
                        foreach (IPeerStatusListener listener in PeerBean.PeerStatusListeners)
                        {
                            listener.PeerFailed(_message.Recipient, new PeerException(cause));
                        }
                    }
                }
            }

            Logger.Debug("Report failure: ", cause);
            _tcsResponse.SetException(cause);
            // TODO channel not already closed in Sender?
            ctx.Close(); // TODO used?
        }

        public override IChannelHandler CreateNewInstance()
        {
            // the same instance can be returned, as there is one RequestHandler per client socket and message
            return this;
        }

        public override string ToString()
        {
            return String.Format("RequestHandler ({0})", RuntimeHelpers.GetHashCode(this));
        }
    }
}
