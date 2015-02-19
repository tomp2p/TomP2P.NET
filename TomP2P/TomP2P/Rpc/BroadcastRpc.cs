using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.P2P.Builder;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    public class BroadcastRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IBroadcastHandler _broadcastHandler;

        public BroadcastRpc(PeerBean peerBean, ConnectionBean connectionBean, IBroadcastHandler broadcastHandler)
            : base(peerBean, connectionBean)
        {
            Register(Rpc.Commands.Broadcast.GetNr());
            _broadcastHandler = broadcastHandler;
        }

        public Task<Message.Message> SendAsync(PeerAddress remotePeer, BroadcastBuilder broadcastBuilder,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public override void HandleResponse(Message.Message requestMessage, Connection.PeerConnection peerConnection, bool sign, Message.IResponder responder)
        {
            // TODO implement
            throw new NotImplementedException();
        }
    }
}
