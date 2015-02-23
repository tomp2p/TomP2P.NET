using System;
using System.Net;
using TomP2P.Extensions.Netty;
using TomP2P.Storage;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Wrapper for a TCP piece. It is only used on the server-side to wrap
    /// TCP information for server-side inbound pipeline processing.
    /// Stores sender, receiver and content.
    /// </summary>
    public class StreamPiece
    {
        private readonly AlternativeCompositeByteBuf _content;
        private readonly IPEndPoint _recipient;
        private readonly IPEndPoint _sender;

        public StreamPiece(AlternativeCompositeByteBuf content, IPEndPoint recipient, IPEndPoint sender)
        {
            if (content == null)
            {
                throw new NullReferenceException("content");
            }

            _content = content;
            _recipient = recipient;
            _sender = sender;
        }

        public AlternativeCompositeByteBuf Content
        {
            get { return _content; }
        }

        public IPEndPoint Recipient
        {
            get { return _recipient; }
        }

        public IPEndPoint Sender
        {
            get { return _sender; }
        }

        public override string ToString()
        {
            if (_sender != null)
            {
                return String.Format("StreamPiece ({0} => {1}, {2})", _sender, _recipient, _content);
            }
            else
            {
                return String.Format("StreamPiece (=> {0}, {1})", _recipient, _content);
            }
        }
    }
}
