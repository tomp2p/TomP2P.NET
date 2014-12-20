using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class UdpClientSocket : AsyncClientSocket
    {
        public UdpClientSocket(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
            // TODO remove from base class connect
            ClientSocket = CreateClientSocket(localEndPoint.AddressFamily);
        }

        protected override Socket CreateClientSocket(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public async Task<int> SendAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            // TODO correct endpoint??
            return await ClientSocket.SendToAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
        }

        public async Task<int> ReceiveAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            // TODO correct endpoint? not wildcard?
            return await ClientSocket.ReceiveFromAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
        }
    }
}
