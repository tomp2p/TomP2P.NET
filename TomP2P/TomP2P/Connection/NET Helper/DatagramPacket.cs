using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.NET_Helper
{
    // TODO figure out how Netty serializes this wrapper and sends it over the wire

    /// <summary>
    /// .NET equivalent for Java Netty's DatagramPacket, which is the message container that
    /// gets sent over the wire for UDP connections.
    /// Stores sendre, receiver and buffer.
    /// </summary>
    public class DatagramPacket
    {
        private readonly ByteBuf _data;
        private readonly IPEndPoint _recipient;
        private readonly IPEndPoint _sender;

        public DatagramPacket(ByteBuf data, IPEndPoint recipient, IPEndPoint sender)
        {
            if (data == null)
            {
                throw new NullReferenceException("data");
            }

            _data = data;
            _recipient = recipient;
            _sender = sender;
        }

        /// <summary>
        /// Equivalent to Java Netty's content().
        /// </summary>
        /// <returns></returns>
        public ByteBuf Data
        {
            get { return _data; }
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
                return String.Format("Datagram ({0} => {1}, {2})", _sender, _recipient, _data);
            }
            else
            {
                return String.Format("Datagram (=> {0}, {1})", _recipient, _data);
            }
        }
    }
}
