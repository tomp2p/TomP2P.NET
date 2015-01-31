using System;
using System.Net;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2PCumulationTcp : IInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Decoder _decoder;
        private AlternativeCompositeByteBuf _cumulation = null;

        private int _lastId = 0;

        public TomP2PCumulationTcp(ISignatureFactory signatureFactory)
        {
            _decoder = new Decoder(signatureFactory);
        }

        public void Read(ChannelHandlerContext ctx, object msg)
        {
            var buf = msg as ByteBuf;
            if (buf == null)
            {
                ctx.FireRead(msg);
                return;
            }
            
            // TODO works?
            var sender = (IPEndPoint) ctx.Channel.Socket.RemoteEndPoint;
            var receiver = (IPEndPoint) ctx.Channel.Socket.LocalEndPoint;
            
            try
            {
                if (_cumulation == null)
                {
                    _cumulation = AlternativeCompositeByteBuf.CompBuffer(buf);
                }
                else
                {
                    _cumulation.AddComponent(buf);
                }
                Decoding(ctx, sender, receiver);
            }
            catch (Exception)
            {
                Logger.Error("Error in TCP decoding.");
                throw;
            }
            finally
            {
                if (_cumulation != null && !_cumulation.IsReadable)
                {
                    _cumulation = null;
                    // no need to discard bytes as this was done in the decoder already
                }
            }
        }

        private void Decoding(ChannelHandlerContext ctx, IPEndPoint sender, IPEndPoint receiver)
        {
            bool finished = true;
            bool moreData = true;
            while (finished && moreData)
            {
                // receiver is server.localAddress
                finished = _decoder.Decode(_cumulation, receiver, sender);
                if (finished)
                {
                    _lastId = _decoder.Message.MessageId;
                    moreData = _cumulation.ReadableBytes > 0;
                    ctx.FireRead(_decoder.PrepareFinish());
                }
                else
                {
                    // This ID was the same as the last and the last message already
                    // finished the parsing. So this message is finished as well, although
                    // it may send only partial content.
                    if (_lastId == _decoder.Message.MessageId)
                    {
                        finished = true;
                        moreData = _cumulation.ReadableBytes > 0;
                        ctx.FireRead(_decoder.PrepareFinish());
                    }
                    else if (_decoder.Message.IsStreaming())
                    {
                        ctx.FireRead(_decoder.Message);
                    }
                }
            }
        }

        // TODO find channelInactive equivalent
        // TODO find exceptionCaught equivalent
    }
}
