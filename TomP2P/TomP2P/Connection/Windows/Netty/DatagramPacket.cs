using System;
using System.Net;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// .NET equivalent for Java Netty's DatagramPacket. It is only used on the server-side to wrap
    /// UDP information for server-side inbound pipeline processing.
    /// Stores sendre, receiver and buffer.
    /// </summary>
    public class DatagramPacket
    {
        private readonly ByteBuf _content;
        private readonly IPEndPoint _recipient;
        private readonly IPEndPoint _sender;

        public DatagramPacket(ByteBuf content, IPEndPoint recipient, IPEndPoint sender)
        {
            if (content == null)
            {
                throw new NullReferenceException("content");
            }

            _content = content;
            _recipient = recipient;
            _sender = sender;
        }

        /// <summary>
        /// Equivalent to Java Netty's content().
        /// </summary>
        /// <returns></returns>
        public ByteBuf Content
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
                return String.Format("Datagram ({0} => {1}, {2})", _sender, _recipient, _content);
            }
            else
            {
                return String.Format("Datagram (=> {0}, {1})", _recipient, _content);
            }
        }
    }
}
