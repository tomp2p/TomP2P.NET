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

        // TODO what to return? Message vs. byte[] vs. ByteBuf
        /// <summary>
        /// .NET-specific encoding handler for outgoing UDP and TCP messages.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public ByteBuf Write(Message msg)
        {
            try
            {
                AlternativeCompositeByteBuf buf = _preferDirect ? _alloc.CompDirectBuffer() : _alloc.CompBuffer();
                // null means create signature
                bool done = _encoder.Write(buf, msg, null);

                Message message = _encoder.Message;

                if (buf.IsReadable)
                {
                    // TODO remove, is done in Sender.SendUDP()
                    /*if (isUdp)
                    {
                        IPEndPoint recipient;
                        IPEndPoint sender;
                        if (message.SenderSocket == null)
                        {
                            // in case of a request
                            if (message.RecipientRelay != null)
                            {
                                // in case of sending to a relay (the relayed flag is already set)
                                recipient = message.RecipientRelay.CreateSocketUdp();
                            }
                            else
                            {
                                recipient = message.Recipient.CreateSocketUdp();
                            }
                            sender = message.Sender.CreateSocketUdp();
                        }
                        else
                        {
                            // in case of a reply
                            recipient = message.SenderSocket;
                            sender = message.RecipientSocket;
                        }
                        // TODO Java uses a DatagramPacket wrapper -> interoperability issue?
                        Logger.Debug("Send UDP message {0}, datagram: TODO.", message);

                        context.UdpSender = sender;
                        context.UdpRecipient = recipient;
                        context.MessageBuffer = buf;
                    }
                    else
                    {
                        Logger.Debug("Send TCP message {0} to {1}.", message, message.SenderSocket);
                        context.MessageBuffer = buf;
                    }*/
                    if (done)
                    {
                        message.SetDone(true);
                        // we wrote the complete message, reset state
                        _encoder.Reset();
                    }
                }
                else
                {
                    return Unpooled.EmptyBuffer;
                }
                return buf;
            }
            catch (Exception)
            {
                // TODO fireExceptionCaught
                throw;
            }
        }
    }
}
