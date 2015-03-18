using System;
using System.Runtime.CompilerServices;
using NLog;
using TomP2P.Core.Connection;
using TomP2P.Core.Connection.Windows.Netty;

namespace TomP2P.Core.Message
{
    public class TomP2PSinglePacketUdp : BaseInboundHandler, ISharable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISignatureFactory _signatureFactory;

        public TomP2PSinglePacketUdp(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
            //Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        /// <summary>
        /// .NET-specific decoding handler for incoming UDP messages.
        /// </summary>
        public override void Read(ChannelHandlerContext ctx, object msg)
        {
            var dgram = msg as DatagramPacket;
            if (dgram == null)
            {
                ctx.FireRead(msg);
                return;
            }

            var buf = dgram.Content;
            var sender = dgram.Sender;
            var recipient = dgram.Recipient;

            try
            {
                var decoder = new Decoder(_signatureFactory);
                bool finished = decoder.Decode(ctx, buf, recipient, sender);
                if (finished)
                {
                    // prepare finish
                    ctx.FireRead(decoder.PrepareFinish());
                }
                else
                {
                    Logger.Warn("Did not get the complete packet!");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP decoding.", ex);
                throw;
            }
        }

        public override IChannelHandler CreateNewInstance()
        {
            // does not have to be implemeted, this class is ISharable
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return String.Format("TomP2PSinglePacketUdp ({0})", RuntimeHelpers.GetHashCode(this));
        }
    }
}
