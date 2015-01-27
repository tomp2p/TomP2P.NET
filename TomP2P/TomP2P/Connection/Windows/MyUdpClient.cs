using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="UdpClient"/>.
    /// </summary>
    public class MyUdpClient : BaseChannel, IUdpChannel
    {
        // wrapped member
        private readonly UdpClient _udpClient;

        public MyUdpClient(IPEndPoint localEndPoint)
        {
            // bind
            _udpClient = new UdpClient(localEndPoint);    
        }

        protected override void DoClose()
        {
            throw new NotImplementedException();
        }

        public bool IsOpen()
        {
            throw new NotImplementedException();
        }
    }
}
