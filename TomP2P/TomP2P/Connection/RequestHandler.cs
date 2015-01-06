using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Futures;
using TomP2P.Message;

namespace TomP2P.Connection
{
    /// <summary>
    /// Is able to send TCP and UDP messages (as a request) and processes incoming responses.
    /// (It is important that this class handles close() because if we shutdown the connections, 
    /// then we need to notify the futures. In case of errors set the peer to offline.)
    /// </summary>
    /// <typeparam name="TFuture">The type of future to handle.</typeparam>
    public class RequestHandler<TFuture> : Inbox where TFuture : FutureResponse
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
            IdleTcpSeconds = configuration.IdleTcpSeconds();
            IdleUdpSeconds = configuration.IdleUdpSeconds();
            ConnectionTimeoutTcpMillis = configuration.ConnectionTimeoutTcpMillis();
        }

        /// <summary>
        /// Sends a UDP message and expects a response.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public TFuture SendUdp(ChannelCreator channelCreator)
        {
            ConnectionBean.Sender.SendUdpAsync(this, _futureResponse, _message, channelCreator, _idleUdpSeconds, false);
            return FutureResponse;
            // TODO await response here, remove Inbox parameter
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

        public override void MessageReceived(Message.Message message)
        {
            // client-side:
            // here, the result for the awaitable task can be set
            // -> actually, this method can be synchronically called after each "async SendX()"
            throw new NotImplementedException();
        }

        public override void ExceptionCaught(Exception cause)
        {
            throw new NotImplementedException();
        }
    }
}
