using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Message;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    public class DirectDataRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private volatile IRawDataReply _rawDataReply;
        private volatile IObjectDataReply _objectDataReply;

        public DirectDataRpc(PeerBean peerBean, ConnectionBean connectionBean)
            : base(peerBean, connectionBean)
        {
            Register(Rpc.Commands.DirectData.GetNr());
        }

        public RequestHandler SendInternal(PeerAddress remotePeer, SendDire)

        public override void HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            throw new NotImplementedException();
        }
    }
}
