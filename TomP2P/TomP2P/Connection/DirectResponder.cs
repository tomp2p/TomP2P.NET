using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Message;
using TomP2P.Rpc;

namespace TomP2P.Connection
{
    public class DirectResponder : IResponder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dispatcher _dispatcher;
        private readonly PeerBean _peerBeanMaster;
        private readonly Message.Message _requestMessage;

        public DirectResponder(Dispatcher dispatcher, PeerBean peerBeanMaster, Message.Message requestMessage)
        {
            _dispatcher = dispatcher;
            _peerBeanMaster = peerBeanMaster;
            _requestMessage = requestMessage;
        }

        public void Response(Message.Message responseMessage)
        {
            if (responseMessage == null || responseMessage.Sender == null)
            {
                Console.WriteLine("why");
            }
            if (responseMessage.Sender.IsRelayed)
            {
                responseMessage.SetPeerSocketAddresses(responseMessage.Sender.PeerSocketAddresses);
            }

            _dispatcher.Respond(responseMessage);
        }

        public void Failed(Message.Message.MessageType type, string reason)
        {
            var responseMessage = DispatchHandler.CreateResponseMessage(_requestMessage, type,
                _peerBeanMaster.ServerPeerAddress);
            _dispatcher.Respond(responseMessage);
        }

        public void ResponseFireAndForget(bool isUdp)
        {
            Logger.Debug("The reply handler was a fire-and-forget handler. No message is sent back for {0}.", _requestMessage);
            if (!isUdp)
            {
                string msg = "There is no TCP fire-and-forget. Use UDP in that case. ";
                Logger.Warn(msg + _requestMessage);
                throw new SystemException(msg);
            }
            else
            {
                // TODO remove timeout
                TimeoutFactory.RemoveTimeout();
            }
        }
    }
}
