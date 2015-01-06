using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Pipeline
{
    public class Context
    {
        public IPEndPoint UdpSender;
        public IPEndPoint UdpRecipient;

        public ByteBuf MessageBuffer;
    }
}
