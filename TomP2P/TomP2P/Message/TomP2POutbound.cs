using System;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2POutbound : IOutboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly bool _preferDirect;
        private readonly Encoder _encoder;
        private readonly CompByteBufAllocator _alloc;

        public TomP2POutbound(bool preferDirect, ISignatureFactory signatureFactory)
            : this(preferDirect, signatureFactory, new CompByteBufAllocator())
        { }

        public TomP2POutbound(bool preferDirect, ISignatureFactory signatureFactory, CompByteBufAllocator alloc)
        {
            _preferDirect = preferDirect;
            _encoder = new Encoder(signatureFactory);
            _alloc = alloc;
        }

        /// <summary>
        /// .NET-specific encoding handler for outgoing UDP and TCP messages.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public void Write(ChannelHandlerContext ctx, object msg)
        {
            try
            {
                AlternativeCompositeByteBuf buf;
                Message message;
                bool done;
                if (msg is Message)
                {
                    message = (Message)msg;
                    buf = _preferDirect ? _alloc.CompDirectBuffer() : _alloc.CompBuffer();
                    // null means create signature
                    done = _encoder.Write(buf, message, null);
                }
                else
                {
                    ctx.Write(msg);
                    return;
                }

                message = _encoder.Message;

                // send buffer
                if (buf.IsReadable)
                {
                    ctx.Write(buf);

                    if (done)
                    {
                        message.SetDone(true);
                        // we wrote the complete message, reset state
                        _encoder.Reset();
                    }
                }
                else
                {
                    ctx.Write(Unpooled.EmptyBuffer);
                }
            }
            catch (Exception ex)
            {
                ctx.FireExceptionCaught(ex);
            }
        }
    }
}
