﻿using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Rpc;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Connection.Windows
{
    /// <summary>
    /// Used in the <see cref="Sender"/> to wait for Rcon response.
    /// Checks whether the reverse connection setup was successful.
    /// </summary>
    public class RconInboundHandler : BaseInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly TaskCompletionSource<Message.Message> _tcsRconResponse;
        private readonly TaskCompletionSource<Message.Message> _tcsResponse;

        public RconInboundHandler(TaskCompletionSource<Message.Message> tcsRconResponse, TaskCompletionSource<Message.Message> tcsResponse)
        {
            _tcsRconResponse = tcsRconResponse;
            _tcsResponse = tcsResponse;
        }

        public override void Read(ChannelHandlerContext ctx, object msg)
        {
            // Java uses a SimpleChannelInboundHandler that only expects Message objects
            var responseMessage = msg as Message.Message;
            if (responseMessage == null)
            {
                return;
            }

            if (responseMessage.Command == Rpc.Rpc.Commands.Rcon.GetNr() &&
                responseMessage.Type == Message.Message.MessageType.Ok)
            {
                Logger.Debug("Successfully set up the reverse connection to peer {0}.", responseMessage.Recipient.PeerId);
                _tcsRconResponse.SetResult(responseMessage);
            }
            else
            {
                Logger.Debug("Could not acquire a reverse connection to peer {0}.", responseMessage.Recipient.PeerId);
                var ex = new TaskFailedException("Could not acquire a reverse connection. Got: " + responseMessage);
                _tcsRconResponse.SetException(ex);
                _tcsResponse.SetException(ex);
            }
        }

        public override IChannelHandler CreateNewInstance()
        {
            // TODO correct? shares references...
            var tcsRconResponse = new TaskCompletionSource<Message.Message>(_tcsRconResponse.Task.AsyncState);
            var tcsResponse = new TaskCompletionSource<Message.Message>(_tcsResponse.Task.AsyncState);
            return new RconInboundHandler(tcsRconResponse, tcsResponse);
        }
    }
}
