using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2PSinglePacketUdp
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISignatureFactory _signatureFactory;

        public TomP2PSinglePacketUdp(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        /// <summary>
        /// .NET-specific decoding handler for incoming UDP messages.
        /// </summary>
        public Message Read(byte[] msgBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            // setup buffer from bytes
            AlternativeCompositeByteBuf buf = AlternativeCompositeByteBuf.CompBuffer(); // TODO use direct?
            buf.WriteBytes(msgBytes.ToSByteArray());

            try
            {
                var decoder = new Decoder(_signatureFactory); // TODO provide isUdp info
                bool finished = decoder.Decode(buf, recipient, sender);
                if (finished)
                {
                    // prepare finish
                    var message = decoder.PrepareFinish();
                    return message;
                }
                else
                {
                    Logger.Warn("Did not get the complete packet!");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP decoding.", ex);
                throw ex;
            }
        }
    }
}
