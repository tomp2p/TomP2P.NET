using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class TomP2PCumulationTcp
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Decoder _decoder;
        private AlternativeCompositeByteBuf _cumulation = null;

        private int _lastId = 0;

        public TomP2PCumulationTcp(ISignatureFactory signatureFactory)
        {
            _decoder = new Decoder(signatureFactory);
        }

        /// <summary>
        /// .NET-specific decoding handler for incoming TCP messages.
        /// </summary>
        public Message Read(byte[] msgBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            try
            {

            }
            catch (Exception)
            {
                
                throw;
            }
        }
    }
}
