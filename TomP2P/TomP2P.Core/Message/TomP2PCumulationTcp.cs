using System;
using System.Net;
using System.Runtime.CompilerServices;
using NLog;
using TomP2P.Core.Connection;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Storage;

namespace TomP2P.Core.Message
{
    public class TomP2PCumulationTcp : BaseInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Decoder _decoder;
        private AlternativeCompositeByteBuf _cumulation;

        private int _lastId;

        // .NET-specific: to be able to clone this instace
        private readonly ISignatureFactory _signatureFactory;

        public TomP2PCumulationTcp(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
            _decoder = new Decoder(signatureFactory);
        }

        public override void Read(ChannelHandlerContext ctx, object msg)
        {
            // .NET-specific: use a content wrapper for TCP, similar to TomP2PSinglePacketUdp
            var piece = msg as StreamPiece;
            if (piece == null)
            {
                //ctx.FireRead(msg);
                return;
            }

            var buf = piece.Content;
            var sender = piece.Sender;
            var recipient = piece.Recipient;
            Logger.Debug("{0}: Cumulating TCP stream piece.", this);
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
                Decoding(ctx, sender, recipient);
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
                    // Java: no need to discard bytes as this was done in the decoder already
                }
            }
        }

        private void Decoding(ChannelHandlerContext ctx, IPEndPoint sender, IPEndPoint receiver)
        {
            bool finished = true;
            bool moreData = true;
            while (finished && moreData)
            {
                finished = _decoder.Decode(ctx, _cumulation, receiver, sender);
                if (finished)
                {
                    _lastId = _decoder.Message.MessageId;
                    moreData = _cumulation.ReadableBytes > 0;
                    ctx.FireRead(_decoder.PrepareFinish());
                }
                else
                {
                    if (_decoder.Message == null)
                    {
                        // Wait for more data. This may happen if we don't get the first
                        // 58 bytes, which is the size of the header.
                        return;
                    }

                    if (_lastId == _decoder.Message.MessageId)
                    {
                        // This ID was the same as the last and the last message already
                        // finished the parsing. So this message is finished as well, although
                        // it may send only partial content.
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

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            if (_decoder.Message == null && _decoder.LastContent == Message.Content.Empty)
            {
                Logger.Error("Exception in decoding TCP. Occurred before starting to decode.", cause);
            }
            else if (_decoder.Message != null && !_decoder.Message.IsDone)
            {
                Logger.Error("Exception in decoding TCP. Occurred after starting to decode.", cause);
            }
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            var sender = ctx.Channel.RemoteEndPoint;
            var recipient = ctx.Channel.LocalEndPoint;

            try
            {
                if (_cumulation != null)
                {
                    Decoding(ctx, sender, recipient);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in TCP decoding. (Inactive)", ex);
                throw;
            }
            finally
            {
                _cumulation = null;
                // TODO ctx.FireInactive needed?
            }
        }

        public override IChannelHandler CreateNewInstance()
        {
            return new TomP2PCumulationTcp(_signatureFactory);
        }

        public override string ToString()
        {
            return String.Format("TomP2PCumulationTcp ({0})", RuntimeHelpers.GetHashCode(this));
        }
    }
}
