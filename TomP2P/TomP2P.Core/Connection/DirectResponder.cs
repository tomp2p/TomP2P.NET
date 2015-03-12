using System;
using NLog;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Rpc;

namespace TomP2P.Core.Connection
{
    public class DirectResponder : IResponder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // references required for .NET, because
        // private classes don't work like in Java
        private readonly Dispatcher _dispatcher;
        private readonly PeerBean _peerBeanMaster;

        private readonly ChannelHandlerContext _ctx;
        private readonly Message.Message _requestMessage;

        public DirectResponder(Dispatcher dispatcher, PeerBean peerBeanMaster, ChannelHandlerContext ctx, Message.Message requestMessage)
        {
            _dispatcher = dispatcher;
            _peerBeanMaster = peerBeanMaster;
            _ctx = ctx;
            _requestMessage = requestMessage;
        }

        public void Response(Message.Message responseMessage)
        {
            if (responseMessage.Sender.IsRelayed)
            {
                responseMessage.SetPeerSocketAddresses(responseMessage.Sender.PeerSocketAddresses);
            }

            _dispatcher.Respond(_ctx, responseMessage);
        }

        public void Failed(Message.Message.MessageType type)
        {
            var responseMessage = DispatchHandler.CreateResponseMessage(_requestMessage, type,
                _peerBeanMaster.ServerPeerAddress);
            _dispatcher.Respond(_ctx, responseMessage);
        }

        public void ResponseFireAndForget()
        {
            Logger.Debug("The reply handler was a fire-and-forget handler. No message is sent back for {0}.", _requestMessage);
            if (!_ctx.Channel.IsUdp)
            {
                const string msg = "There is no TCP fire-and-forget. Use UDP in that case. ";
                Logger.Warn(msg + _requestMessage);
                throw new SystemException(msg);
            }
            TimeoutFactory.RemoveTimeout(_ctx);
        }
    }
}
