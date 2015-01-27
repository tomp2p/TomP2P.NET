using System;
using System.Net;
using System.Net.Sockets;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="TcpClient"/>.
    /// </summary>
    public class MyTcpClient : BaseChannel, ITcpChannel
    {
        // wrapped member
        private readonly TcpClient _tcpClient;

        public MyTcpClient(IPEndPoint localEndPoint)
        {
            // bind
            _tcpClient = new TcpClient(localEndPoint);    
        }

        protected override void DoClose()
        {
            _tcpClient.Close();
        }

        public override Socket Socket
        {
            get { return _tcpClient.Client; }
        }

        public bool IsActive
        {
            get { throw new NotImplementedException(); }
        }
    }
}
