using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class UdpClientSocket : AsyncClientSocket
    {
        public UdpClientSocket(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
        }

        protected override Socket CreateClientSocket(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public override async Task<int> Send(byte[] buffer)
        {
            // TODO correct endpoint??
            throw new NotImplementedException();
        }

        public override async Task<int> Receive(byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}
