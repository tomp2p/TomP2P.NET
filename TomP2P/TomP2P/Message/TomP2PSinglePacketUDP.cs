using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2PSinglePacketUDP
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISignatureFactory _signatureFactory;

        public TomP2PSinglePacketUDP(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        /// <summary>
        /// .NET-specific decoding handler for incoming UDP messages.
        /// </summary>
        public void Read(byte[] msgBytes)
        {
            // setup buffer from bytes
            AlternativeCompositeByteBuf buf = AlternativeCompositeByteBuf.CompBuffer(); // TODO use direct?
            buf.WriteBytes(msgBytes.ToSByteArray());

            try
            {
                var decoder = new Decoder(_signatureFactory);
                bool finished = decoder.Decode(buf,)
            }
            catch (Exception)
            {
                
                throw;
            }
        }
    }
}
