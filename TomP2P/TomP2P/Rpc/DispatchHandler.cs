using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// The dispatcher handlers that can be added to the Dispatcher.
    /// </summary>
    public abstract class DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The peer bean.
        /// </summary>
        public PeerBean PeerBean { get; private set; }

        /// <summary>
        /// The connection bean.
        /// </summary>
        public ConnectionBean ConnectionBean { get; private set; }

        private bool _sign = false;

        /// <summary>
        /// Creates a handler with a peer bean and a connection bean.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        protected DispatchHandler(PeerBean peerBean, ConnectionBean connectionBean)
        {
            PeerBean = peerBean;
            ConnectionBean = connectionBean;
        }

        /// <summary>
        /// Registers all names on the dispatcher on behalf of the own peer.
        /// </summary>
        /// <param name="names"></param>
        public void Register(params int[] names)
        {
            Number160 onBehalfOf = PeerBean.ServerPeerAddress().PeerId;
            Register(onBehalfOf, names);
        }

        /// <summary>
        /// Registers all names on the dispatcher on behalf of the own peer.
        /// </summary>
        /// <param name="onBehalfOf">The IO handler can be registered for the own 
        /// use of in behalf of another peer. (e.g., iin case of a relay node)</param>
        /// <param name="names"></param>
        public void Register(Number160 onBehalfOf, params int[] names)
        {
            ConnectionBean.Dispatcher.RegisterIOHandler(PeerBean.ServerPeerAddress().PeerId, onBehalfOf, this, names);
        }

        /// <summary>
        /// Set to true, if the message is signed.
        /// </summary>
        /// <param name="sign"></param>
        public void SetSign(bool sign)
        {
            _sign = sign;
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
                .SetVersion(ConnectionBean.P2PId);
        }

        // TODO finish port
    }
}
