using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.NET_Helper
{
    /// <summary>
    /// .NET equivalent for Java Netty's DatagramPacket, which is the message container that
    /// gets sent over the wire for UDP connections.
    /// Stores sendre, receiver and buffer.
    /// </summary>
    public class DatagramPacket
    {
        private readonly ByteBuf _data;
        private readonly IPAddress _recipient;
        private readonly IPAddress _sender;

        public DatagramPacket(ByteBuf data, IPAddress recipient, IPAddress sender)
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

        public IPAddress Recipient
        {
            get { return _recipient; }
        }

        public IPAddress Sender
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
