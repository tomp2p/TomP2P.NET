using System;
using System.Runtime.CompilerServices;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Netty.Buffer;
using TomP2P.Storage;

namespace TomP2P.Message
{
    public class TomP2POutbound : BaseChannelHandler, IOutboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly bool _preferDirect;
        private readonly Encoder _encoder;
        private readonly CompByteBufAllocator _alloc;

        // .NET-specific: to be able to clone this instace
        private readonly ISignatureFactory _signatureFactory;

        public TomP2POutbound(bool preferDirect, ISignatureFactory signatureFactory)
            : this(preferDirect, signatureFactory, new CompByteBufAllocator())
        { }

        public TomP2POutbound(bool preferDirect, ISignatureFactory signatureFactory, CompByteBufAllocator alloc)
        {
            _preferDirect = preferDirect;
            _signatureFactory = signatureFactory;
            _encoder = new Encoder(signatureFactory);
            _alloc = alloc;
            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            try
            {
                AlternativeCompositeByteBuf buf;
                bool done;
                var message = msg as Message;
                if (message != null)
                {
                    buf = _preferDirect ? _alloc.CompDirectBuffer() : _alloc.CompBuffer();
                    // null means create signature
                    done = _encoder.Write(buf, message, null);
                }
                else
                {
                    ctx.FireWrite(msg); // TODO ok? Java uses ctx.write() (3x)
                    return;
                }

                message = _encoder.Message;

                // send buffer
                if (buf.IsReadable)
                {
                    // sender/recipient information is extracted in MyUdpClient.SendAsync()
                    ctx.FireWrite(buf);

                    if (done)
                    {
                        message.SetDone(true);
                        // we wrote the complete message, reset state
                        _encoder.Reset();
                    }
                }
                else
                {
                    ctx.FireWrite(Unpooled.EmptyBuffer);
                }
            }
            catch (Exception ex)
            {
                ctx.FireExceptionCaught(ex);
            }
        }

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            if (_encoder.Message == null)
            {
                Logger.Error("Exception in encoding when starting.", cause);
                Console.WriteLine(cause.StackTrace);
            }
            else if (_encoder.Message != null && !_encoder.Message.IsDone)
            {
                Logger.Error("Exception in encoding when started.", cause);
                Console.WriteLine(cause.StackTrace);
            }
        }

        public override IChannelHandler CreateNewInstance()
        {
            return new TomP2POutbound(_preferDirect, _signatureFactory);
        }

        public override string ToString()
        {
            return String.Format("TomP2POutbound ({0})", RuntimeHelpers.GetHashCode(this));
        }
    }
}
