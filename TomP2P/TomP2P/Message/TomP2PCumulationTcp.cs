using System;
using System.Net;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
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
            // setup buffer from bytes
            AlternativeCompositeByteBuf buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(msgBytes.ToSByteArray());

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
                return Decoding(recipient, sender);
            }
            catch (Exception)
            {
                Logger.Error("Error in TCP decoding.");
                throw;
            }
            finally
            {
                if (!_cumulation.IsReadable)
                {
                    _cumulation = null;
                    // no need to discard bytes as this was done in the decoder already
                }
            }
        }

        private Message Decoding(IPEndPoint recipient, IPEndPoint sender)
        {
            bool finished = true;
            bool moreData = true;
            while (finished && moreData)
            {
                // receiver is server.localAddress
                finished = _decoder.Decode(_cumulation, recipient, sender);
                if (finished)
                {
                    _lastId = _decoder.Message.MessageId;
                    moreData = _cumulation.ReadableBytes > 0;
                    // Java's fireChannelRead
                    return _decoder.PrepareFinish();
                }
                else
                {
                    // This ID was the same as the last and the last message already
                    // finished the parsing. So this message is finished as well, although
                    // it may send only partial data.
                    if (_lastId == _decoder.Message.MessageId)
                    {
                        finished = true;
                        moreData = _cumulation.ReadableBytes > 0;
                        return _decoder.PrepareFinish();
                    }
                    else if (_decoder.Message.IsStreaming())
                    {
                        return _decoder.Message;
                    }
                }
            }
            // TODO correct?
            return null;
        }
    }
}
