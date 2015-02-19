using System;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Message;
using TomP2P.P2P;
using TomP2P.P2P.Builder;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    public class BroadcastRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The broadcast handler that is currently used.
        /// </summary>
        public IBroadcastHandler BroadcastHandler { get; private set; }

        public BroadcastRpc(PeerBean peerBean, ConnectionBean connectionBean, IBroadcastHandler broadcastHandler)
            : base(peerBean, connectionBean)
        {
            Register(Rpc.Commands.Broadcast.GetNr());
            BroadcastHandler = broadcastHandler;
        }

        public Task<Message.Message> SendAsync(PeerAddress remotePeer, BroadcastBuilder broadcastBuilder,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Broadcast.GetNr(),
                Message.Message.MessageType.RequestFf1);
            message.SetIntValue(broadcastBuilder.HopCounter);
            message.SetKey(broadcastBuilder.MessageKey);
            if (broadcastBuilder.DataMap != null)
            {
                message.SetDataMap(new DataMap(broadcastBuilder.DataMap));
            }
            var tcsResponse = new TaskCompletionSource<Message.Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, configuration);
            if (!broadcastBuilder.IsUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            else
            {
                return requestHandler.FireAndForgetUdpAsync(channelCreator);
            }
        }

        public override void HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            if (!(requestMessage.Type == Message.Message.MessageType.RequestFf1
                && requestMessage.Command == Rpc.Commands.Broadcast.GetNr()))
            {
                throw new ArgumentException("Message content is wrong for this handler.");   
            }
            Logger.Debug("Received BROADCAST message: {0}.", requestMessage);
            BroadcastHandler.Receive(requestMessage);
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
