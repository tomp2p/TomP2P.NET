using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.Windows.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="TcpClient"/>.
    /// </summary>
    public class MyTcpClient : BaseChannel, ITcpChannel
    {
        // wrapped member
        private readonly TcpClient _tcpClient;

        public MyTcpClient(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(pipeline)
        {
            // bind
            _tcpClient = new TcpClient(localEndPoint);    
        }

        public Task ConnectAsync(IPEndPoint remoteEndPoint)
        {
            // just forward
            return _tcpClient.ConnectAsync(remoteEndPoint.Address, remoteEndPoint.Port);
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

        public override bool IsUdp
        {
            get { return false; }
        }

        public override bool IsTcp
        {
            get { return true; }
        }
    }
}
