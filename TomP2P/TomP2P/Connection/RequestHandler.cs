using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Futures;
using TomP2P.Message;

namespace TomP2P.Connection
{
    // TODO document with updated JavaDoc (not UDP-only anymore, I guess)
    public class RequestHandler<K> where K : FutureResponse // TODO extend SimpleChannelInboundHandler?
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // the FutureResponse which is currently being waited for
        private readonly K _futureResponse;

        // the node with which this request handler is associated with
        private readonly PeerBean _peerBean;
        private readonly ConnectionBean _connectionBean;

        private readonly Message.Message _message;
        private readonly MessageId _sendMessageId;

        // modifiable variables
        private readonly int _idleTcpSeconds;
        private readonly int _idleUdpSeconds;
        private readonly int _connectionTimeoutTcpMillis;

        /// <summary>
        /// Creates a request handler.
        /// </summary>
        /// <param name="futureResponse">The future that will be called when we get an answer.</param>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="configuration">The client-side connection configuration.</param>
        public RequestHandler(K futureResponse, PeerBean peerBean, ConnectionBean connectionBean, IConnectionConfiguration configuration)
        {
            _futureResponse = futureResponse;
            _peerBean = peerBean;
            _connectionBean = connectionBean;
            _message = futureResponse.Request();
            _sendMessageId = new MessageId(_message);
            _idleTcpSeconds = configuration.IdleTcpSeconds();
            _idleUdpSeconds = configuration.IdleUdpSeconds();
            _connectionTimeoutTcpMillis = configuration.ConnectionTimeoutTcpMillis();
        }

        /// <summary>
        /// Send a UDP message and expect a reply.
        /// </summary>
        /// <param name="channelCreator">The channel creator will create a UDP connection.</param>
        /// <returns>The future that was added in the constructor.</returns>
        public K SendUdp(ChannelCreator channelCreator)
        {
            _connectionBean.Sender.SendUdp(this, _futureResponse, _message, channelCreator, _idleUdpSeconds, false);
            return _futureResponse;
        }
    }
}
