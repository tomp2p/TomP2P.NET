using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2POutbound // TODO ChannelOutboundHandlerAdapter needed?
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

        //TODO overwrite?
        public void Write(Object msg)
        {
            AlternativeCompositeByteBuf buf = null;
            try
            {
                bool done = false;
                if (msg is Message)
                {
                    var message = (Message) msg;

                    if (_preferDirect)
                    {
                        buf = _alloc.CompDirectBuffer();
                    }
                    else
                    {
                        buf = _alloc.CompBuffer();
                    }
                    // null means create signature
                    done = _encoder.Write(buf, message, null);
                }
                else
                {
                    // TODO ctx.write(buf, message, null), return
                }

                Message message2 = _encoder.Message;

                if (buf.IsReadable)
                {
                    // distinct between UDP and TCP
                    // TODO implement
                    throw new NotImplementedException();
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }
    }
}
