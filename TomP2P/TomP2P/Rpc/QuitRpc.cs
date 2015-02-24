using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P.Builder;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// This QuitRpc is used to send friendly shutdown messages by peers that are shut down regularly.
    /// </summary>
    public class QuitRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IPeerStatusListener> _listeners = new List<IPeerStatusListener>();

        /// <summary>
        /// Constructor that registers this RPC with the message handler.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        public QuitRpc(PeerBean peerBean, ConnectionBean connectionBean)
            : base(peerBean, connectionBean)
        {
            Register(Rpc.Commands.Quit.GetNr());
        }

        /// <summary>
        /// Adds a peer status listener that gets notified when a peer is offline.
        /// </summary>
        /// <param name="listener">The listener to be added.</param>
        /// <returns>This instance.</returns>
        public QuitRpc AddPeerStatusListener(IPeerStatusListener listener)
        {
            _listeners.Add(listener);
            return this;
        }

        /// <summary>
        /// Sends a message that indicates this peer is about to quit. This is an RPC.
        /// </summary>
        /// <param name="remotePeer">The remote peer to send this request.</param>
        /// <param name="shutdownBuilder">Used for the sign and force TCP flag. Set if the message should be signed.</param>
        /// <param name="channelCreator">The channel creator that creates connections.</param>
        /// <returns>The future response message.</returns>
        public Task<Message.Message> QuitAsync(PeerAddress remotePeer, ShutdownBuilder shutdownBuilder,
            ChannelCreator channelCreator)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Quit.GetNr(),
                Message.Message.MessageType.RequestFf1);
            if (shutdownBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(shutdownBuilder.KeyPair);
            }

            var tcsResponse = new TaskCompletionSource<Message.Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, shutdownBuilder);
            Logger.Debug("Send QUIT message {0}.", message);
            if (!shutdownBuilder.IsForceTcp)
            {
                return requestHandler.FireAndForgetUdpAsync(channelCreator);
            }
            return requestHandler.SendTcpAsync(channelCreator);
        }

        public override void HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            if (!(requestMessage.Type == Message.Message.MessageType.RequestFf1 && requestMessage.Command == Rpc.Commands.Quit.GetNr()))
            {
                throw new ArgumentException("Message content is wrong for this handler.");
            }
            Logger.Debug("Received QUIT message {0}.", requestMessage);

            lock (PeerBean.PeerStatusListeners)
            {
                foreach (var listener in PeerBean.PeerStatusListeners)
                {
                    listener.PeerFailed(requestMessage.Sender,
                        new PeerException(PeerException.AbortCauseEnum.Shutdown, "shutdown"));
                }
            }
            if (requestMessage.IsUdp)
            {
                responder.ResponseFireAndForget();
            }
            else
            {
                responder.Response(CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok));
            }
        }
    }
}
