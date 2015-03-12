using System;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection;
using TomP2P.Core.Peers;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Core.Rpc
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

        /// <summary>
        /// Sends data directly to a peer. Make sure you have set up a reply handler.
        /// This is an RPC.
        /// </summary>
        /// <param name="remotePeer">The remote peer to store the data.</param>
        /// <param name="sendDirectBuilder"></param>
        /// <returns></returns>
        public RequestHandler SendInternal(PeerAddress remotePeer, ISendDirectBuilder sendDirectBuilder)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.DirectData.GetNr(),
                sendDirectBuilder.IsRaw ? Message.Message.MessageType.Request1 : Message.Message.MessageType.Request2);

            var tcsResponse = new TaskCompletionSource<Message.Message>(message);

            if (sendDirectBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(sendDirectBuilder.KeyPair);
            }
            message.SetStreaming(sendDirectBuilder.IsStreaming);

            if (sendDirectBuilder.IsRaw)
            {
                message.SetBuffer(sendDirectBuilder.Buffer);
            }
            else
            {
                try
                {
                    sbyte[] me = Utils.Utils.EncodeObject(sendDirectBuilder.Object);
                    message.SetBuffer(new Message.Buffer(Unpooled.WrappedBuffer(me)));
                }
                catch (Exception ex)
                {
                    tcsResponse.SetException(ex);
                }
            }

            return new RequestHandler(tcsResponse, PeerBean, ConnectionBean, sendDirectBuilder);
        }

        public Task<Message.Message> SendAsync(PeerAddress remotePeer, ISendDirectBuilder sendDirectBuilder,
            ChannelCreator channelCreator)
        {
            var requestHandler = SendInternal(remotePeer, sendDirectBuilder);

            if (!sendDirectBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public override void HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            if (!(requestMessage.Type == Message.Message.MessageType.Request1
                  || requestMessage.Type == Message.Message.MessageType.Request2)
                && requestMessage.Command == Rpc.Commands.DirectData.GetNr())
            {
                throw new ArgumentException("Message content is wrong for this handler.");
            }
            var responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);

            if (sign)
            {
                responseMessage.SetPublicKeyAndSign(PeerBean.KeyPair);
            }
            var rawDataReply2 = _rawDataReply;
            var objectDataReply2 = _objectDataReply;

            if (requestMessage.Type == Message.Message.MessageType.Request1 && rawDataReply2 == null)
            {
                responseMessage.SetType(Message.Message.MessageType.NotFound);
            }
            else if (requestMessage.Type == Message.Message.MessageType.Request2 && objectDataReply2 == null)
            {
                responseMessage.SetType(Message.Message.MessageType.NotFound);
            }
            else
            {
                var requestBuffer = requestMessage.Buffer(0);
                // The user can reply with null, indicating not found or returning 
                // the request buffer, which means nothing is returned.
                // Or an exception can be thrown.
                if (requestMessage.Type == Message.Message.MessageType.Request1)
                {
                    Logger.Debug("Handling Request1.");
                    var responseBuffer = rawDataReply2.Reply(requestMessage.Sender, requestBuffer, requestMessage.IsDone);
                    if (responseBuffer == null && requestMessage.IsDone)
                    {
                        Logger.Warn("Raw reply is null, returning not found.");
                        responseMessage.SetType(Message.Message.MessageType.NotFound);
                    }
// ReSharper disable once PossibleUnintendedReferenceComparison
                    else if (responseBuffer != requestBuffer) // reference equality ok
                    {
                        // can be partial as well
                        if (!responseBuffer.IsComplete)
                        {
                            responseMessage.SetStreaming();
                        }
                        responseMessage.SetBuffer(responseBuffer);
                    }
                }
                else
                {
                    // no streaming here when we deal with objects
                    object obj = Utils.Utils.DecodeObject(requestBuffer.BackingBuffer);
                    Logger.Debug("Handling {0}.", obj);

                    object reply = objectDataReply2.Reply(requestMessage.Sender, obj);
                    if (reply == null)
                    {
                        responseMessage.SetType(Message.Message.MessageType.NotFound);
                    }
                    else if (reply == obj)
                    {
                        responseMessage.SetType(Message.Message.MessageType.Ok);
                    }
                    else
                    {
                        sbyte[] me = Utils.Utils.EncodeObject(reply);
                        responseMessage.SetBuffer(new Message.Buffer(Unpooled.WrappedBuffer(me)));
                    }
                }
            }

            responder.Response(responseMessage);
        }

        public void SetRawDataReply(IRawDataReply rawDataReply)
        {
            _rawDataReply = rawDataReply;
        }

        public void SetObjecDataReply(IObjectDataReply objectDataReply)
        {
            _objectDataReply = objectDataReply;
        }

        public bool HasRawDataReply
        {
            get { return _rawDataReply != null; }
        }

        public bool HasObjectDataReply
        {
            get { return _objectDataReply != null; }
        }
    }
}
